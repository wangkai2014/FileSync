using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static FileSync.GlobalDefinitions;
using static FileSync.Core.CopyManager;

namespace FileSync.Core
{
    /// <summary>
    /// Main class used to manage the syncing list.
    /// TODO: Consider having multiple lists. Shouldn't be needed in the initial use case of this program.
    /// </summary>
    public class ListManager
    {
        #region Classes and enums

        #endregion

        #region Fields

        // NEVER USE! Use property.
        private static bool s_isDirty;

        private static string s_listPath;

        private static IList<CopyWorkItem> s_syncList;

        #endregion

        #region Properties

        public static IList<CopyWorkItem> SyncList => s_syncList.ToList(); // Return a copy

        /// <summary>
        /// True if the list has uncommited changes.
        /// </summary>
        public static bool IsDirty
        {
            get
            {
                return s_isDirty;
            }
            private set
            {
                s_isDirty = value;
                OnIsDirtyChanged(new EventArgs()); // Actually a bad practice, could take long.
            }
        }

        #endregion

        #region Event handlers

        public static event EventHandler IsDirtyChanged;

        #endregion

        #region Public methods

        public static void Init(string listPath = null)
        {
            s_listPath = String.IsNullOrEmpty(listPath) ? "mappings.list" : listPath;

            LoadList();
        }

        /// <summary>
        /// Adds an entry to the map list, but does not save the list to disk.
        /// Call CommitChanges to save the list.
        /// </summary>
        /// <param name="sourcePath">Must not be a directory.</param>
        /// <param name="destinationPath">Must not be a directory.</param>
        /// <param name="direction"></param>
        public static void AddEntry(string sourcePath, string destinationPath, CopyDirection direction)
        {
            s_syncList.Add(new CopyWorkItem { SourcePath = sourcePath, DestinationPath = destinationPath, Direction = direction, IsDirectory = true });
            IsDirty = true;
        }

        /// <summary>
        /// Removes an entry. Index must be valid in an up to date copy of SyncList.
        /// Does not save the list to disk.
        /// Call CommitChanges to save the list.
        /// </summary>
        /// <param name="index">Index of the element to remove.</param>
        public static bool DeleteEntry(int index)
        {
            if (index < 0 || index >= s_syncList.Count)
                return false;

            s_syncList.RemoveAt(index);
            IsDirty = true;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sourcePath">Not changed if null.</param>
        /// <param name="destinationPath">Not changed if null.</param>
        /// <param name="copyDirection">Optional.</param>
        public static bool EditEntry(int index, string sourcePath, string destinationPath, CopyDirection copyDirection = CopyDirection.ToDestination | CopyDirection.DeleteAtDestination)
        {
            if (index < 0 || index >= s_syncList.Count)
                return false;

            if (copyDirection != (CopyDirection.ToDestination | CopyDirection.DeleteAtDestination))
                s_syncList[index].Direction = copyDirection;
                
            if (!String.IsNullOrEmpty(sourcePath))
                s_syncList[index].SourcePath = sourcePath;

            if (!String.IsNullOrEmpty(destinationPath))
                s_syncList[index].DestinationPath = destinationPath;

            IsDirty = true;

            return true;
        }

        /// <summary>
        /// Sounds fancy but actually just flushes the content of the list to the file.
        /// </summary>
        /// <returns>False if no changes were made.</returns>
        public static bool CommitChanges()
        {
            if (!IsDirty)
                return false;

            File.WriteAllLines(s_listPath, s_syncList.Select(m => $"{m.SourcePath}|{m.DestinationPath}|{(byte)m.Direction}"));

            IsDirty = false;

            return true;
        }

        #endregion

        #region Private methods

        private static void LoadList()
        {
            s_syncList = new List<CopyWorkItem>();

            if (!File.Exists(s_listPath))
            {
                File.Create(s_listPath).Close();
                return;
            }

            var lines = File.ReadAllLines(s_listPath).Select(line => line.Split('|'));

            foreach (var line in lines)
            {
                if (line.Length != 3)
                    continue;

                s_syncList.Add(new CopyWorkItem { SourcePath = line[0], DestinationPath = line[1], Direction = (CopyDirection)Byte.Parse(line[2]), IsDirectory = true });
            }
        }

        private void WriteMapingsToFile()
        {
            File.WriteAllLines(s_listPath, s_syncList.Select(e => $"{e.SourcePath}|{e.DestinationPath}|{(byte)e.Direction}"));
        }

        protected static void OnIsDirtyChanged(EventArgs e)
        {
            IsDirtyChanged?.Invoke(null, e);
        }

        #endregion
    }
}
