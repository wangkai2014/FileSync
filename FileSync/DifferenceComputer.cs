using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static FileSync.CopyManager;

namespace FileSync
{
    /// <summary>
    /// This class will handle the difference finding logic between source and destination directories.
    /// It will feed an async queue that can be consumed by the copy module.
    /// </summary>
    public class DifferenceComputer
    {
        #region Fields



        #endregion

        #region Public methods
        
        /// <summary>
        /// Will compute the differences based on the static ListManager and will
        /// fill the queue given as a parameter with the resulting copy work items.
        /// </summary>
        /// <param name="filesQueue"></param>
        public void ComputeDifferences(BufferBlock<CopyManager.CopyWorkItem> filesQueue)
        {
            if (ListManager.SyncList == null)
                throw new InvalidOperationException("ListManager is not initialized correctly.");

            foreach (var element in ListManager.SyncList)
            {
                ComputeDifferences(element, filesQueue);
            }

        }

        #endregion

        #region Private methods

        private void ComputeDifferences(CopyWorkItem element, BufferBlock<CopyWorkItem> filesQueue)
        {
            // If we copy to source and the directory doesn't exist, we create it
            if (!Directory.Exists(element.SourcePath) && (element.Direction & CopyDirection.ToSource) != 0)
            {
                var copyWorkItem = new CopyWorkItem { Direction = element.Direction, DestinationPath = element.DestinationPath, SourcePath = element.SourcePath, IsDirectory = true};
                copyWorkItem.Direction = CopyDirection.ToSource;
                filesQueue.Post(copyWorkItem);
            }
            // If we copy to destination and the directory doesn't exist, we create it
            if (!Directory.Exists(element.DestinationPath) && (element.Direction & CopyDirection.ToDestination) != 0)
            {
                var copyWorkItem = new CopyWorkItem { Direction = element.Direction, DestinationPath = element.DestinationPath, SourcePath = element.SourcePath, IsDirectory = true};
                copyWorkItem.Direction = CopyDirection.ToDestination;
                filesQueue.Post(copyWorkItem);
            }
        }

        #endregion
    }
}
