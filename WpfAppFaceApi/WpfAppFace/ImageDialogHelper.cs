using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace WpfAppFace
{
    class ImageDialogHelper
    {
        public static void LoadImage(Image i, string filename)
        {
            i.Source = new BitmapImage(new Uri(filename));
        }

        public static OpenFileDialog GetFileDialog()
        {
            return new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Image Files(*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
        }
    }
}
