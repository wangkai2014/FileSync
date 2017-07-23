using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using static FileSync.GlobalDefinitions;
using Res = FileSync.Properties.Resources;

namespace FileSync.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private ProgressWindow m_progressWindow;

        private int m_lastSelectedRowIndex = -1;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            CoreController.Instance.Init();

            UpdateListView();
        }

        #region Handlers
        
        private void AddMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            var context = DataContext as MainWindowPresenter;

            if (context == null)
                return;

            context.AddRow("", "", CopyDirection.ToDestination | CopyDirection.ToSource);
            CoreController.Instance.AddEntryToList("", "", CopyDirection.ToDestination | CopyDirection.ToSource);

            MappingsDataGrid.Items.Refresh();
        }

        private void RemoveMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            var context = DataContext as MainWindowPresenter;

            if (context == null)
                return;

            bool success = context.RemoveRow(m_lastSelectedRowIndex);
            success &= CoreController.Instance.DeleteEntryFromList(m_lastSelectedRowIndex);

            if (success)
            {
                MappingsDataGrid.Items.Refresh();
            }
        }

        private void SyncAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var feedbackQueue = new BufferBlock<Tuple<long, bool>>();
            var fileToCopyQueue = new BufferBlock<CopyWorkItem>();
            CoreController.Instance.StartSync(fileToCopyQueue, feedbackQueue);

            m_progressWindow = new ProgressWindow();
            m_progressWindow.Init(fileToCopyQueue, feedbackQueue);
            m_progressWindow.Show();
        }

        private void FullSyncMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyDirection.ToDestination | CopyDirection.ToSource);
        }

        private void SyncToRightMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyDirection.ToDestination);
        }

        private void SyncToLeftMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyDirection.ToSource);
        }

        private void SyncToRightWithDeletionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyDirection.DeleteAtDestination);
        }

        private void SyncToLeftWithDeletionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyDirection.DeleteAtSource);
        }

        private void MappingsDataGrid_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var context = DataContext as MainWindowPresenter;

            if (context == null)
                return;

            if (!MappingsDataGrid.SelectedCells.Any())
                return;

            // We rely on the fact that we can only select ONE cell. If it changes, this will break
            var row = (MainWindowPresenter.MappingRow)MappingsDataGrid.SelectedCells[0].Item;

            m_lastSelectedRowIndex = context.MappingRows.IndexOf(row);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            CoreController.Instance.CommitChangesToListIfAny();
        }

        private void LeftTextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed || e.ClickCount != 2)
                return;

            ChangePaths(m_lastSelectedRowIndex, GetPathFromUser(), null);
        }

        private void RightTextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed || e.ClickCount != 2)
                return;

            ChangePaths(m_lastSelectedRowIndex, null, GetPathFromUser());
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Private methods

        private void UpdateListView()
        {
            var context = (DataContext as MainWindowPresenter);

            context.MappingRows.Clear();

            var list = CoreController.Instance.DirectoryMappingList;

            foreach (var item in list)
            {
                context.AddRow(item.SourcePath, item.DestinationPath, item.Direction);
            }
        }
        
        private string GetPathFromUser(string description = null)
        {
            description = description ?? Res.DefaultDirectoryBrowserDescription;
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = description;
            dialog.ShowDialog();

            return dialog.SelectedPath;
        }

        private void ChangeSyncMehtod(int index, CopyDirection newCopyDirection)
        {
            UpdateEntry(index, null, null, newCopyDirection);
        }

        private void ChangePaths(int index, string left, string right)
        {
            UpdateEntry(index, left, right, CopyDirection.ToDestination | CopyDirection.DeleteAtDestination);
        }

        private void UpdateEntry(int index, string left, string right, CopyDirection newDirection)
        {
            var context = DataContext as MainWindowPresenter;

            if (context == null)
                return;

            context.UpdateRow(index, left, right, newDirection);
            CoreController.Instance.EditListEntry(index, left, right, newDirection);
        }

        #endregion

        private void ShowLogsButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(LogsDirectory);
        }
    }
}
