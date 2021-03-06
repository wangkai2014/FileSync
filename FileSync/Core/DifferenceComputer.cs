﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using static FileSync.GlobalDefinitions;

namespace FileSync.Core
{
    /// <summary>
    /// This class will handle the difference finding logic between source and destination directories.
    /// It will feed an async queue that can be consumed by the copy module.
    /// </summary>
    public class DifferenceComputer
    {
        #region Fields

        private static Logger s_logger;

        #endregion

        #region Public methods

        /// <summary>
        /// Will compute the differences based on the static ListManager and will
        /// fill the queue given as a parameter with the resulting copy work items.
        /// </summary>
        /// <param name="filesQueue"></param>
        public static void ComputeDifferences(BroadcastBlock<CopyWorkItem> filesQueue)
        {
            if (ListManager.SyncList == null)
                throw new InvalidOperationException("ListManager is not initialized correctly.");

            if (s_logger == null)
                s_logger = new Logger("DifferenceComputer");

            var list = ListManager.SyncList;

            try
            {
                foreach (var element in list)
                {
                    ComputeDifferences(element, filesQueue);
                }
            }
            catch (Exception ex)
            {
                s_logger.AppendException(ex);
            }

            // Once we are done, we send the "end of queue signal"
            filesQueue.Post(new CopyWorkItem { Direction = StopCode });
        }

        #endregion

        #region Private methods

        private static void ComputeDifferences(CopyWorkItem workItem, BroadcastBlock<CopyWorkItem> filesQueue)
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

            s_logger.AppendInfo("Analyzing " + workItem.ToString());

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
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToSource, IsDirectory = false, Size = fileInfo.Length });
                        }
                        catch (Exception ex)
                        {
                            s_logger.AppendException(ex);
                        }
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
                        // Generate the "destination" version of the directory's path
                        var sourcePath = directory.Replace(srcPath, destPath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = sourcePath, Direction = CopyDirection.ToDestination, IsDirectory = true });
                    }

                    var sourceFiles = Directory.EnumerateFiles(workItem.SourcePath, "*", SearchOption.AllDirectories);
                    foreach (var file in sourceFiles)
                    {
                        // Generate the "destination" version of the file's path
                        var destinationPath = file.Replace(srcPath, destPath);
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            filesQueue.Post(new CopyWorkItem { SourcePath = file, DestinationPath = destinationPath, Direction = CopyDirection.ToDestination, IsDirectory = false, Size = fileInfo.Length });
                        }
                        catch (Exception ex)
                        {
                            s_logger.AppendException(ex);
                        }
                    }

                    return;
                }
            }

            // If we made it here, both directories exist and we have to check files one by one and decide which should be copied.
            var filesInSource = new HashSet<string>(Directory.GetFiles(workItem.SourcePath, "*", SearchOption.TopDirectoryOnly).Select(path => path.Replace(srcPath, destPath)));
            var filesInDestination = new HashSet<string>(Directory.GetFiles(workItem.DestinationPath, "*", SearchOption.TopDirectoryOnly));

            var commonFiles = GetCommon(filesInSource, filesInDestination);

            // TODO: evaluate if it is better to compare source to destination and vice versa rather than using commonFiles
            var sourceFilesToCopy = GetUniqueLeft(filesInSource, commonFiles);
            var destinationFilesToCopy = GetUniqueLeft(filesInDestination, commonFiles);

            if (copyToDestination)
            {
                foreach (var file in sourceFilesToCopy)
                {
                    var sourcePath = file.Replace(destPath, srcPath);
                    try
                    {
                        var fileInfo = new FileInfo(sourcePath);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToDestination, IsDirectory = false, Size = fileInfo.Length });
                    }
                    catch (Exception ex)
                    {
                        s_logger.AppendException(ex);
                    }
                }
            }

            if (copyToSource)
            {
                foreach (var file in destinationFilesToCopy)
                {
                    var sourcePath = file.Replace(destPath, srcPath);
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        filesQueue.Post(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = file, Direction = CopyDirection.ToSource, IsDirectory = false, Size = fileInfo.Length });
                    }
                    catch (Exception ex)
                    {
                        s_logger.AppendException(ex);
                    }
                }
            }

            // Now we handle the directories, recursively.
            // Note that the path.replace is to allow for a good comparison between source and destination
            var directoriesInSource = Directory.GetDirectories(workItem.SourcePath, "*", SearchOption.TopDirectoryOnly).Select(path => path.Replace(srcPath, destPath));
            var directoriesInDestination = Directory.GetDirectories(workItem.DestinationPath, "*", SearchOption.TopDirectoryOnly);

            var uniqueDestinationDirectories = GetUniqueLeft(directoriesInDestination, directoriesInSource);

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

        private static IEnumerable<string> GetCommon(IEnumerable<string> leftFiles, IEnumerable<string> rightFiles)
        {
            return GetSubset(leftFiles, rightFiles, false);
        }

        private static IEnumerable<string> GetUniqueLeft(IEnumerable<string> leftFiles, IEnumerable<string> rightFiles)
        {
            return GetSubset(leftFiles, rightFiles, true);
        }

        /// <summary>
        /// Calculates the intersection between two collections of strings.
        /// </summary>
        /// <param name="leftFiles"></param>
        /// <param name="rightFiles"></param>
        /// <param name="getUniqueInLeft">If true, it is not the intersection that is returned but the left minus the intersection.</param>
        /// <returns></returns>
        private static IEnumerable<string> GetSubset(IEnumerable<string> leftFiles, IEnumerable<string> rightFiles, bool getUniqueInLeft)
        {
            // TODO: if the functionnality is implemented, add file size/MD5 comparison here.
            IEnumerable<string> commonFiles;
            if (getUniqueInLeft)
                commonFiles = leftFiles.AsParallel().Where(f => !rightFiles.Contains(f));
            else
                commonFiles = leftFiles.AsParallel().Where(f => rightFiles.Contains(f));

            return commonFiles;
        }

        #endregion
    }
}
