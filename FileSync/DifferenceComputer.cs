using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FileSync
{
    /// <summary>
    /// This class will handle the difference finding logic between source and destination directories.
    /// It will feed an async queue that can be consumed by the copy module.
    /// </summary>
    public sealed class DifferenceComputer
    {
        #region Singleton pattern

        private static readonly Lazy<DifferenceComputer> lazy =
        new Lazy<DifferenceComputer>(() => new DifferenceComputer());

        public static DifferenceComputer Instance { get { return lazy.Value; } }

        private DifferenceComputer()
        {
        }

        #endregion

        #region Fields

        // TODO: have a reference to the async queue here
        private BufferBlock<CopyManager.CopyWorkItem> m_toCopyQueue;
        private bool m_initialized;

        #endregion

        #region Public methods

        public void Init(BufferBlock<CopyManager.CopyWorkItem> filesQueue)
        {
            m_toCopyQueue = filesQueue;
            m_initialized = true;
        }

        public void ComputeDifferences()
        {
            if (!m_initialized)
                return;


        }

        #endregion
    }
}
