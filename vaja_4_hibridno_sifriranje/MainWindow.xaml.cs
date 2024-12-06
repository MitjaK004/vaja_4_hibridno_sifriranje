using Microsoft.Win32;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vaja_4_hibridno_sifriranje.Network;
using vaja_4_hibridno_sifriranje.ViewModelNamespace;

namespace vaja_4_hibridno_sifriranje
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkHandler networkHandler;
        
        private ViewModel VM = new ViewModel();
        public MainWindow()
        {
            networkHandler = new NetworkHandler(VM);
            DataContext = VM;
            InitializeComponent();
        }

        private void sendFiles_Click(object sender, RoutedEventArgs e)
        {
            SendWindow sendWindow = new SendWindow();
            bool? result = sendWindow.ShowDialog();

            if (result == true)
            {
                networkHandler.IP = sendWindow.IP;
                networkHandler.Port = sendWindow.Port;

                networkHandler.Sender();
            }
            else
            {
                MessageBox.Show("Send window was canceled.", "Info");
            }
            
        }

        private void recieveFiles_Click(object sender, RoutedEventArgs e)
        {
            RecieveWindow receiveWindow = new RecieveWindow();
            bool? result = receiveWindow.ShowDialog();

            if (result == true)
            {
                networkHandler.Port = receiveWindow.Port;
                networkHandler.IP = "127.0.0.1";
                networkHandler.Reciever();
            }
            else
            {
                MessageBox.Show("Receive window was canceled.", "Info");
            }
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Select a File";
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.InitialDirectory = "C:\\";

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                if (networkHandler.AddFile(selectedFilePath))
                    VM.AddFilePath(selectedFilePath);
            }
            else
            {
                MessageBox.Show("No file selected.", "Information");
            }
            
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            VM.Title = ViewModel.WindowTitle;
            System.Environment.Exit(1);
        }
    }
}