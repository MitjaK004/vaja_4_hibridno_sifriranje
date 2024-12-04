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

            ConnectionRunning = true;
            while (ConnectionRunning)
            {
                Client = new TcpClient();
                Client.Connect(iPEndPoint);

                NetStream = Client.GetStream();
                RecieveAll();
            }
            return Task.CompletedTask;
        }
        private Task RecieveFiles() {
            iPEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            Listener = new TcpListener(iPEndPoint);

            Listener.Start();
            ConnectionRunning = true;

            while (ConnectionRunning)
            {
                using (Client = Listener.AcceptTcpClient())
                using (NetStream = Client.GetStream())
                {
                    RecieveAll();
                }
            }

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
        private bool RecieveAll() { return true; }
        private bool SendAll() { return true; }
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
        private static string? RecieveString(NetworkStream ns, int MaxBufflen)
        {
            try
            {
                byte[] buffer = new byte[MaxBufflen];
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
