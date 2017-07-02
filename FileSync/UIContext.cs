using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Res = FileSync.Properties.Resources;

namespace FileSync
{
    public class UIContext
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
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            #endregion
        }

        #endregion

        #region Properties

        public List<MappingRow> MappingRows { get; private set; }

        #endregion

        #region Constructors

        public UIContext()
        {
            MappingRows = new List<MappingRow> ();
        }

        #endregion

        #region Public methods

        public void AddRow(string left, string right, CopyManager.CopyDirection direction)
        {
            MappingRows.Add(new MappingRow { LeftPath = left, RightPath = right, SyncIcon = GetIconFromDirection(direction) });
        }

        public void UpdateRowDirectionIcon(int index, CopyManager.CopyDirection newDirection)
        {
            MappingRows[index].SyncIcon = GetIconFromDirection(newDirection);
        }

        #endregion

        private Image GetIconFromDirection(CopyManager.CopyDirection direction)
        {
            System.Drawing.Image syncIcon;

            switch (direction)
            {
                case CopyManager.CopyDirection.ToDestination:
                    syncIcon = Res.SyncToRightIcon;
                    break;
                case CopyManager.CopyDirection.ToSource:
                    syncIcon = Res.SyncToLeftIcon;
                    break;
                case CopyManager.CopyDirection.DeleteAtDestination:
                    syncIcon = Res.SyncToRightWithDeletionIcon;
                    break;
                case CopyManager.CopyDirection.DeleteAtSource:
                    syncIcon = Res.SyncToLeftWithDeletionIcon;
                    break;
                case CopyManager.CopyDirection.ToDestination | CopyManager.CopyDirection.ToSource:
                    syncIcon = Res.FullSyncIcon;
                    break;
                default:
                    throw new InvalidOperationException("Unexpected copy direction.");
            }

            return syncIcon;
        }
    }
}
