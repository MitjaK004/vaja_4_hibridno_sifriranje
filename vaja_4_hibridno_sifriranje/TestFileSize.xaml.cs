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
    /// Interaction logic for TestFileSize.xaml
    /// </summary>
    public partial class TestFileSize : Window
    {
        public TestFileSize()
        {
            InitializeComponent();
        }

        public int Size = 100;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Size = int.Parse(SizeTextBox.Text);
            DialogResult = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
