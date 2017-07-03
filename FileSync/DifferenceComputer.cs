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
        public static void ComputeDifferences(BufferBlock<CopyWorkItem> filesQueue)
        {
            if (ListManager.SyncList == null)
                throw new InvalidOperationException("ListManager is not initialized correctly.");

            var list = ListManager.SyncList;

            foreach (var element in list)
            {
                ComputeDifferences(element, filesQueue);
            }

            // Once we are done, we send the "end of queue signal"
            filesQueue.Post(new CopyWorkItem { Direction = (CopyDirection.DeleteAtDestination | CopyDirection.ToDestination) });
        }

        #endregion

        #region Private methods

        private static void ComputeDifferences(CopyWorkItem workItem, BufferBlock<CopyWorkItem> filesQueue)
        {
            if (!workItem.IsDirectory)
                return; // No code path should lead here

            // -------------- BUG FIX ---------------------
            // Access to System Volume Information at the root of a removable device is always denied.
            // TODO: When black lists are implemented, use that to fix this bug more cleanely.
            if (workItem.SourcePath.Contains("System Volume Information") || workItem.DestinationPath.Contains("System Volume Information"))
                return;
            // --------------------------------------------

            // -------------- BUG FIX ---------------------
            // If one of the paths was a root and not the other, it ended with a '\' (and not the other)
            // and the Replace later didn't make sense. These two new strings will be used for the Replace
            // operations only.
            var dirInfo = new DirectoryInfo(workItem.DestinationPath);
            var destPath = dirInfo.Parent == null ? workItem.DestinationPath.Replace("\\", "") : workItem.DestinationPath;
            dirInfo = new DirectoryInfo(workItem.SourcePath);
            var srcPath = dirInfo.Parent == null ? workItem.SourcePath.Replace("\\", "") : workItem.SourcePath;
            // --------------------------------------------

            bool copyToSource = (workItem.Direction & CopyDirection.ToSource) != 0;
            bool copyToDestination = (workItem.Direction & CopyDirection.ToDestination) != 0;
            bool sourceDirectoryExists = Directory.Exists(workItem.SourcePath);
            bool destinationDirectoryExists = Directory.Exists(workItem.DestinationPath);

            if (!sourceDirectoryExists && !destinationDirectoryExists)
                return;

            if (!copyToSource && !copyToDestination) // ....
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
                        // Generate the "source" version of the directory's path
                        var sourcePath = directory.Replace(destPath, srcPath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = sourcePath, Direction = CopyDirection.ToSource, IsDirectory = true });
                    }

                    var distantFiles = Directory.EnumerateFiles(workItem.DestinationPath, "*", SearchOption.AllDirectories);
                    foreach (var file in distantFiles)
                    {
                        // Generate the "source" version of the file's path
                        var sourcePath = file.Replace(destPath, srcPath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToSource, IsDirectory = false });
                    }

                    return;
                }
            }

            if (copyToDestination)
            {
                // If we copy to destination and the current directory doesn't exist, we create it and automatically queue all the content
                if (!destinationDirectoryExists)
                {
                    var copyWorkItem = new CopyWorkItem(workItem);
                    copyWorkItem.Direction = CopyDirection.ToDestination;
                    filesQueue.Post(copyWorkItem);

                    var sourceDirectories = Directory.EnumerateDirectories(workItem.SourcePath, "*", SearchOption.AllDirectories);
                    foreach (var directory in sourceDirectories)
                    {
                        // Generate the "source" version of the directory's path
                        var sourcePath = directory.Replace(destPath, srcPath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = sourcePath, Direction = CopyDirection.ToDestination, IsDirectory = true });
                    }

                    var sourceFiles = Directory.EnumerateFiles(workItem.SourcePath, "*", SearchOption.AllDirectories);
                    foreach (var file in sourceFiles)
                    {
                        // Generate the "destination" version of the file's path
                        var destinationPath = file.Replace(srcPath, destPath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = file, DestinationPath = destinationPath, Direction = CopyDirection.ToDestination, IsDirectory = false });
                    }

                    return;
                }
            }

            // If we made it here, both directories exist and we have to check files one by one and decide which should be copied.
            // TODO: find a more efficient way to do that, I don't like it.
            var filesInSource = Directory.GetFiles(workItem.SourcePath, "*", SearchOption.TopDirectoryOnly).Select(path => path.Replace(srcPath, destPath));
            var filesInDestination = Directory.GetFiles(workItem.DestinationPath, "*", SearchOption.TopDirectoryOnly);

            var commonFiles = filesInSource.Where(f => filesInDestination.Contains(f));

            var sourceFilesToCopy = filesInSource.Where(f => !commonFiles.Contains(f));
            var destinationFilesToCopy = filesInDestination.Where(f => !commonFiles.Contains(f));
            // TODO: Actually, I hate it...

            if (copyToDestination)
            {
                foreach (var file in sourceFilesToCopy)
                {
                    var sourcePath = file.Replace(destPath, srcPath);
                    filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToDestination, IsDirectory = false });
                }
            }

            if (copyToSource)
            {
                foreach (var file in destinationFilesToCopy)
                {
                    var sourcePath = file.Replace(destPath, srcPath);
                    filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToSource, IsDirectory = false });
                }
            }

            // Now we handle the directories, recursively.
            // Note that the path.replace is to allow for a good comparison between source and destination
            var directoriesInSource = Directory.GetDirectories(workItem.SourcePath, "*", SearchOption.TopDirectoryOnly).Select(path => path.Replace(srcPath, destPath));
            var directoriesInDestination = Directory.GetDirectories(workItem.DestinationPath, "*", SearchOption.TopDirectoryOnly);

            var commonDirectories = directoriesInSource.Where(f => directoriesInDestination.Contains(f));

            var uniqueDestinationDirectories = directoriesInDestination.Where(f => !commonDirectories.Contains(f));

            foreach (var directory in directoriesInSource)
            {
                var sourcePath = directory.Replace(destPath, srcPath);
                ComputeDifferences(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = directory, Direction = workItem.Direction, IsDirectory = true }, filesQueue);
            }

            foreach (var directory in uniqueDestinationDirectories)
            {
                var sourcePath = directory.Replace(destPath, srcPath);
                ComputeDifferences(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = directory, Direction = workItem.Direction, IsDirectory = true }, filesQueue);
            }
        }

        #endregion
    }
}
