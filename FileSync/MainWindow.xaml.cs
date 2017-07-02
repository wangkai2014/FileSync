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

        private void SyncIcon_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
