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
        public double Progress { get; private set; }
        private const int MaxBufflen = 1024;
        private List<byte[]> RecievedFileData = new List<byte[]>();
        private List<byte[]> SendFileData = new List<byte[]>();
        private List<string> RecievedFileNames = new List<string>();
        private List<string> SendFileNames = new List<string>();
        private TcpClient? Client = null;
        private TcpListener? Listener = null;
        private NetworkStream? NetStream = null;
        private IPEndPoint? iPEndPoint = null;
        public string IP = "127.0.0.1";
        public int Port = 5789;
        private bool ConnectionRunning = false;
        private List<string> SendFilePaths = new List<string>();
        public NetworkHandler() {}
        public NetworkHandler(int _Port) { Port = _Port; }
        public NetworkHandler(string _IP, int _Port) { Port = _Port; IP = _IP; }
        public bool AddFile(string Path)
        {
            if (File.Exists(Path))
            {
                SendFilePaths.Append(Path);
                return true;
            }
            else
            {
                MessageBox.Show("File: \'" + Path + "\' does not exsist!", "ERROR");
                return false;
            }
        }
        private Task SendFiles() {
            (SendFileData, SendFileNames) = ReadAllFiles();

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

            WriteAllFiles();

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
            for (int i = 0; i < NumFiles; i++)
            {
                List<byte[]> RecievedParcels = new List<byte[]>();
                List<int> RecievedParcelSizes = new List<int>();
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
            int NumFiles = SendFileNames.Count;
            Send(NetStream, NumFiles.ToString());
            for(int i = 0; i < NumFiles; i++)
            {
                Send(NetStream, SendFileNames[i]);
                List<byte[]> Parcels = new List<byte[]>();
                List<int> ParcelSizes = new List<int>();
                (Parcels, ParcelSizes) = ParseBytes(SendFileData[i], MaxBufflen);
                Send(NetStream, ParcelSizes.Count.ToString());
                for (int j = 0; j < ParcelSizes.Count; j++)
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
        private Tuple<List<byte[]>, List<string>> ReadAllFiles()
        {
            try {
                List<string> fileNames = new List<string>();
                List<byte[]> filesData = new List<byte[]>();
                foreach(string file in SendFilePaths)
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
                return new Tuple<List<byte[]>, List<string>>(filesData, fileNames);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ... " + e.StackTrace, "ERROR");
                return new Tuple<List<byte[]>, List<string>>(new List<byte[]>(), new List<string>());
            }
        }
        public static byte[] ReverseParseBytes(List<byte[]> Bytes, List<int> ParcelSizes)
        {
            int ArraySize = 0;
            foreach(int ParcelSize in ParcelSizes)
            {
                ArraySize += ParcelSize;
            }
            byte[] rval = new byte[ArraySize];
            int x = 0;
            int y = 0;

            foreach(int ParcelSize in ParcelSizes)
            {
                for(int i = 0; i < ParcelSize; i++)
                {
                    rval[x++] = Bytes[y][i];
                }
                y++;
            }
            
            return rval;
        }
        public static Tuple<List<byte[]>, List<int>> ParseBytes(byte[] Bytes, int ParcelSize)
        {
            List<byte[]> rval = new List<byte[]>();
            List<int> parcelSizes = new List<int>();
            for(int i = 0; i < Bytes.Length; i += ParcelSize)
            {
                byte[] bytesRead;
                int bytesSize;
                (bytesRead, bytesSize) = GetBytesBetween(Bytes, i, i + ParcelSize);
                rval.Append(bytesRead);
                parcelSizes.Append(bytesSize);
            }
            return new Tuple<List<byte[]>, List<int>>(rval, parcelSizes);
        }
        public static Tuple<byte[], int> GetBytesBetween(byte[] bytes, int begin, int end)
        {
            int size = 0;
            for (int i = begin; i < end && i < bytes.Length; i++)
            {
                size++;
            }
            byte[] rval = new byte[size];
            int x = 0;
            for(int i = begin; i < size; i++)
            {
                rval[x++] = bytes[i];
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
