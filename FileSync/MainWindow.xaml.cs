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

        private void LoadOtherListBtnBtn_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AddMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            //var dialog = new System.Windows.Forms.FolderBrowserDialog();
            //dialog.Description = "Select the directory on your INTERNAL drive";
            //dialog.ShowDialog();
            //if (String.IsNullOrEmpty(dialog.SelectedPath))
            //    return;
            //var paths = new PathPair();
            //paths.LocalPathOrSource = dialog.SelectedPath;
            //dialog.Description = "Select the directory on your EXTERNAL drive";
            //dialog.ShowDialog();
            //if (dialog.SelectedPath == paths.LocalPathOrSource)
            //    return;
            //paths.ExternalPathOrDestination = dialog.SelectedPath;

            //var root = Path.GetPathRoot(paths.ExternalPathOrDestination);
            
            //// Make the external path relative, assuming that the program's directory is at the external drive's root.
            //paths.ExternalPathOrDestination = paths.ExternalPathOrDestination.Remove(0, root.Length);
            //paths.ExternalPathOrDestination = Path.Combine("..", paths.ExternalPathOrDestination);
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

            var list = ListManager.SyncList;

            foreach (var item in list)
            {
                System.Drawing.Image syncIcon;

                switch (item.Direction)
                {
                    case CopyManager.CopyDirection.ToDestination:
                        syncIcon = Res.SyncToRight;
                        break;
                    case CopyManager.CopyDirection.ToSource:
                        syncIcon = Res.SyncToLeft;
                        break;
                    case CopyManager.CopyDirection.DeleteAtDestination:
                        syncIcon = Res.SyncToRightWithDeletion;
                        break;
                    case CopyManager.CopyDirection.DeleteAtSource:
                        syncIcon = Res.SyncToLeftWithDeletion;
                        break;
                    case CopyManager.CopyDirection.ToDestination | CopyManager.CopyDirection.ToSource:
                        syncIcon = Res.FullSync;
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected copy direction.");
                }

                context.AddRow(item.SourcePath, item.DestinationPath, syncIcon);
            }

            //throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        private void SyncFiles()
        {
            var queue = new System.Threading.Tasks.Dataflow.BufferBlock<CopyManager.CopyWorkItem>();

            System.Threading.Tasks.Task.Factory.StartNew(() => DifferenceComputer.ComputeDifferences(queue));

            System.Threading.Tasks.Task.Factory.StartNew(() => CopyManager.Instance.HandleQueue(queue));

            throw new NotImplementedException();

        }

        #endregion
    }
}
