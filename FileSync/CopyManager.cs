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
    /// TODO: To have more control and display a more accurate progress based on size, implement our own stream copier.
    /// </summary>
    public class CopyManager
    {
        #region Classes and enums
        
        public class CopyWorkItem
        {
            public ListManager.SyncElement SyncElement;
            public bool IsDirectory; // True if the paths in SyncElement are directories
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

            while (m_currentWorkItemsQueue.Count != 0)
            {
                var workItem = await m_currentWorkItemsQueue.ReceiveAsync();

                var sourcePath = workItem.SyncElement.SourcePath;
                var destinationPath = workItem.SyncElement.DestinationPath;

                switch (workItem.SyncElement.Direction)
                {
                    case ListManager.CopyDirection.DeleteAtDestination:
                        if (workItem.IsDirectory)
                            Directory.Delete(destinationPath);
                        else
                            File.Delete(destinationPath);
                        break;

                    case ListManager.CopyDirection.DeleteAtSource:
                        if (workItem.IsDirectory)
                            Directory.Delete(sourcePath);
                        else
                            File.Delete(sourcePath);
                        break;

                    case ListManager.CopyDirection.ToDestination:
                        if (workItem.IsDirectory)
                            Directory.CreateDirectory(destinationPath);
                        else
                            File.Copy(sourcePath, destinationPath);
                        break;

                    case ListManager.CopyDirection.ToSource:
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
