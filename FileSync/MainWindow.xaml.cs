using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static FileSync.CopyManager;
using Res = FileSync.Properties.Resources;

namespace FileSync
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

            ListManager.Init(Res.DefaultMapPath);
            ListManager.Init(@"..\..\..\testList.list"); // TODO: for debugging only, remove.

            UpdateListView();

            m_progressWindow = new ProgressWindow();
        }

        #region Handlers
        
        private void AddMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            var context = DataContext as UIContext;

            if (context == null)
                return;

            context.AddRow("", "", CopyDirection.ToDestination | CopyDirection.ToSource);
            ListManager.AddEntry("", "", CopyDirection.ToDestination | CopyDirection.ToSource);

            MappingsDataGrid.Items.Refresh();

            SetDirty();
        }

        private void RemoveMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            var context = DataContext as UIContext;

            if (context == null)
                return;

            bool success = context.RemoveRow(m_lastSelectedRowIndex);
            success &= ListManager.DeleteEntry(m_lastSelectedRowIndex);

            if (success)
            {
                MappingsDataGrid.Items.Refresh();
                SetDirty();
            }
        }

        private void SyncAllBtn_Click(object sender, RoutedEventArgs e)
        {
            SyncFiles();
        }

        private void FullSyncMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyManager.CopyDirection.ToDestination | CopyManager.CopyDirection.ToSource);
        }

        private void SyncToRightMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyManager.CopyDirection.ToDestination);
        }

        private void SyncToLeftMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyManager.CopyDirection.ToSource);
        }

        private void SyncToRightWithDeletionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyManager.CopyDirection.DeleteAtDestination);
        }

        private void SyncToLeftWithDeletionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeSyncMehtod(m_lastSelectedRowIndex, CopyManager.CopyDirection.DeleteAtSource);
        }

        private void MappingsDataGrid_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var context = DataContext as UIContext;

            if (context == null)
                return;

            if (!MappingsDataGrid.SelectedCells.Any())
                return;

            // We rely on the fact that we can only select ONE cell. If it changes, this will break
            var row = (UIContext.MappingRow)MappingsDataGrid.SelectedCells[0].Item;

            m_lastSelectedRowIndex = context.MappingRows.IndexOf(row);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            ListManager.CommitChanges();

            SetClean();
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
            var context = (DataContext as UIContext);

            context.MappingRows.Clear();

            var list = ListManager.SyncList;

            foreach (var item in list)
            {
                context.AddRow(item.SourcePath, item.DestinationPath, item.Direction);
            }
        }

        private void SyncFiles()
        {
            var queue = new System.Threading.Tasks.Dataflow.BufferBlock<CopyManager.CopyWorkItem>();

            System.Threading.Tasks.Task.Factory.StartNew(() => DifferenceComputer.ComputeDifferences(queue));

            System.Threading.Tasks.Task.Factory.StartNew(() => CopyManager.Instance.HandleQueue(queue));

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
            var context = DataContext as UIContext;

            if (context == null)
                return;

            context.UpdateRow(index, left, right, newDirection);

            ListManager.EditEntry(index, left, right, newDirection);

            SetDirty();
        }

        /// <summary>
        /// Call when a change is made.
        /// </summary>
        private void SetDirty()
        {
            SaveBtn.IsEnabled = true;
        }

        /// <summary>
        /// Call when all changes have been commited.
        /// </summary>
        private void SetClean()
        {
            SaveBtn.IsEnabled = false;
        }

        #endregion
    }
}
