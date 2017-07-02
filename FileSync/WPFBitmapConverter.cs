using System;
using System.Drawing;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FileSync
{
    /// <summary>
    /// This is kinda stupid, UIContext.MappingRow.SyncIcon could just be a path to the image on the disk.
    /// Instead we use the images we have in our resources and convert them every time we use them
    /// with this converter.
    /// A quick copy-paste from : http://www.shujaat.net/2010/08/wpf-images-from-project-resource.html
    /// and edited it a bit.
    /// Will be changed if I have time.
    /// </summary>
    public class WPFBitmapConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            var img = value as Image;
            MemoryStream ms = new MemoryStream();
            ((Bitmap)value).Save(ms, img == null? System.Drawing.Imaging.ImageFormat.Png : img.RawFormat);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
