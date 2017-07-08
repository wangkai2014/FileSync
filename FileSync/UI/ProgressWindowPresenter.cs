using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static FileSync.GlobalDefinitions;

namespace FileSync.UI
{
    class ProgressWindowPresenter : INotifyPropertyChanged
    {
        #region Fields

        private double m_fullCopySize;
        private double m_currentFileSize;
        private double m_fullCopySizeCopied;
        private double m_currentFileSizeCopied;

        private BufferBlock<CopyWorkItem> m_filesToCopyQueue;
        private BufferBlock<Tuple<long, bool>> m_feedbackQueue; // Item1 is copied size, Item2 is IsCompleted

        #endregion

        #region Properties

        public Queue<CopyWorkItem> FilesToBeCopied { get; private set; }

        public string CurrentFileName { get; private set; }

        public string FullCopySize { get; private set; }

        public string CurrentFileSize { get; private set; }

        public string FullCopySizeCopied { get; private set; }

        public string CurrentFileSizeCopied { get; private set; }

        public int SingleFileProgress { get; private set; }

        public int FullCopyProgress { get; private set; }

        #endregion

        #region Event handlers

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public void Init(BufferBlock<CopyWorkItem> fillingQueue, BufferBlock<Tuple<long, bool>> feedbackQueue)
        {
            FilesToBeCopied = new Queue<CopyWorkItem>();
            m_filesToCopyQueue = fillingQueue;
            m_feedbackQueue = feedbackQueue;
            CurrentFileName = "--";
            UpdateProgressDisplay();

            ReceiveFilesList(); // Fire and forget
            ListenFeedback(); // Fire and forget
        }

        #endregion

        #region Private methods

        private void UpdateProgressDisplay()
        {
            FullCopySize = $"{m_fullCopySize / 1024} MB";
            CurrentFileSize = $"{m_currentFileSize / 1024} MB";
            FullCopySizeCopied = $"{m_fullCopySizeCopied / 1024} MB";
            CurrentFileSizeCopied = $"{m_currentFileSizeCopied / 1024} MB";

            SingleFileProgress = m_currentFileSize == 0 ? 0 : (int)(m_currentFileSizeCopied * 100 / m_currentFileSize);
            FullCopyProgress = m_fullCopySize == 0 ? 0 : (int)(m_fullCopySizeCopied * 100 / m_fullCopySize);

            NotifyPropertyChanged("FullCopySize");
            NotifyPropertyChanged("CurrentFileSize");
            NotifyPropertyChanged("FullCopySizeCopied");
            NotifyPropertyChanged("CurrentFileSizeCopied");

            NotifyPropertyChanged("SingleFileProgress");
            NotifyPropertyChanged("FullCopyProgress");
        }

        private async void ReceiveFilesList()
        {
            var item = await m_filesToCopyQueue.ReceiveAsync();

            while (item.Direction != StopCode)
            {
                EnqueueFile(item);

                item = await m_filesToCopyQueue.ReceiveAsync();
            }
        }

        private async void ListenFeedback()
        {
            var item = await m_feedbackQueue.ReceiveAsync();

            while (item.Item1 != LongStopCode)
            {
                NotifyCopyProgress(item.Item1, item.Item2);

                item = await m_feedbackQueue.ReceiveAsync();
            }
        }

        private void EnqueueFile(CopyWorkItem item)
        {
            bool showFirst = !FilesToBeCopied.Any();

            FilesToBeCopied.Enqueue(item);
            m_fullCopySize += item.Size;

            NotifyPropertyChanged("FilesToBeCopied");
            UpdateProgressDisplay();

            if (showFirst)
                PeekNextFile();
        }

        private void DequeueFile()
        {
            FilesToBeCopied.Dequeue();
            NotifyPropertyChanged("FilesToBeCopied");
            PeekNextFile();
        }

        private void PeekNextFile()
        {
            if (!FilesToBeCopied.Any())
                return;

            m_fullCopySizeCopied += m_currentFileSize;
            var nextFile = FilesToBeCopied.Peek();
            m_currentFileSize = nextFile.Size;
            CurrentFileName = Path.GetFileName(nextFile.SourcePath);
            m_currentFileSizeCopied = 0;

            NotifyPropertyChanged("CurrentFileName");
            UpdateProgressDisplay();
        }

        private void NotifyCopyProgress(long copiedSize, bool isDone)
        {
            m_currentFileSizeCopied = copiedSize;
            UpdateProgressDisplay();

            if (isDone)
                DequeueFile();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
