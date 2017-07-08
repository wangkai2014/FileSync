using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileSync.Core;
using System.Threading.Tasks.Dataflow;
using static FileSync.GlobalDefinitions;
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

        public BufferBlock<Tuple<long, bool>> StartSync(BufferBlock<CopyWorkItem> queueToUI, BufferBlock<Tuple<long, bool>> feedbackQueue)
        {
            // TODO: do the appropriate checks and finish the job.

            // No need to keep this reference, once the copy is started, we can forget about it.
            var queueToCopyManager = new BufferBlock<CopyWorkItem>();

            var broadcastBlock = new BroadcastBlock<CopyWorkItem>(item => item);
            broadcastBlock.LinkTo(queueToCopyManager);
            broadcastBlock.LinkTo(queueToUI);

            Task.Factory.StartNew(() => DifferenceComputer.ComputeDifferences(broadcastBlock));

            Task.Factory.StartNew(() => CopyManager.Instance.HandleQueue(queueToCopyManager, feedbackQueue));

            return feedbackQueue;
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

        public void UnsubscribeToListDirtinessChanges(EventHandler e)
        {
            ListManager.IsDirtyChanged -= e;
        }

        #endregion
    }
}
