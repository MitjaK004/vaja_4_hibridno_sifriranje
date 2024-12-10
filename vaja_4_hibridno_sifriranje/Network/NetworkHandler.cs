using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using vaja_4_hibridno_sifriranje.ViewModelNamespace;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public double FileTransferProgress { get; private set; } = 0;
        public double PacketShare { get; private set; } = 0;
        public int NumPackets { get; private set; } = 0;
        private ViewModel VM;
        private const int MaxBufflen = 4096;
        private const int DataBufflen = 4112;
        private List<byte[]> RecievedFileData = new List<byte[]>();
        private List<byte[]> SendFileData = new List<byte[]>();
        private List<string> RecievedFileNames = new List<string>();
        private List<string> SendFilePaths = new List<string>();
        private TcpClient? Client = null;
        private TcpListener? Listener = null;
        private NetworkStream? NetStream = null;
        private IPEndPoint? iPEndPoint = null;
        private byte[] AesIV = null;
        private byte[] SharedSecret = null;
        private byte[] AesKey = null;
        public string IP = "127.0.0.1";
        public int Port = 5789;
        private bool ConnectionRunning = false;
        private byte[] success = { 1, 2, 3, 4, 5 };
        private byte[] fail = { 7, 8, 9, 10, 11 };
        private byte[] nl = { (byte)'\n', (byte)'\n' };
        public NetworkHandler() {}
        public NetworkHandler(int _Port) { Port = _Port; }
        public NetworkHandler(string _IP, int _Port) { Port = _Port; IP = _IP; }
        public NetworkHandler(ViewModel _VM)
        {
            VM = _VM;
        }
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
        public void Clear()
        {
            SendFilePaths.Clear();
        }
        private Task SendFiles() {
            VM.Title = ViewModel.WindowTitleSend;
            VM.ConnectionStatus = Status.Wait.ToString();
            FileTransferProgress = 0;
            VM.FilesTransferProgress = ViewModel.ProgressToString(FileTransferProgress);
            iPEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);

            Client = new TcpClient();
            Client.Connect(iPEndPoint);

            using (NetStream = Client.GetStream())
            {
                SharedSecret = DiffieHellmanHelper.PerformHandshakeClient(NetStream, out AesIV);

                AesKey = AESHelper.Encrypt(SharedSecret, SharedSecret, AesIV)[..32];

                VM.ConnectionStatus = Status.Success.ToString();

                try
                {
                    SendAll();
                }
                catch (Exception e) {
                    MessageBox.Show(e.Message + " \n " + e.StackTrace, "ERROR");
                }
                finally
                {
                    MessageBox.Show("Files Sent Succesfully", "Info");
                }
            }
            VM.ConnectionStatus = Status.Stopped.ToString();
            Client?.Close();
            VM.Title = ViewModel.WindowTitle;
            return Task.CompletedTask;
        }
        private Task RecieveFiles() {
            VM.Title = ViewModel.WindowTitleRecieve;
            VM.ConnectionStatus = Status.Wait.ToString();
            FileTransferProgress = 0;
            VM.FilesTransferProgress = ViewModel.ProgressToString(FileTransferProgress);
            (NumPackets, PacketShare) = CalculateAllPackets();
            iPEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            Listener = new TcpListener(iPEndPoint);

            Listener.Start();

           using (Client = Listener.AcceptTcpClient())
           using (NetStream = Client.GetStream())
           {
                SharedSecret = DiffieHellmanHelper.PerformHandshakeServer(NetStream, out AesIV);

                AesKey = AESHelper.Encrypt(SharedSecret, SharedSecret, AesIV)[..32];

                VM.ConnectionStatus = Status.Success.ToString();

                try { 
                    RecieveAll();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + " \n " + e.StackTrace, "ERROR");
                }
                finally
                {
                    MessageBox.Show("Files Recieved Succesfully", "Info");
                }
            }

            Listener.Stop();

            VM.ConnectionStatus = Status.Stopped.ToString();
            VM.Title = ViewModel.WindowTitle;
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
            VM.FilesTransferStatus = Status.Wait.ToString();
            VM.FilesTransferProgress = "0%";
            Send(NetStream, success, AesKey, AesIV);
            byte[] progressData = RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
            (NumPackets, PacketShare) = GetIntDoubleFromBytes(progressData);
            Send(NetStream, success, AesKey, AesIV);
            int NumFiles = int.Parse(RecieveString(NetStream, MaxBufflen, AesKey, AesIV));
            Send(NetStream, success, AesKey, AesIV);
            for (int i = 0; i < NumFiles; i++)
            {
                string fileName = RecieveString(NetStream, MaxBufflen, AesKey, AesIV);
                Send(NetStream, success, AesKey, AesIV);
                int NumParcels = BitConverter.ToInt32(RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV), 0);
                Send(NetStream, success, AesKey, AesIV);
                long FileSize = BitConverter.ToInt64(RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV), 0);
                if (IsSpaceAvailableInCurrentFolder(FileSize)) {
                    DeleteFileIfExists(fileName);
                    Send(NetStream, success, AesKey, AesIV);
                    for(int j = 0; j < NumParcels; j++)
                    {
                        int ParcelLength = BitConverter.ToInt32(RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV));
                        Send(NetStream, success, AesKey, AesIV);
                        byte[] Data = RecieveBytes(NetStream, DataBufflen, AesKey, AesIV);
                        Array.Resize(ref Data, ParcelLength);
                        AppendOrCreateFile(fileName, Data);
                        Send(NetStream, success, AesKey, AesIV);
                        FileTransferProgress += PacketShare;
                        VM.FilesTransferProgress = ViewModel.ProgressToString(FileTransferProgress);
                    }  
                }
                else {
                    Send(NetStream, fail, AesKey, AesIV);
                    MessageBox.Show("Not enough disk space!!!", "ERROR");
                    return false;
                }
            }
            VM.FilesTransferProgress = "100%";
            VM.FilesTransferStatus = Status.Success.ToString();
            return true; 
        }
        private bool SendAll() {
            VM.FilesTransferStatus = Status.Wait.ToString();
            int NumFiles = SendFilePaths.Count;
            RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
            (NumPackets, PacketShare) = CalculateAllPackets();
            byte[] progressData = JoinToBytes(NumPackets, PacketShare);
            Send(NetStream, progressData, AesKey, AesIV);
            RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV );
            Send(NetStream, NumFiles.ToString(), AesKey, AesIV);
            RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
            for(int i = 0; i < NumFiles; i++)
            {
                Send(NetStream, GetFileName(SendFilePaths[i]), AesKey, AesIV);
                RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
                int NumParcels = GetNumParcels(SendFilePaths[i], MaxBufflen);
                Send(NetStream, BitConverter.GetBytes(NumParcels), AesKey, AesIV);
                RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
                long FileSize = GetFileSize(SendFilePaths[i]);
                Send(NetStream, BitConverter.GetBytes(FileSize), AesKey, AesIV);
                RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
                for(int j = 0, x = 0; j < NumParcels; j++, x += MaxBufflen)
                {
                    byte[] Data = ReadPartOfBinaryFile(SendFilePaths[i], x, MaxBufflen);
                    Send(NetStream, BitConverter.GetBytes((int) Data.Length), AesKey, AesIV);
                    RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
                    Send(NetStream, Data, AesKey, AesIV);
                    RecieveBytes(NetStream, MaxBufflen, AesKey, AesIV);
                    NetStream.Flush();
                    FileTransferProgress += PacketShare;
                    VM.FilesTransferProgress = ViewModel.ProgressToString(FileTransferProgress);
                }
            }
            VM.FilesTransferProgress = "100%";
            VM.FilesTransferStatus = Status.Success.ToString();
            return true; 
        }
        private Tuple<int, double> CalculateAllPackets()
        {
            int AllPackets = 0;
            foreach(var SendFilePath in SendFilePaths)
            {
                AllPackets += GetNumParcels(SendFilePath, MaxBufflen);
            }
            double OnePacketShare = 1.0 / (double)AllPackets;
            return new Tuple<int, double>(AllPackets, OnePacketShare);
        }
        private static byte[] JoinToBytes(int a, double b)
        {
            byte[] bytes = new byte[4+8];
            byte[] _a = BitConverter.GetBytes(a);
            byte[] _b = BitConverter.GetBytes(b);
            Array.Copy(_a, 0, bytes, 0, _a.Length);
            Array.Copy(_b, 0, bytes, _a.Length, _b.Length);
            return bytes;
        }
        private static Tuple<int, double> GetIntDoubleFromBytes(byte[] bytes)
        {
            byte[] _a = new byte[4], _b = new byte[8];
            Array.Copy(bytes, 0, _a, 0, 4);
            Array.Copy(bytes, 4, _b, 0, 8);
            int a = BitConverter.ToInt32(_a);
            double b = BitConverter.ToDouble(_b);
            return new Tuple<int, double>(a, b);
        }
        private static void DeleteFileIfExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file '{filePath}': {ex.Message}");
                }
            }
        }
        private static bool IsSpaceAvailableInCurrentFolder(long requiredSpaceInBytes)
        {
            if (requiredSpaceInBytes < 0)
                throw new ArgumentException("Required space must be a non-negative value.", nameof(requiredSpaceInBytes));

            string currentFolderPath = Environment.CurrentDirectory;

            string rootPath = System.IO.Path.GetPathRoot(currentFolderPath);

            if (string.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException("Could not determine the root drive for the current folder.");

            DriveInfo driveInfo = new DriveInfo(rootPath);

            if (!driveInfo.IsReady)
                throw new InvalidOperationException($"The drive {rootPath} is not ready.");

            long availableSpace = driveInfo.AvailableFreeSpace;
            return availableSpace >= requiredSpaceInBytes;
        }
        private static int GetNumParcels(string filePath, int Bufflen)
        {
            long size = GetFileSize(filePath);
            return (int)((size + ((long)Bufflen - 1)) / (long)Bufflen);
        }
        private static long GetFileSize(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        private static byte[] GetFileName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string fileName = System.IO.Path.GetFileName(filePath);

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("The file path does not contain a valid file name.", nameof(filePath));

            return Encoding.UTF8.GetBytes(fileName);
        }

        private static byte[] ReadPartOfBinaryFile(string filePath, long startPosition, int numberOfBytes)
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
        private static void AppendOrCreateFile(string filePath, byte[] data)
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
                MessageBox.Show($"An error occurred: {ex.Message} \n {ex.StackTrace}", "ERROR");
            }
        }
        private static bool Send(NetworkStream ns, byte[] buffer, byte[] aesKey, byte[] aesIv)
        {
            try
            {
                byte[] encryptedData = AESHelper.Encrypt(buffer, aesKey, aesIv);
                ns.Write(encryptedData, 0, encryptedData.Length);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " \n " + e.StackTrace, "ERROR");
                return false;
            }
        }

        private static bool Send(NetworkStream ns, string buffer, byte[] aesKey, byte[] aesIv)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(buffer);
            return Send(ns, stringBytes, aesKey, aesIv);
        }

        private static byte[]? RecieveBytes(NetworkStream ns, int buffSize, byte[] aesKey, byte[] aesIv)
        {
            try
            {
                byte[] buffer = new byte[buffSize];
                int bytesRead = ns.Read(buffer, 0, buffSize);
                if (bytesRead == 0) return null;

                byte[] actualData = new byte[bytesRead];
                Array.Copy(buffer, actualData, bytesRead);

                return AESHelper.Decrypt(actualData, aesKey, aesIv);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message + " \n " + e.StackTrace, "ERROR");
                return null;
            }
        }

        private static string? RecieveString(NetworkStream ns, int maxBufflen, byte[] aesKey, byte[] aesIv)
        {
            byte[]? decryptedData = RecieveBytes(ns, maxBufflen, aesKey, aesIv);
            if (decryptedData == null) return null;
            return Encoding.UTF8.GetString(decryptedData);
        }

    }
}
