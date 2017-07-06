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

        #endregion
    }
}
