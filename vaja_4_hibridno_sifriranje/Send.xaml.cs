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

namespace vaja_4_hibridno_sifriranje
{
    /// <summary>
    /// Interaction logic for Send.xaml
    /// </summary>
    public partial class SendWindow : Window
    {
        public string IP { get; private set; }
        public int Port { get; private set; }
        public SendWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            IP = IpTextBox.Text;

            if (int.TryParse(PortTextBox.Text, out int port))
            {
                Port = port;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid port number.", "Validation Error");
            }
        }
    }
}
