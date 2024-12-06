using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vaja_4_hibridno_sifriranje.Network;

namespace vaja_4_hibridno_sifriranje.ViewModelNamespace
{
    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged = null;
        private ObservableCollection<Path> _filePaths = new ObservableCollection<Path>();
        private string _connectionStatus = Status.Stopped.ToString();
        private string _filesTransferStatus = Status.Stopped.ToString();
        private string _filesTransferProgress = "--";
        public ViewModel() { }
        public void AddFilePath(string FilePath)
        {
            _filePaths.Add(new Path(FilePath));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePaths"));
        }
        public ObservableCollection<Path> FilePaths { get { return _filePaths; } }
        public string ConnectionStatus { 
            get { return _connectionStatus; }
            set { 
                _connectionStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionStatus"));
            }
        }
        public string FilesTransferStatus
        {
            get { return _filesTransferStatus; }
            set
            {
                _filesTransferStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilesTransferStatus"));
            }
        }
        public string FilesTransferProgress
        {
            get { return _filesTransferProgress; }
            set
            {
                _filesTransferProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilesTransferProgress"));
            }
        }
    }
}
