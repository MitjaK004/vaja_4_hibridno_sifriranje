using Microsoft.Win32;
using System.IO;
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

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            networkHandler.Clear();
            VM.FilePaths.Clear();
        }

        private void Make_a_Test_File_Click(object sender, RoutedEventArgs e)
        {
            int size = 0;
            TestFileSize receiveWindow = new TestFileSize();
            bool? result = receiveWindow.ShowDialog();
            if (result == true)
            {
                size = receiveWindow.Size * 1024 * 1024;
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                
                saveFileDialog.Title = "Save File";
                saveFileDialog.Filter = "All Files|*.*";
                saveFileDialog.FileName = "output.txt";

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            byte[] buffer = new byte[1024*1024];
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                buffer[i] = (byte)'c';
                            }
                            
                            long bytesWritten = 0;
                            while (bytesWritten < size)
                            {
                                long bytesToWrite = Math.Min(buffer.Length, size - bytesWritten);
                                fs.Write(buffer, 0, (int)bytesToWrite);
                                bytesWritten += bytesToWrite;
                            }
                            
                        }

                        MessageBox.Show($"File saved successfully at: {saveFileDialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}", "ERROR");
                    }
                }
                
            }
            else
            {
                MessageBox.Show("Receive window was canceled.", "Info");
            }
        }
    }
}