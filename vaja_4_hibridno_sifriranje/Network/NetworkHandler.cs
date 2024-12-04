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
        public Status status { get; private set; } = Status.Stopped;
        private const int MaxBufflen = 1024;
        private byte[][]? RecievedFileData = null;
        private byte[][]? SendFileData = null;
        private string[]? RecievedFileNames = null;
        private string[]? SendFileNames = null;
        private TcpClient? Client = null;
        private TcpListener? Listener = null;
        private NetworkStream? NetStream = null;
        private IPEndPoint? iPEndPoint = null;
        private string IP = "127.0.0.1";
        private int Port = 5789;
        private bool ConnectionRunning = false;
        private ObservableCollection<string> files = new ObservableCollection<string>();
        public NetworkHandler() {}
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
        void Sender()
        {
            Task.Run(SendFiles);
        }

        void Reciever()
        {
            Task.Run(RecieveFiles);
        }
        Status GetStatus()
        {
            return status;
        }
        private bool RecieveAll() {
            int NumFiles = int.Parse(RecieveString(NetStream, MaxBufflen));
            for (int i = 0; i < NumFiles; i++)
            {
                byte[][] RecievedParcels = new byte[0][];
                int[] RecievedParcelSizes = new int[0];
                string FileName = RecieveString(NetStream, MaxBufflen);
                int NumParcels = int.Parse(RecieveString(NetStream, MaxBufflen));
                for(int j = 0; j < NumParcels; j++)
                {
                    int ParcelSize = int.Parse(RecieveString(NetStream, MaxBufflen));
                    byte[] Parcel = RecieveBytes(NetStream, MaxBufflen);
                    RecievedParcelSizes.Append(ParcelSize);
                    RecievedParcels.Append(Parcel);
                }
                byte[] FileData = ReverseParseBytes(RecievedParcels, RecievedParcelSizes);
                RecievedFileNames.Append(FileName);
                RecievedFileData.Append(FileData);
            }
            return true; 
        }
        private bool SendAll() {
            int NumFiles = SendFileNames.Length;
            Send(NetStream, NumFiles.ToString());
            for(int i = 0; i < NumFiles; i++)
            {
                Send(NetStream, SendFileNames[i]);
                byte[][] Parcels = new byte[0][];
                int[] ParcelSizes = new int[0];
                (Parcels, ParcelSizes) = ParseBytes(SendFileData[i], MaxBufflen);
                Send(NetStream, ParcelSizes.Length.ToString());
                for (int j = 0; j < ParcelSizes.Length; j++)
                {
                    Send(NetStream, ParcelSizes[j].ToString());
                    Send(NetStream, Parcels[j]);
                }
            }
            return true; 
        }
        private bool WriteAllFiles()
        {
            try {
                if (RecievedFileNames != null || RecievedFileData != null)
                {
                    int i = 0;
                    foreach (string file in RecievedFileNames)
                    {
                        if (RecievedFileData[i] != null)
                        {
                            WriteFile(file, RecievedFileData[i]);
                        }
                        else
                        {
                            throw new Exception("File data missng!");
                        }
                        i++;
                    }
                }
                else
                {
                    throw new Exception("Nothing to Write!");
                }
                return true;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return false;
            }
        }
        private Tuple<byte[][]?, string[]?> ReadAllFiles()
        {
            try {
                string[]? fileNames = new string[0];
                byte[][]? filesData = new byte[0][];
                foreach(string file in files)
                {
                    if (File.Exists(file))
                    {
                        filesData.Append(ReadFile(file));
                        string[] fp = file.Split('\\');
                        fileNames.Append(fp[fp.Length - 1]);
                    }
                    else
                    {
                        throw new FileNotFoundException(file);
                    }
                }
                return new Tuple<byte[][]?, string[]?>(filesData, fileNames);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return new Tuple<byte[][]?, string[]?>(null, null);
            }
        }
        public static byte[] ReverseParseBytes(byte[][] Bytes, int[] ParcelSizes)
        {
            byte[] rval = new byte[0];
            int y = 0;

            foreach(int ParcelSize in ParcelSizes)
            {
                for(int i = 0; i < ParcelSize; i++)
                {
                    rval.Append(Bytes[y][i]);
                }
                y++;
            }
            
            return rval;
        }
        public static Tuple<byte[][], int[]> ParseBytes(byte[] Bytes, int ParcelSize)
        {
            byte[][] rval = new byte[0][];
            int[] parcelSizes = new int[0];
            for(int i = 0; i < Bytes.Length; i += ParcelSize)
            {
                byte[] bytesRead;
                int bytesSize;
                (bytesRead, bytesSize) = GetBytesBetween(Bytes, i, i + ParcelSize);
                rval.Append(bytesRead);
                parcelSizes.Append(bytesSize);
            }
            return new Tuple<byte[][], int[]>(rval, parcelSizes);
        }
        public static Tuple<byte[], int> GetBytesBetween(byte[] bytes, int begin, int end)
        {
            byte[] rval = new byte[0];
            int size = 0;
            for(int i = begin; i < end && i < bytes.Length; i++)
            {
                rval.Append(bytes[i]);
                size++;
            }
            return new Tuple<byte[], int>(rval, size);
        }
        public static byte[]? ReadFile(string fileName)
        {
            try {
                byte[] fileBytes = File.ReadAllBytes(fileName);
                return fileBytes;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " ... " + ex.StackTrace, "ERROR");
                return null;
            }
        }
        public static bool WriteFile(string fileName, byte[] fileBytes)
        {
            try {
                File.WriteAllBytes(fileName, fileBytes);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return false;
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
