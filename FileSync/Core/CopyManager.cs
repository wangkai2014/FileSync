using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.IO;
using static FileSync.GlobalDefinitions;

namespace FileSync.Core
{
    /// <summary>
    /// This class will handle the actual copy.
    /// It is a singleton because this program is very IO intensive and having multiple copies simultaneously on
    /// the same (physical) drive is often not desirable.
    /// TODO: To have more control and display a more accurate progress based on size, implement our own stream copier.
    /// </summary>
    public sealed class CopyManager
    {
        #region Singleton pattern

        private static readonly Lazy<CopyManager> lazy =
        new Lazy<CopyManager>(() => new CopyManager());

        public static CopyManager Instance { get { return lazy.Value; } }

        private CopyManager()
        {
        }

        #endregion

        #region Fields

        // We can have multiple queues to handle (or the same one multiple times, 
        // like if there is an end of queue signal in the middle).
        // Also, queues always come by pairs, on to process and one to push feedback in.
        private BufferBlock<Tuple<BufferBlock<CopyWorkItem>, BufferBlock<Tuple<long, bool>>>> m_queues;
        private BufferBlock<CopyWorkItem> m_currentWorkItemsQueue;
        private BufferBlock<Tuple<long, bool>> m_currentFeedbackQueue;

        private Task m_copyTask;

        #endregion

        #region Public methods

        /// <summary>
        /// Enqueue the given buffer to be handled next.
        /// </summary>
        /// <param name="queue"></param>
        public void HandleQueue(BufferBlock<CopyWorkItem> queue, BufferBlock<Tuple<long, bool>> feedbackQueue)
        {
            if (m_queues == null)
            {
                m_queues = new BufferBlock<Tuple<BufferBlock<CopyWorkItem>, BufferBlock<Tuple<long, bool>>>>();
            }

            m_queues.Post(new Tuple<BufferBlock<CopyWorkItem>, BufferBlock<Tuple<long, bool>>>(queue, feedbackQueue));

            if (m_copyTask == null || m_copyTask.IsCompleted)
            {
                m_copyTask = Task.Factory.StartNew(HandleNextQueue);
            }
        }

        #endregion

        #region Private methods

        private async Task HandleNextQueue()
        {
            if (m_queues.Count == 0)
                return;

            var item = await m_queues.ReceiveAsync();

            m_currentWorkItemsQueue = item.Item1;
            m_currentFeedbackQueue = item.Item2;

            while (true) // Consume the data until we get the end code
            {
                var workItem = await m_currentWorkItemsQueue.ReceiveAsync();

                // This is our signal that we are done for this queue
                if (workItem.Direction == StopCode)
                {
                    m_currentFeedbackQueue.Post(new Tuple<long, bool>(LongStopCode, false));
                    break;
                }

                var sourcePath = workItem.SourcePath;
                var destinationPath = workItem.DestinationPath;

                switch (workItem.Direction)
                {
                    case CopyDirection.DeleteAtDestination:
                        if (workItem.IsDirectory)
                            DeleteDirectory(destinationPath);
                        else
                            DeleteFile(destinationPath);
                        break;

                    case CopyDirection.DeleteAtSource:
                        if (workItem.IsDirectory)
                            DeleteDirectory(sourcePath);
                        else
                            DeleteFile(sourcePath);
                        break;

                    case CopyDirection.ToDestination:
                        if (workItem.IsDirectory)
                            CreateDirectory(destinationPath);
                        else
                            CopyFile(sourcePath, destinationPath, workItem.Size);
                        break;

                    case CopyDirection.ToSource:
                        if (workItem.IsDirectory)
                            CreateDirectory(sourcePath);
                        else
                            CopyFile(destinationPath, sourcePath, workItem.Size);
                        break;
                }
            }

            HandleNextQueue(); // Fire and forget
        }

        private void CreateDirectory(string directory)
        {
            // TODO: add a logger or something similar
            Directory.CreateDirectory(directory);
        }

        private void CopyFile(string filePath, string destination, long fileSize) // TODO: REMOVE FILE SIZE WHEN IMPLEMENT OUR COPY
        {
            // TODO: add a logger or something similar.
            // TODO: when a "detailed" copy is implemented, give higher resolution feedback in loop.
            m_currentFeedbackQueue.Post(new Tuple<long, bool>(0, false));
            File.Copy(filePath, destination);
            m_currentFeedbackQueue.Post(new Tuple<long, bool>(fileSize, true));
        }

        private void DeleteDirectory(string directory)
        {
            // TODO: add a logger or something similar
            Directory.Delete(directory);
        }

        private void DeleteFile(string file)
        {
            // TODO: add a logger or something similar
            File.Delete(file);
        }

        #endregion
    }
}
