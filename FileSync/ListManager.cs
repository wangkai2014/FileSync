using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync
{
    /// <summary>
    /// Main class used to manage the syncing list.
    /// TODO: Consider having multiple lists. Shouldn't be needed in the initial use case of this program.
    /// </summary>
    class ListManager
    {
        #region Classes and enums

        /// <summary>
        /// Describes the way files must be synced.
        /// TODO: Invalid values are possible (eg: ToDestination | ToDestinationWithDeletion). Might need fixing later.
        /// </summary>
        public enum CopyDirection
        {
            ToDestination = 1,
            ToSource = 2,
            ToDestinationWithDeletion = 4,
            ToSourceWithDeletion = 8
        }

        /// <summary>
        /// Representation of a path pair, with all the specific sync settings.
        /// TODO: Find a better name... Please...
        /// </summary>
        public class SyncElement
        {
            public string SourcePath;
            public string DestinationPath;

            public CopyDirection Direction;
        }

        #endregion

        #region Fields

        private static string s_listPath;

        private static IList<SyncElement> s_syncList;

        #endregion

        #region Public methods

        public static void Init(string listPath = null)
        {
            s_listPath = String.IsNullOrEmpty(listPath) ? "mappings.list" : listPath;

            LoadList();
        }

        #endregion

        #region Private methods

        private static void LoadList()
        {
            s_syncList = new List<SyncElement>();

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

                s_syncList.Add(new SyncElement { SourcePath = line[0], DestinationPath = line[1], Direction = (CopyDirection)Byte.Parse(line[3]) });
            }
        }

        #endregion
    }
}
