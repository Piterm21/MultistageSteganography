using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace MultistageSteganography
{
    public partial class DecodedFileWindow : Window
    {
        public DecodedFileWindow ()
        {
            InitializeComponent();
        }

        public void setFile (ref byte[] fileBytes)
        {
            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(fileBytes);
            bitmap.EndInit();

            ResultFile.Source = bitmap;
        }

        private void buttonOk (object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
