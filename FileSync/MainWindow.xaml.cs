using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FileSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class PathPair { public string LocalPathOrSource; public string ExternalPathOrDestination; }
        private const string MapFileName = "mappings.list";
        private const string SettingsFileName = "settings"; // TODO: use this file to store settings (xml)

        private bool m_syncToExternal;
        private bool m_syncToInternal;

        private ProgressWindow m_progressWindow;
        private List<PathPair> m_folderMapping;

        public MainWindow()
        {
            InitializeComponent();

            m_syncToExternal = true;
            m_syncToInternal = true;

            m_progressWindow = new ProgressWindow();
        }

        #region Handlers

        private void AddMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select the directory on your INTERNAL drive";
            dialog.ShowDialog();
            if (String.IsNullOrEmpty(dialog.SelectedPath))
                return;
            var paths = new PathPair();
            paths.LocalPathOrSource = dialog.SelectedPath;
            dialog.Description = "Select the directory on your EXTERNAL drive";
            dialog.ShowDialog();
            if (dialog.SelectedPath == paths.LocalPathOrSource)
                return;
            paths.ExternalPathOrDestination = dialog.SelectedPath;

            var root = Path.GetPathRoot(paths.ExternalPathOrDestination);
            
            // Make the external path relative, assuming that the program's directory is at the external drive's root.
            paths.ExternalPathOrDestination = paths.ExternalPathOrDestination.Remove(0, root.Length);
            paths.ExternalPathOrDestination = Path.Combine("..", paths.ExternalPathOrDestination);

            AddPathMap(paths);
        }

        private void RemoveMappingBtn_Click(object sender, RoutedEventArgs e)
        {
            m_folderMapping.RemoveAt(InternalPathsListBox.SelectedIndex);
        }

        private void SyncNowBtn_Click(object sender, RoutedEventArgs e)
        {
            SyncFiles();
        }

        private void Copy_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            m_progressWindow.CopyProgressBar.Value = e.ProgressPercentage;
        }

        private void Copy_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            m_progressWindow.CloseButton.IsEnabled = true;
        }

        private void InternalPathsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ExternalPathsListBox.SelectedIndex = InternalPathsListBox.SelectedIndex;
        }

        private void ExternalPathsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InternalPathsListBox.SelectedIndex = ExternalPathsListBox.SelectedIndex;
        }
        
        private void SyncLocalToExternalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            m_syncToExternal = true;
        }

        private void SyncLocalToExternalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            m_syncToExternal = false;
        }

        private void SyncExternalToLocalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            m_syncToInternal = true;
        }

        private void SyncExternalToLocalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            m_syncToInternal = false;
        }

        #endregion

        #region Private methods
        
        private void AddPathMap(PathPair pair)
        {
            m_folderMapping.Add(pair);

            InternalPathsListBox.Items.Add(new ListBoxItem { Content = pair.LocalPathOrSource });
            ExternalPathsListBox.Items.Add(new ListBoxItem { Content = pair.ExternalPathOrDestination });
        }
        
        private void SyncFiles()
        {
            ListManager.Init(@"D:\Projects\FileSync\FileSync\testList.list");

            var queue = new System.Threading.Tasks.Dataflow.BufferBlock<CopyManager.CopyWorkItem>();

            System.Threading.Tasks.Task.Factory.StartNew(() => DifferenceComputer.ComputeDifferences(queue));

            System.Threading.Tasks.Task.Factory.StartNew(() => CopyManager.Instance.HandleQueue(queue));
            
        }
        
        #endregion
    }
}
