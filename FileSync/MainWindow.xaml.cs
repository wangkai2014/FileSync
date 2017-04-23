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
        private BackgroundWorker m_bw;

        public MainWindow()
        {
            InitializeComponent();

            LoadMappingsFromFile();

            m_syncToExternal = true;
            m_syncToInternal = true;

            m_progressWindow = new ProgressWindow();

            m_bw = new BackgroundWorker();
            m_bw.WorkerSupportsCancellation = true; // TODO: allow user cancellation from the copy window.
            m_bw.WorkerReportsProgress = true;
            m_bw.DoWork += new DoWorkEventHandler((s, args) => { SyncFiles(); });
            m_bw.ProgressChanged += new ProgressChangedEventHandler(Copy_ProgressChanged);
            m_bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Copy_RunWorkerCompleted);
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

            ReloadMapList();
        }

        private void SyncNowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!m_bw.IsBusy)
            {
                m_progressWindow.CloseButton.IsEnabled = false;
                m_progressWindow.Show();
                m_bw.RunWorkerAsync();
            }
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

        private void LoadMappingsFromFile()
        {
            m_folderMapping = new List<PathPair>();
            InternalPathsListBox.Items.Clear();
            ExternalPathsListBox.Items.Clear();

            if (!File.Exists(MapFileName))
            {
                File.Create(MapFileName).Close();
                return;
            }

            var lines = File.ReadAllLines(MapFileName).Select(line => line.Split('|'));

            foreach (var line in lines)
            {
                m_folderMapping.Add(new PathPair { LocalPathOrSource = line[0], ExternalPathOrDestination = line[1] });

                InternalPathsListBox.Items.Add(new ListBoxItem { Content = line[0] });
                ExternalPathsListBox.Items.Add(new ListBoxItem { Content = line[1] });
            }
        }

        private void WriteAllMapsToFile()
        {
            File.WriteAllLines(MapFileName, m_folderMapping.Select(pair => $"{pair.LocalPathOrSource}|{pair.ExternalPathOrDestination}"));
        }

        private void AddPathMap(PathPair pair)
        {
            m_folderMapping.Add(pair);

            InternalPathsListBox.Items.Add(new ListBoxItem { Content = pair.LocalPathOrSource });
            ExternalPathsListBox.Items.Add(new ListBoxItem { Content = pair.ExternalPathOrDestination });

            ReloadMapList();
        }

        private void ReloadMapList()
        {
            WriteAllMapsToFile();

            LoadMappingsFromFile();
        }

        private void SyncFiles()
        {
            foreach (var pair in m_folderMapping)
            {
                var differences = GetFoldersDifferences(pair);

                int copiedFiles = 0;

                foreach (var difference in differences)
                {
                    // NOTE: This condition is useless if both source and destination are on the same disk.
                    if (Path.GetPathRoot(difference.LocalPathOrSource) == Path.GetPathRoot(pair.LocalPathOrSource) && m_syncToExternal ||
                        Path.GetPathRoot(difference.LocalPathOrSource) == Path.GetPathRoot(pair.ExternalPathOrDestination) && m_syncToInternal)
                    {
                        // Update UI to show the files that are beeing copied
                        Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
                            m_progressWindow.SourceFileTextBox.Text = difference.LocalPathOrSource;
                            m_progressWindow.DestinationFolderTextBox.Text = Path.GetDirectoryName(difference.ExternalPathOrDestination);
                            m_progressWindow.FileCountLabel.Content = copiedFiles + "/" + differences.Count;
                        }));

                        File.Copy(difference.LocalPathOrSource, difference.ExternalPathOrDestination);
                        copiedFiles++;
                    }

                    m_bw.ReportProgress(copiedFiles * 100 / differences.Count);
                }

                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
                    m_progressWindow.FileCountLabel.Content = copiedFiles + "/" + differences.Count;
                }));
            }
        }

        /// <summary>
        /// Find files and dorectories that are not synced between the two folders in the pair.
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        private List<PathPair> GetFoldersDifferences(PathPair pair)
        {
            if (!Directory.Exists(pair.LocalPathOrSource))
                Directory.CreateDirectory(pair.LocalPathOrSource);
            if (!Directory.Exists(pair.ExternalPathOrDestination))
                Directory.CreateDirectory(pair.ExternalPathOrDestination);

            List<PathPair> differences = new List<PathPair>();

            var localFiles = Directory.GetFiles(pair.LocalPathOrSource).Select(path => Path.GetFileName(path)).ToList();
            var externalFiles = Directory.GetFiles(pair.ExternalPathOrDestination).Select(path => Path.GetFileName(path)).ToList();

            var commonFiles = localFiles.Where(file => externalFiles.Contains(file)).ToList();

            localFiles.RemoveAll(file => commonFiles.Contains(file));
            externalFiles.RemoveAll(file => commonFiles.Contains(file));

            foreach (var file in localFiles)
            {
                differences.Add(new PathPair { LocalPathOrSource = Path.Combine(pair.LocalPathOrSource, file), ExternalPathOrDestination = Path.Combine(pair.ExternalPathOrDestination, file) });
            }

            foreach (var file in externalFiles)
            {
                differences.Add(new PathPair { LocalPathOrSource = Path.Combine(pair.ExternalPathOrDestination, file), ExternalPathOrDestination = Path.Combine(pair.LocalPathOrSource, file) });
            }

            // Recursively handle folders
            //if (RecursiveCheckBox.IsChecked.Value)
            {
                var localFolders = Directory.GetDirectories(pair.LocalPathOrSource).ToList();
                var externalFolders = Directory.GetDirectories(pair.ExternalPathOrDestination).ToList();

                foreach (var folder in localFolders)
                {
                    differences.AddRange(GetFoldersDifferences(
                        new PathPair
                        {
                            LocalPathOrSource = folder,
                            ExternalPathOrDestination = Path.Combine(pair.ExternalPathOrDestination, Path.GetFileName(folder))
                        }));
                }

                foreach (var folder in externalFolders)
                {
                    differences.AddRange(GetFoldersDifferences(
                        new PathPair
                        {
                            LocalPathOrSource = Path.Combine(pair.LocalPathOrSource, Path.GetFileName(folder)),
                            ExternalPathOrDestination = folder
                        }));
                }
            }

            return differences;
        }

        #endregion
    }
}
