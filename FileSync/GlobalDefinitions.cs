using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync
{
    public static class GlobalDefinitions
    {
        #region Classes and enums

        /// <summary>
        /// Describes the way files must be synced. Described here because it is also used
        /// in the UI to describe the sync icon.
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

        /// <summary>
        /// Represent one copy to be done.
        /// Used by the CopyManager to do the actual copy and by the UI to show a list 
        /// of files to be copied.
        /// </summary>
        public class CopyWorkItem
        {
            #region Properties

            public string SourcePath { get; set; }
            public string DestinationPath { get; set; }
            public bool IsDirectory { get; set; }

            public CopyDirection Direction { get; set; }

            /// <summary>
            /// Number of bytes. Only used when notifying the UI.
            /// Represents the file's size if CopyWorkItem is used to fill the initial files to copy list.
            /// Represents the file's copied size if CopyWorkItem is used to give ongoing copy feedback.
            /// </summary>
            public long Size { get; set; }

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
                this.Size = copyWorkItem.Size;
            }

            #endregion
        }

        #endregion

        #region Constants

        public static CopyDirection StopCode = (CopyDirection.DeleteAtDestination | CopyDirection.ToDestination);
        public static long LongStopCode = -1;

        #endregion
    }
}
