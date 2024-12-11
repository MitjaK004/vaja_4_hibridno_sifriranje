using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        private int _ftprogress = 0;
        public const string WindowTitle = "Hybrid Encription";
        public const string WindowTitleRecieve = "Hybrid Encription - Reciever";
        public const string WindowTitleSend = "Hybrid Encription - Sender";
        private string _title = WindowTitle;
        public ViewModel() { }
        public void AddFilePath(string FilePath)
        {
            _filePaths.Add(new Path(FilePath));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePaths"));
        }
        public ObservableCollection<Path> FilePaths { 
            get { return _filePaths; }
            set
            {
                _filePaths = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
            }
        }
        public string ConnectionStatus { 
            get { return _connectionStatus; }
            set { 
                _connectionStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionStatus"));
            }
        }
        public int FTProgress
        {
            get { return _ftprogress; }
            set { 
                _ftprogress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FTProgress"));
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
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
            }
        }
        public void ProgressToString(double _0progress)
        {
            string rval = "";
            FTProgress = (int)(100 * _0progress);
            _0progress = 100 * _0progress;
            rval = _0progress.ToString();
            if(_0progress < 10)
            {
                if (rval.Length > 3) {
                    rval = rval.Substring(0, 3);
                }
            }
            else
            {
                if (rval.Length > 4)
                {
                    rval = rval.Substring(0, 4);
                }
            }
            rval += " %";
            FilesTransferProgress = rval;
        }
    }
}
