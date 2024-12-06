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
    /// Interaction logic for Recieve.xaml
    /// </summary>
    public partial class RecieveWindow : Window
    {
        public int Port { get; private set; }
        public RecieveWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PortTextBox.Text, out int port))
            {
                Port = port;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid port number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
