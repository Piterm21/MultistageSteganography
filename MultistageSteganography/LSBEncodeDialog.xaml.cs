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

namespace MultistageSteganography
{
    public enum LayerSourceType { Text, File };

    public struct LayerInformation
    {
        public string text;
        public string filePath;
        public LayerSourceType type;
    }

    public struct EncodeQueryResult
    {
        public List<LayerInformation> layers;
    }

    public partial class LSBEncodeDialog : Window
    {
        List<Panel> layerContainerPanels;

        public LSBEncodeDialog ()
        {
            InitializeComponent();

            layerContainerPanels = new List<Panel>();
            layerContainerPanels.Add(encodingSourceTypeFirstLayer);
            layerContainerPanels.Add(encodingSourceTypeSecondLayer);
        }

        private void ChooseFile (object sender, RoutedEventArgs e)
        {
            Button buttonSender = (Button)sender;
            Grid parentGrid = (Grid)buttonSender.Parent;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ".bmp";
            openFileDialog.Filter = MainWindow.acceptedFileFormats;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true) {
                parentGrid.Tag = openFileDialog.FileName;
                IEnumerable<Label> labels = parentGrid.Children.OfType<Label>();
                labels.First<Label>().Content = openFileDialog.FileName;
            }
        }

        private void setVisibilityOfFirstChildGridOfElement(Panel element, Visibility visibility)
        {
            IEnumerable<Grid> children = element.Children.OfType<Grid>();
            Grid firstChild = children.First<Grid>();
            firstChild.Visibility = visibility;
            firstChild.Tag = "";
            IEnumerable<Label> labels = firstChild.Children.OfType<Label>();
            labels.First<Label>().Content = "";
        }

        private void setVisibilityOfFirstChildTextBoxOfElement(Panel element, Visibility visibility)
        {
            IEnumerable<TextBox> children = element.Children.OfType<TextBox>();
            TextBox firstChild = children.First<TextBox>();
            firstChild.Visibility = visibility;
            firstChild.Tag = "";
            firstChild.Text = "";
        }

        private void SingleLayerEncoding_Checked (object sender, RoutedEventArgs e)
        {
            foreach (RadioButton button in encodingSourceTypeFirstLayer.Children.OfType<RadioButton>()) {
                if ((LayerSourceType)button.Tag == LayerSourceType.Text) {
                    button.IsChecked = true;
                } else {
                    button.IsChecked = false;
                }

                button.IsEnabled = true;
            }

            setVisibilityOfFirstChildGridOfElement(encodingSourceTypeFirstLayer, Visibility.Collapsed);
            setVisibilityOfFirstChildTextBoxOfElement(encodingSourceTypeFirstLayer, Visibility.Visible);

            foreach (RadioButton button in encodingSourceTypeSecondLayer.Children.OfType<RadioButton>()) {
                button.IsChecked = false;
                button.IsEnabled = false;
            }

            setVisibilityOfFirstChildGridOfElement(encodingSourceTypeSecondLayer, Visibility.Collapsed);
            setVisibilityOfFirstChildTextBoxOfElement(encodingSourceTypeSecondLayer, Visibility.Collapsed);
        }

        private void DoubleLayerEncoding_Checked (object sender, RoutedEventArgs e)
        {
            foreach (RadioButton button in encodingSourceTypeFirstLayer.Children.OfType<RadioButton>()) {
                if ((LayerSourceType)button.Tag == LayerSourceType.Text) {
                    button.IsChecked = false;
                } else {
                    button.IsChecked = true;
                }

                button.IsEnabled = false;
            }

            setVisibilityOfFirstChildGridOfElement(encodingSourceTypeFirstLayer, Visibility.Visible);
            setVisibilityOfFirstChildTextBoxOfElement(encodingSourceTypeFirstLayer, Visibility.Collapsed);

            foreach (RadioButton button in encodingSourceTypeSecondLayer.Children.OfType<RadioButton>()) {
                if ((LayerSourceType)button.Tag == LayerSourceType.Text) {
                    button.IsChecked = true;
                } else {
                    button.IsChecked = false;
                }

                button.IsEnabled = true;
            }

            setVisibilityOfFirstChildGridOfElement(encodingSourceTypeSecondLayer, Visibility.Collapsed);
            setVisibilityOfFirstChildTextBoxOfElement(encodingSourceTypeSecondLayer, Visibility.Visible);
        }

        private void LSBEncodeDialogOK (object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
            
        private void addLayerToListIfNotNull(ref List<LayerInformation> list, Panel panel)
        {
            LayerSourceType? type = null;

            foreach (RadioButton button in panel.Children.OfType<RadioButton>()) {
                if (button.IsChecked == true) {
                    type = (LayerSourceType)button.Tag;
                }
            }

            if (type.HasValue == true) {
                LayerInformation layerSourceInformation = new LayerInformation();
                layerSourceInformation.type = type.Value;

                if (layerSourceInformation.type == LayerSourceType.File) {
                    IEnumerable<Grid> grids = panel.Children.OfType<Grid>();
                    layerSourceInformation.filePath = grids.First<Grid>().Tag.ToString();
                } else {
                    IEnumerable<TextBox> textBoxes = panel.Children.OfType<TextBox>();
                    layerSourceInformation.text = textBoxes.First<TextBox>().Text;
                }

                list.Add(layerSourceInformation);
            }
        }

        private void ChangeLayerDataType_Click (object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            StackPanel parentStackPanel = (StackPanel)radioButton.Parent;

            if ((LayerSourceType)radioButton.Tag == LayerSourceType.File) {
                setVisibilityOfFirstChildGridOfElement(parentStackPanel, Visibility.Visible);
                setVisibilityOfFirstChildTextBoxOfElement(parentStackPanel, Visibility.Collapsed);
            } else {
                setVisibilityOfFirstChildGridOfElement(parentStackPanel, Visibility.Collapsed);
                setVisibilityOfFirstChildTextBoxOfElement(parentStackPanel, Visibility.Visible);
            }
        }

        public EncodeQueryResult getResult ()
        {
            EncodeQueryResult result = new EncodeQueryResult();

            result.layers = new List<LayerInformation>();

            foreach (Panel panel in layerContainerPanels) {
                addLayerToListIfNotNull(ref result.layers, panel);
            }

            return result;
        }
    }
}
