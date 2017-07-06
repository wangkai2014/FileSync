using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileSync.Core;
using static FileSync.GlobalDefinitions;
using static FileSync.Core.CopyManager;
using Res = FileSync.Properties.Resources;

namespace FileSync
{
    /// <summary>
    /// Used as an interface between the UI and the core elements of the program.
    /// </summary>
    class CoreController
    {
        #region Singleton pattern

        private static readonly Lazy<CoreController> lazy =
        new Lazy<CoreController>(() => new CoreController());

        public static CoreController Instance { get { return lazy.Value; } }

        private CoreController()
        {
        }

        #endregion

        #region Properties

        public IList<CopyWorkItem> DirectoryMappingList => ListManager.SyncList;

        public bool IsListDirty => ListManager.IsDirty;

        #endregion

        #region Public methods

        public void Init()
        {
            ListManager.Init(Res.DefaultMapPath);
        }

        public void StartSync()
        {
            // TODO: do the appropriate checks and finish the job.

            // No need to keep this reference, once the copy is started, we can forget about it.
            var queue = new System.Threading.Tasks.Dataflow.BufferBlock<CopyWorkItem>();

            Task.Factory.StartNew(() => DifferenceComputer.ComputeDifferences(queue));

            Task.Factory.StartNew(() => CopyManager.Instance.HandleQueue(queue));
        }

        public void AddEntryToList(string sourcePath, string destinationPath, CopyDirection direction)
        {
            ListManager.AddEntry("", "", CopyDirection.ToDestination | CopyDirection.ToSource);
        }

        public bool DeleteEntryFromList(int index)
        {
            return ListManager.DeleteEntry(index);
        }

        public bool EditListEntry(int index, string left, string right, CopyDirection newDirection)
        {
            return ListManager.EditEntry(index, left, right, newDirection);
        }

        public bool CommitChangesToListIfAny()
        {
            return ListManager.CommitChanges();
        }

        public void SubscribeToListDirtinessChanges(EventHandler e)
        {
            ListManager.IsDirtyChanged += e;
        }

        #endregion
    }
}
