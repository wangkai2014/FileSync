using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using static FileSync.GlobalDefinitions;
using Res = FileSync.Properties.Resources;

namespace FileSync.UI
{
    public class MainWindowPresenter : INotifyPropertyChanged
    {
        #region Classes
        
        public class MappingRow : INotifyPropertyChanged
        {
            #region Fields

            private string m_leftPath;
            private Image m_syncIcon;
            private string m_rightPath;

            #endregion

            #region Properties

            public string LeftPath
            {
                get
                {
                    return m_leftPath;
                }
                set
                {
                    m_leftPath = value;
                    NotifyPropertyChanged("LeftPath");
                }
            }

            public Image SyncIcon
            {
                get
                {
                    return m_syncIcon;
                }
                set
                {
                    m_syncIcon = value;
                    NotifyPropertyChanged("SyncIcon");
                }
            }

            public string RightPath
            {
                get
                {
                    return m_rightPath;
                }
                set
                {
                    m_rightPath = value;
                    NotifyPropertyChanged("RightPath");
                }
            }

            #endregion

            #region Event handlers

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Private methods

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }

        #endregion

        #region Properties

        public List<MappingRow> MappingRows { get; private set; }

        public bool IsSaveButtonEnabled => CoreController.Instance.IsListDirty;

        #endregion

        #region Constructors

        public MainWindowPresenter()
        {
            MappingRows = new List<MappingRow>();
            CoreController.Instance.SubscribeToListDirtinessChanges(NotifyListDirtyChanged);
        }

        #endregion

        #region Event handlers

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public methods

        public void AddRow(string left, string right, CopyDirection direction)
        {
            MappingRows.Add(new MappingRow { LeftPath = left, RightPath = right, SyncIcon = GetIconFromDirection(direction) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="left">Not changed if null.</param>
        /// <param name="right">Not changed if null.</param>
        /// <param name="newDirection"></param>
        public bool UpdateRow(int index, string left, string right, CopyDirection newDirection)
        {
            if (index < 0 || index >= MappingRows.Count)
                return false;

            if (!String.IsNullOrEmpty(left))
                MappingRows[index].LeftPath = left;

            if (!String.IsNullOrEmpty(right))
                MappingRows[index].RightPath = right;

            if (newDirection != (CopyDirection.ToDestination | CopyDirection.DeleteAtDestination))
                MappingRows[index].SyncIcon = GetIconFromDirection(newDirection);

            return true;
        }

        public bool RemoveRow(int index)
        {
            if (index < 0 || index >= MappingRows.Count)
                return false;

            MappingRows.RemoveAt(index);
            return true;
        }

        #endregion

        #region Private methods

        private Image GetIconFromDirection(CopyDirection direction)
        {
            Image syncIcon;

            switch (direction)
            {
                case CopyDirection.ToDestination:
                    syncIcon = Res.SyncToRightIcon;
                    break;
                case CopyDirection.ToSource:
                    syncIcon = Res.SyncToLeftIcon;
                    break;
                case CopyDirection.DeleteAtDestination:
                    syncIcon = Res.SyncToRightWithDeletionIcon;
                    break;
                case CopyDirection.DeleteAtSource:
                    syncIcon = Res.SyncToLeftWithDeletionIcon;
                    break;
                case CopyDirection.ToDestination | CopyDirection.ToSource:
                    syncIcon = Res.FullSyncIcon;
                    break;
                default:
                    throw new InvalidOperationException("Unexpected copy direction.");
            }

            return syncIcon;
        }

        private void NotifyListDirtyChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSaveButtonEnabled"));
        }

        #endregion
    }
}
