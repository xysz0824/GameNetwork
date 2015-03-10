using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameNetwork
{
    public class TCPServer
    {
        public delegate void JoinedEventHandler(TCPClientSocket client);
        public delegate void DisconnectedEventHandler(TCPClientSocket client);
        public delegate void ReceivedEventHandler(TCPClientSocket client, TCPMessage message);
        public event JoinedEventHandler Joined;
        public event DisconnectedEventHandler Disconnected;
        public event ReceivedEventHandler Received;

        Thread listener;

        IPEndPoint ipe;
        Socket socket;

        List<TCPClientSocket> clientpool;
        public List<TCPClientSocket> Clientpool { get { return clientpool; } }
        //the maximum of connections
        public int Capacity;

        bool started;
        public TCPServer(ITCPServerListener lsn, int port, int capacity)
        {
            if (lsn != null)
            {
                Joined += lsn.OnTCPJoined;
                Disconnected += lsn.OnTCPDisconnected;
                Received += lsn.OnTCPReceived;
            }
            this.Capacity = capacity;
            ipe = new IPEndPoint(IPAddress.Any, port);
            clientpool = new List<TCPClientSocket>();
        }
        public void Start()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipe);
            socket.Listen(10);
            started = true;
            listener = new Thread(ListenThread);
            listener.Start();
        }
        public void SafeClose()
        {
            started = false;
            for (int i = clientpool.Count; i > 0; i--)
                SafeClose(clientpool[0]);
            if (listener != null)
                listener.Abort();
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        public void SafeClose(TCPClientSocket target)
        {
            target.SafeClose();
            clientpool.Remove(target);
        }
        public void SendMessageToAll(TCPMessageType msgType, byte[] data)
        {
            foreach (var i in clientpool)
                SendMessage(i, msgType, data);
        }
        public void SendMessage(TCPClientSocket cilent, TCPMessageType msgType, byte[] data)
        {
            byte[] temp = new byte[data.Length + 3];
            temp[0] = (byte)msgType;
            temp[1] = (byte)(data.Length / 256);
            temp[2] = (byte)(data.Length % 256);
            for (int i = 0; i < data.Length; i++)
                temp[i + 3] = data[i];
            cilent.Send(temp);
        }
        public void TestDelay(TCPClientSocket client)
        {
            SendMessage(client, TCPMessageType.Delay, new byte[0]);
            client.delaywatch = new System.Diagnostics.Stopwatch();
            client.delaywatch.Start();
        }
        void ListenThread()
        {
            while (started)
            {
                Socket c = socket.Accept();
                int existIndex = -1;
                //Check if it's the same ip
                for (int i = 0; i < clientpool.Count; ++i)
                    if (((IPEndPoint)clientpool[i].Client.RemoteEndPoint).ToString().StartsWith(((IPEndPoint)c.RemoteEndPoint).ToString().Split(':')[0]))
                    {
                        existIndex = i;
                        break;
                    }
                if (clientpool.Count < Capacity || existIndex != -1)
                {
                    TCPClientSocket client = new TCPClientSocket(this, c);
                    client.Disconnected += Disconnected;
                    client.Received += Received;
                    if (existIndex == -1)
                        clientpool.Add(client);
                    else
                    {
                        clientpool[existIndex].SafeClose();
                        clientpool[existIndex] = client;
                    }
                    if (Joined != null)
                        Joined(client);
                }
                else
                {
                    TCPClientSocket client = new TCPClientSocket(this, c);
                    //Send a message to tell the client the server is full.
                    SendMessage(client, TCPMessageType.Full, new byte[0]);
                    client.Client.Disconnect(false);
                }
            }
        }
    }
}
