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

        private void ComputeDifferences(CopyWorkItem workItem, BufferBlock<CopyWorkItem> filesQueue)
        {
            if (!workItem.IsDirectory)
                return; // No code path should lead here

            bool copyToSource = (workItem.Direction & CopyDirection.ToSource) != 0;
            bool copyToDestination = (workItem.Direction & CopyDirection.ToDestination) != 0;
            bool sourceDirectoryExists = Directory.Exists(workItem.SourcePath);
            bool destinationDirectoryExists = Directory.Exists(workItem.DestinationPath);

            if (!sourceDirectoryExists && !destinationDirectoryExists)
                return;

            if (copyToSource)
            {
                // If we copy to source and the current directory doesn't exist, we create it and automatically queue all the content
                if (!sourceDirectoryExists)
                {
                    var copyWorkItem = new CopyWorkItem(workItem);
                    copyWorkItem.Direction = CopyDirection.ToSource;
                    filesQueue.Post(copyWorkItem);

                    var distantDirectories = Directory.EnumerateDirectories(workItem.DestinationPath, "*", SearchOption.AllDirectories);
                    foreach (var directory in distantDirectories)
                    {
                        filesQueue.Post(new CopyWorkItem { SourcePath = directory, DestinationPath = directory, Direction = CopyDirection.ToSource, IsDirectory = true });
                    }

                    var distantFiles = Directory.EnumerateFiles(workItem.DestinationPath, "*", SearchOption.AllDirectories);
                    foreach (var file in distantFiles)
                    {
                        // Generate the "source" version of the file's path
                        var sourcePath = file.Replace(workItem.DestinationPath, workItem.SourcePath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToSource, IsDirectory = false });
                    }

                    return;
                }
            }

            if (copyToDestination)
            {
                // If we copy to destination and the current directory doesn't exist, we create it
                if (!destinationDirectoryExists)
                {
                    var copyWorkItem = new CopyWorkItem(workItem);
                    copyWorkItem.Direction = CopyDirection.ToDestination;
                    filesQueue.Post(copyWorkItem);

                    var sourceDirectories = Directory.EnumerateDirectories(workItem.SourcePath, "*", SearchOption.AllDirectories);
                    foreach (var directory in sourceDirectories)
                    {
                        filesQueue.Post(new CopyWorkItem { SourcePath = directory, DestinationPath = directory, Direction = CopyDirection.ToDestination, IsDirectory = true });
                    }

                    var sourceFiles = Directory.EnumerateFiles(workItem.SourcePath, "*", SearchOption.AllDirectories);
                    foreach (var file in sourceFiles)
                    {
                        // Generate the "source" version of the file's path
                        var destinationPath = file.Replace(workItem.SourcePath, workItem.DestinationPath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = file, DestinationPath = destinationPath, Direction = CopyDirection.ToDestination, IsDirectory = false });
                    }

                    return;
                }
            }
        }

        #endregion
    }
}
