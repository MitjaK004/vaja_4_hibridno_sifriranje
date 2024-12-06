using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace vaja_4_hibridno_sifriranje.Network
{
    class FileNotExsistException : Exception
    {
        public new string Message { get; private set; }
        public FileNotExsistException(string FileName) {
            Message = "The file: " + FileName + ", does not exsist!";
        }
    }
    class NetworkHandler
    {
        public Status ConnectionStatus { get; private set; } = Status.Stopped;
        public Status FileTransferStatus { get; private set; } = Status.Stopped;
        public double FileTransferProgress { get; private set; } = -1;
        private const int MaxBufflen = 4096;
        private const int KeyLength = 8192;
        private List<byte[]> RecievedFileData = new List<byte[]>();
        private List<byte[]> SendFileData = new List<byte[]>();
        private List<string> RecievedFileNames = new List<string>();
        private List<string> SendFilePaths = new List<string>();
        private TcpClient? Client = null;
        private TcpListener? Listener = null;
        private NetworkStream? NetStream = null;
        private IPEndPoint? iPEndPoint = null;
        public string IP = "127.0.0.1";
        public int Port = 5789;
        private bool ConnectionRunning = false;
        private byte[] success = { 1, 2, 3, 4, 5 };
        private byte[] fail = { 7, 8, 9, 10, 11 };
        public NetworkHandler() {}
        public NetworkHandler(int _Port) { Port = _Port; }
        public NetworkHandler(string _IP, int _Port) { Port = _Port; IP = _IP; }
        public bool AddFile(string Path)
        {
            if (File.Exists(Path))
            {
                SendFilePaths.Add(Path);
                return true;
            }
            else
            {
                MessageBox.Show("File: \'" + Path + "\' does not exsist!", "ERROR");
                return false;
            }
        }
        private Task SendFiles() {

            iPEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);

            Client = new TcpClient();
            Client.Connect(iPEndPoint);

            using (NetStream = Client.GetStream())
            {
                SendAll();
            }

            Client?.Close();

            return Task.CompletedTask;
        }
        private Task RecieveFiles() {
            iPEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            Listener = new TcpListener(iPEndPoint);

            Listener.Start();

           using (Client = Listener.AcceptTcpClient())
           using (NetStream = Client.GetStream())
           {
               RecieveAll();
           }

            Listener.Stop();

            return Task.CompletedTask;
        }
        public void Sender()
        {
            Task.Run(SendFiles);
        }

        public void Reciever()
        {
            Task.Run(RecieveFiles);
        }
        private bool RecieveAll() {
            int NumFiles = int.Parse(RecieveString(NetStream, MaxBufflen));
            Send(NetStream, success);
            for (int i = 0; i < NumFiles; i++)
            {
                string fileName = RecieveString(NetStream, MaxBufflen);
                Send(NetStream, success);
                int NumParcels = BitConverter.ToInt32(RecieveBytes(NetStream, MaxBufflen), 0);
                Send(NetStream, success);
                long FileSize = BitConverter.ToInt64(RecieveBytes(NetStream, MaxBufflen), 0);
                if (IsSpaceAvailableInCurrentFolder(FileSize)) {
                    Send(NetStream, success);
                    for(int j = 0; j < NumParcels; j++)
                    {
                        int ParcelLength = BitConverter.ToInt32(RecieveBytes(NetStream, MaxBufflen));
                        Send(NetStream, success);
                        byte[] Data = RecieveBytes(NetStream, MaxBufflen);
                        Array.Resize(ref Data, ParcelLength);
                        AppendOrCreateFile(fileName, Data);
                        Send(NetStream, success);
                    }
                }
                else {
                    Send(NetStream, fail);
                    MessageBox.Show("Not enough disk space!!!", "ERROR");
                    return false;
                }
            }
            return true; 
        }
        private bool SendAll() {
            int NumFiles = SendFilePaths.Count;
            Send(NetStream, NumFiles.ToString());
            RecieveBytes(NetStream, MaxBufflen);
            for(int i = 0; i < NumFiles; i++)
            {
                Send(NetStream, GetFileName(SendFilePaths[i]));
                RecieveBytes(NetStream, MaxBufflen);
                int NumParcels = GetNumParcels(SendFilePaths[i], MaxBufflen);
                Send(NetStream, BitConverter.GetBytes(NumParcels));
                RecieveBytes(NetStream, MaxBufflen);
                long FileSize = GetFileSize(SendFilePaths[i]);
                Send(NetStream, BitConverter.GetBytes(FileSize));
                RecieveBytes(NetStream, MaxBufflen);
                for(int j = 0, x = 0; j < NumParcels; j++, x += MaxBufflen)
                {
                    byte[] Data = ReadPartOfBinaryFile(SendFilePaths[i], x, MaxBufflen);
                    Send(NetStream, BitConverter.GetBytes((int) Data.Length));
                    RecieveBytes(NetStream, MaxBufflen);
                    Send(NetStream, Data);
                    RecieveBytes(NetStream, MaxBufflen);
                }
            }
            return true; 
        }
        public static bool IsSpaceAvailableInCurrentFolder(long requiredSpaceInBytes)
        {
            if (requiredSpaceInBytes < 0)
                throw new ArgumentException("Required space must be a non-negative value.", nameof(requiredSpaceInBytes));

            string currentFolderPath = Environment.CurrentDirectory;

            string rootPath = Path.GetPathRoot(currentFolderPath);

            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException("Could not determine the root drive for the current folder.");

            DriveInfo driveInfo = new DriveInfo(rootPath);

            if (!driveInfo.IsReady)
                throw new InvalidOperationException($"The drive {rootPath} is not ready.");

            long availableSpace = driveInfo.AvailableFreeSpace;
            return availableSpace >= requiredSpaceInBytes;
        }
        public static int GetNumParcels(string filePath, int Bufflen)
        {
            long size = GetFileSize(filePath);
            return (int)((size + ((long)Bufflen - 1)) / (long)Bufflen);
        }
        public static long GetFileSize(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        public static byte[] GetFileName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string fileName = Path.GetFileName(filePath);

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("The file path does not contain a valid file name.", nameof(filePath));

            return Encoding.UTF8.GetBytes(fileName);
        }

        public static byte[] ReadPartOfBinaryFile(string filePath, long startPosition, int numberOfBytes)
        {
            if (numberOfBytes <= 0)
                throw new ArgumentException("Number of bytes to read must be greater than zero.", nameof(numberOfBytes));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            byte[] buffer = new byte[numberOfBytes];

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (startPosition < 0 || startPosition >= fileStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(startPosition), "Start position is outside the file range.");

                fileStream.Seek(startPosition, SeekOrigin.Begin);
                int bytesRead = fileStream.Read(buffer, 0, numberOfBytes);

                if (bytesRead < numberOfBytes)
                {
                    Array.Resize(ref buffer, bytesRead);
                }
            }

            return buffer;
        }
        public static void AppendOrCreateFile(string filePath, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty.", nameof(data));

            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message} ... {ex.StackTrace}", "ERROR");
            }
        }
        private static string? RecieveString(NetworkStream ns, int _MaxBufflen)
        {
            try
            {
                byte[] buffer = new byte[_MaxBufflen];
                int len = ns.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, len);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return null;
            }
        }
        private static bool Send(NetworkStream ns, string msg)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg.ToCharArray(), 0, msg.Length);
                ns.Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return false;
            }
        }
        public static byte[]? RecieveBytes(NetworkStream ns, int BuffSize)
        {
            try
            {
                byte[] buffer = new byte[BuffSize];
                int len = 0;
                while (len == 0)
                {
                    len = ns.Read(buffer, 0, buffer.Length);
                }
                return buffer;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " +  e.StackTrace, "ERROR");
                return null;
            }
        }
        public static bool Send(NetworkStream ns, byte[] buffer)
        {
            try
            {
                ns.Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return false;
            }
        }
        
    }
}
