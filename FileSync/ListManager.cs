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
    public class ListManager
    {
        #region Classes and enums

        #endregion

        #region Fields

        private static string s_listPath;

        #endregion

        #region Properties

        public static IList<CopyManager.CopyWorkItem> SyncList { get; private set; }

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
            SyncList = new List<CopyManager.CopyWorkItem>();

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

                SyncList.Add(new CopyManager.CopyWorkItem { SourcePath = line[0], DestinationPath = line[1], Direction = (CopyManager.CopyDirection)Byte.Parse(line[2]), IsDirectory = true });
            }
        }

        private void WriteMapingsToFile()
        {
            File.WriteAllLines(s_listPath, SyncList.Select(e => $"{e.SourcePath}|{e.DestinationPath}|{(byte)e.Direction}"));
        }

        #endregion
    }
}
