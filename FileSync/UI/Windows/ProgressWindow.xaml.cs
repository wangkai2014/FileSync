using System;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using static FileSync.GlobalDefinitions;

namespace FileSync.UI.Windows
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void Init(BufferBlock<CopyWorkItem> fillingQueue, BufferBlock<Tuple<long, bool>> feedbackQueue)
        {
            var progressContext = DataContext as ProgressWindowPresenter;
            if (progressContext != null)
                progressContext.Init(fillingQueue, feedbackQueue, FilesToCopyDataGrid);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // TODO: Show warning and cancel the copy if user agrees (implement cancellation).
        }
    }
}
