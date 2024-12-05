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
        private NetworkHandler networkHandler = new NetworkHandler();
        private const string WindowTitle = "Hybrid Encription";
        private const string WindowTitleRecieve = "Hybrid Encription - Reciever";
        private const string WindowTitleSend = "Hybrid Encription - Sender";
        private ViewModel VM = new ViewModel();
        public MainWindow()
        {
            DataContext = VM;
            InitializeComponent();
        }

        private void sendFiles_Click(object sender, RoutedEventArgs e)
        {
            this.Title = WindowTitleSend;
            networkHandler.Sender();
        }

        private void recieveFiles_Click(object sender, RoutedEventArgs e)
        {
            this.Title = WindowTitleRecieve;
            networkHandler.Reciever();
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            networkHandler.AddFile("C:\\Test\\baje.txt");
            VM.AddFilePath("C:\\Test\\baje.txt");
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(1);
        }
    }
}