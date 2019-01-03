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
    public partial class DecodedMessageWindow : Window
    {
        public DecodedMessageWindow ()
        {
            InitializeComponent();
        }

        public void setMessage(String message)
        {
            DecodedMessage.Text = message;
        }

        private void buttonOk (object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
