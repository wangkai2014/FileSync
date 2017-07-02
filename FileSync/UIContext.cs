using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FileSync
{
    public class UIContext
    {
        #region Classes

        public class MappingRow : INotifyPropertyChanged
        {
            #region Fields

            private string m_leftPath;
            private BitmapImage m_syncIcon;
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

            public BitmapImage SyncIcon
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

        public void AddRow(string left, string right, Image icon)
        {
            MappingRows.Add(new MappingRow { LeftPath = left, RightPath = right, SyncIcon = ConvertToImageSource(icon) });
        }

        #endregion

        #region Private methods

        /// <summary>
        /// This is kinda stupid, MappingRow.SyncIcon could just be a path to the image on the disk.
        /// Instead we use the images we have in our resources and convert them every time we use them.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private BitmapImage ConvertToImageSource(Image image)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)image).Save(ms, image.RawFormat);
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            img.StreamSource = ms;
            img.EndInit();

            return img;
        }

        #endregion
    }
}
