using System;
using System.Windows;
using System.Windows.Controls;
using Res = FileSync.Properties.Resources;

namespace FileSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProgressWindow m_progressWindow;

        private int m_lastSelectedRowIndex;

        public MainWindow()
        {
            InitializeComponent();

            ListManager.Init(Res.DefaultMapPath);
            ListManager.Init(@"..\..\..\testList.list"); // TODO: for debugging only, remove.

            UpdateListView();

            m_progressWindow = new ProgressWindow();
        }

        #region Handlers

        private void LoadOtherListBtn_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AddMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RemoveMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SyncAllBtn_Click(object sender, RoutedEventArgs e)
        {
            SyncFiles();
        }
        
        private void LeftPathsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RightPathsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

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

        #endregion

        #region Private methods

        private void SyncFiles()
        {
            var queue = new System.Threading.Tasks.Dataflow.BufferBlock<CopyManager.CopyWorkItem>();

            System.Threading.Tasks.Task.Factory.StartNew(() => DifferenceComputer.ComputeDifferences(queue));

            System.Threading.Tasks.Task.Factory.StartNew(() => CopyManager.Instance.HandleQueue(queue));

        }

        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
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

        private void ChangeSyncMehtod(int index, CopyManager.CopyDirection newCopyDirection)
        {
            var context = this.DataContext as UIContext;

            if (context == null)
                return;

            context.UpdateRowDirectionIcon(index, newCopyDirection);

            ListManager.EditEntry(index, null, null, newCopyDirection);

            SaveBtn.IsEnabled = true;
        }

        private void MappingsDataGrid_Selected(object sender, RoutedEventArgs e)
        {
            m_lastSelectedRowIndex = MappingsDataGrid.SelectedIndex;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            ListManager.CommitChanges();

            SaveBtn.IsEnabled = false;
        }

        private void LeftTextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed || e.ClickCount != 2)
                return;

            Application.Current.Shutdown();
        }

        private void RightTextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed || e.ClickCount != 2)
                return;

            Application.Current.Shutdown();
        }
    }
}
