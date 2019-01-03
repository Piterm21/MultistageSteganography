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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.InteropServices;

namespace MultistageSteganography
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public String openImageFileExt;
        public String openImageFileName;
        public static String acceptedFileFormats = "Image Files (*.bmp;*.jpg;*.jpeg;*.JPG;*.JPEG)|*.bmp;*.jpg;*.jpeg;*.JPG;*.JPEG";
        public byte[] fileBytes;

        public MainWindow ()
        {
            InitializeComponent();
        }

        private void OpenImage (object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ".bmp";
            openFileDialog.Filter = MainWindow.acceptedFileFormats;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true) {
                BitmapImage bitmap = new BitmapImage();

                this.fileBytes = File.ReadAllBytes(openFileDialog.FileName);
                openImageFileName = openFileDialog.FileName;
                string[] splitFilename = openFileDialog.FileName.Split('.');
                openImageFileExt = splitFilename[splitFilename.Length - 1].ToLower();

                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(this.fileBytes);
                bitmap.EndInit();

                SourceImage.Source = bitmap;
            }
        }

        private void SaveImage (object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = this.openImageFileExt;
            saveFileDialog.FileName = this.openImageFileName;
            saveFileDialog.Filter = MainWindow.acceptedFileFormats;

            if (saveFileDialog.ShowDialog() == true) {
                File.WriteAllBytes(saveFileDialog.FileName, this.fileBytes);
            }
        }

        private bool encodeLSBBMP ()
        {
            bool result = false;

            BmpFileHeaders bmpFileHeaders = FileFormatHelpers.readBmpFileHeaders(this.fileBytes);

            LSBEncodeDialog lSBEncodeDialog = new LSBEncodeDialog();

            if (lSBEncodeDialog.ShowDialog() == true) {
                EncodeQueryResult encodeQueryResult = lSBEncodeDialog.getResult();

                byte[] sourceByteArray = FileFormatHelpers.convertLayerToByteArray(encodeQueryResult.layers[encodeQueryResult.layers.Count - 1]);

                for (int index = (encodeQueryResult.layers.Count - 1); index >= 0; index--) {
                    if (index == 0) {
                        FileFormatHelpers.encodeDataIntoFileBytesBmp(
                            ref bmpFileHeaders,
                            ref sourceByteArray,
                            ref this.fileBytes
                        );
                    } else {
                        LayerInformation layerTargetInformation = encodeQueryResult.layers[index - 1];
                        sourceByteArray = FileFormatHelpers.encodeDataIntoLayersFileBytes(
                            ref sourceByteArray,
                            layerTargetInformation
                        );
                    }

                    encodeQueryResult.layers.RemoveAt(index);
                }

                result = true;
            }

            return result;
        }

        private void decodeLSB (FileType fileType)
        {
            byte[] currentFileBytesToDecode = this.fileBytes;

            if (fileType == FileType.BMP) {
                currentFileBytesToDecode = FileFormatHelpers.decodeDataFromFileBytesBmp(ref currentFileBytesToDecode);
            } else {
                currentFileBytesToDecode = FileFormatHelpers.decodeDataFromFileBytesJpg(ref currentFileBytesToDecode);
            }

            while (currentFileBytesToDecode != null) {
                FileType decodedFileBytesFileType = FileFormatHelpers.checkFileType(ref currentFileBytesToDecode);

                switch (decodedFileBytesFileType) {
                    case (FileType.BMP): {
                        DecodedFileWindow decodedFileWindow = new DecodedFileWindow();
                        decodedFileWindow.setFile(ref currentFileBytesToDecode);
                        decodedFileWindow.Show();

                        currentFileBytesToDecode = FileFormatHelpers.decodeDataFromFileBytesBmp(ref currentFileBytesToDecode);
                    } break;

                    case (FileType.JPEG): {
                        DecodedFileWindow decodedFileWindow = new DecodedFileWindow();
                        decodedFileWindow.setFile(ref currentFileBytesToDecode);
                        decodedFileWindow.Show();

                        currentFileBytesToDecode = FileFormatHelpers.decodeDataFromFileBytesJpg(ref currentFileBytesToDecode);
                    } break;

                    case (FileType.NONE): {
                        StringBuilder message = new StringBuilder();

                        byte[] currentCharBytesUnicode = System.Text.Encoding.Convert(Encoding.ASCII, Encoding.Unicode, currentFileBytesToDecode);
                        char[] unicodeChars = new char[Encoding.Unicode.GetCharCount(currentCharBytesUnicode, 0, currentCharBytesUnicode.Length)];
                        Encoding.Unicode.GetChars(currentCharBytesUnicode, 0, currentCharBytesUnicode.Length, unicodeChars, 0);
                        string unicodeString = new string(unicodeChars);
                        currentFileBytesToDecode = null;

                        DecodedMessageWindow decodedMessageWindow = new DecodedMessageWindow();
                        decodedMessageWindow.setMessage(unicodeString);
                        decodedMessageWindow.Show();
                    } break;
                }
            }
        }

        private bool encodeLSBJPG ()
        {
            bool result = false;
            JpgFileStatistics jpgFileStatistics = FileFormatHelpers.getJpgFileStatistics(ref this.fileBytes);

            LSBEncodeDialog lSBEncodeDialog = new LSBEncodeDialog();
            if (lSBEncodeDialog.ShowDialog() == true) {
                EncodeQueryResult encodeQueryResult = lSBEncodeDialog.getResult();
                byte[] sourceByteArray = FileFormatHelpers.convertLayerToByteArray(encodeQueryResult.layers[encodeQueryResult.layers.Count - 1]);

                for (int index = (encodeQueryResult.layers.Count - 1); index >= 0; index--) {
                    if (index == 0) {
                        FileFormatHelpers.encodeDataIntoFileBytesJpg(
                            ref jpgFileStatistics,
                            ref sourceByteArray,
                            ref this.fileBytes
                        );
                    } else {
                        LayerInformation layerTargetInformation = encodeQueryResult.layers[index - 1];
                        sourceByteArray = FileFormatHelpers.encodeDataIntoLayersFileBytes(
                            ref sourceByteArray,
                            layerTargetInformation
                        );
                    }

                    encodeQueryResult.layers.RemoveAt(index);
                }

                result = true;
            }

            return result;
        }

        private void EncodeLSB (object sender, RoutedEventArgs e)
        {
            bool encodingResult = false;

            switch (openImageFileExt) {
                case "bmp": {
                        encodingResult = this.encodeLSBBMP();
                } break;

                case "jpg":
                case "jpeg": {
                        encodingResult = this.encodeLSBJPG();
                } break;
            }

            if (encodingResult) {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(this.fileBytes);
                bitmap.EndInit();

                ResultImage.Source = bitmap;
            }
        }

        private void DecodeLSB (object sender, RoutedEventArgs e)
        {
            switch (openImageFileExt) {
                case "bmp": {
                    this.decodeLSB(FileType.BMP);
                } break;

                case "jpg":
                case "jpeg": {
                    this.decodeLSB(FileType.JPEG);
                } break;
            }
        }
    }
}