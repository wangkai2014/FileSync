using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.IO;

namespace FileSync
{
    /// <summary>
    /// This class will handle the actual copy.
    /// It is a singleton because this program is very IO intensive and having multiple copies simultaneously on
    /// the same (physical) drive is often not desirable.
    /// TODO: To have more control and display a more accurate progress based on size, implement our own stream copier.
    /// </summary>
    public sealed class CopyManager
    {
        #region Classes and enums

        /// <summary>
        /// Describes the way files must be synced.
        /// Unused values are possible (eg: ToDestination | DeleteAtDestination), it's ok,
        /// it's actually used to signal the end of a queue.
        /// </summary>
        public enum CopyDirection
        {
            ToDestination = 1, // Copy from source to destination
            ToSource = 2, // Copy from destination to source
            DeleteAtDestination = 4, // Delete files that are [in destination but not in source]
            DeleteAtSource = 8 // Delete files that are [in source but not at destination]
        }

        public class CopyWorkItem
        {
            #region Fields

            public string SourcePath;
            public string DestinationPath;

            public CopyDirection Direction;

            public bool IsDirectory;

            #endregion

            #region Constructors

            public CopyWorkItem() { }

            /// <summary>
            /// Copy constructor.
            /// </summary>
            /// <param name="copyWorkItem">The object to copy from.</param>
            public CopyWorkItem(CopyWorkItem copyWorkItem)
            {
                this.SourcePath = copyWorkItem.SourcePath;
                this.DestinationPath = copyWorkItem.DestinationPath;
                this.Direction = copyWorkItem.Direction;
                this.IsDirectory = copyWorkItem.IsDirectory;
            }

            #endregion
        }

        #endregion

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
        // eg. if there is an end of queue signal in the middle).
        private BufferBlock<BufferBlock<CopyWorkItem>> m_workItemQueues;
        private BufferBlock<CopyWorkItem> m_currentWorkItemsQueue;

        private Task m_copyTask;

        #endregion

        #region Public methods

        /// <summary>
        /// Enqueue the given buffer to be handled next.
        /// </summary>
        /// <param name="queue"></param>
        public void HandleQueue(BufferBlock<CopyWorkItem> queue)
        {
            if (m_workItemQueues == null)
            {
                m_workItemQueues = new BufferBlock<BufferBlock<CopyWorkItem>>();
            }

            m_workItemQueues.Post(queue);

            if (m_copyTask == null || m_copyTask.IsCompleted)
            {
                m_copyTask = Task.Factory.StartNew(HandleNextQueue);
            }
        }

        #endregion

        #region Private methods

        // TODO: Add some kind of feedback so we can display what we are copying and progress.
        private async Task HandleNextQueue()
        {
            if (m_workItemQueues.Count == 0)
                return;

            m_currentWorkItemsQueue = await m_workItemQueues.ReceiveAsync();

            while (true) // Consume the data until we get the end code
            {
                var workItem = await m_currentWorkItemsQueue.ReceiveAsync();

                // This is our signal that we are done for this queue
                if (workItem.Direction == (CopyDirection.DeleteAtDestination | CopyDirection.ToDestination))
                    break;

                var sourcePath = workItem.SourcePath;
                var destinationPath = workItem.DestinationPath;

                switch (workItem.Direction)
                {
                    case CopyDirection.DeleteAtDestination:
                        if (workItem.IsDirectory)
                            Directory.Delete(destinationPath);
                        else
                            File.Delete(destinationPath);
                        break;

                    case CopyDirection.DeleteAtSource:
                        if (workItem.IsDirectory)
                            Directory.Delete(sourcePath);
                        else
                            File.Delete(sourcePath);
                        break;

                    case CopyDirection.ToDestination:
                        if (workItem.IsDirectory)
                            Directory.CreateDirectory(destinationPath);
                        else
                            File.Copy(sourcePath, destinationPath);
                        break;

                    case CopyDirection.ToSource:
                        if (workItem.IsDirectory)
                            Directory.CreateDirectory(sourcePath);
                        else
                            File.Copy(destinationPath, sourcePath);
                        break;
                }
            }

            HandleNextQueue(); // Fire and forget
        }

        #endregion
    }
}
