using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameNetwork
{
    public class TCPClient
    {
        public delegate void ConnectingEventHandler();
        public delegate void ConnectedEventHandler();
        public delegate void DisconnectedEventHandler(int errorCode);
        public delegate void ReceivedEventHandler(TCPMessage message);
        public event ConnectingEventHandler Connecting;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event ReceivedEventHandler Received;

        TCPState state = TCPState.Disconnected;
        public TCPState State { get { return state; } }
        Thread connecter;
        Thread receiver;
        IPEndPoint ipe;
        Socket socket;
        public Socket Socket { get { return socket; } }
        bool started;
        MessageHelper msgHelper;
        public TCPClient(ITCPClientListener lsn, string ip, int port)
        {
            msgHelper = new MessageHelper(ProcessMessage);
            if (lsn != null)
            {
                Connecting += lsn.OnTCPConnecting;
                Connected += lsn.OnTCPConnected;
                Disconnected += lsn.OnTCPDisconnected;
                Received += lsn.OnTCPReceived;
            }
            ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        void ConnectThread()
        {
            try
            {
                state = TCPState.Connecting;
                if (Connecting != null) Connecting();
                socket.Connect(ipe);
                //After connected : 
                state = TCPState.Connected;
                if (Connected != null) Connected();
                receiver = new Thread(ReceiveThread);
                started = true;
                receiver.Start();
            }
            catch (SocketException e)
            {
                if (Disconnected != null) Disconnected(e.ErrorCode);
                if (e.ErrorCode == (int)TCPState.Refused) state = TCPState.Refused;
                else if (e.ErrorCode == (int)TCPState.Timeout) state = TCPState.Timeout;
            }
        }
        public void Connect()
        {
            connecter = new Thread(ConnectThread);
            connecter.Start();
        }
        public void SafeClose()
        {
            if (connecter != null)
                connecter.Abort();
            started = false;
            state = TCPState.Disconnected;
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        void Send(byte[] data)
        {
            socket.Send(data);
        }
        public void SendMessage(TCPMessageType msgType, byte[] data)
        {
            byte[] temp = new byte[data.Length + 3];
            temp[0] = (byte)msgType;
            temp[1] = (byte)(data.Length / 256);
            temp[2] = (byte)(data.Length % 256);
            for (int i = 0; i < data.Length; i++)
                temp[i + 3] = data[i];
            Send(temp);
        }
        void ReceiveThread()
        {
            while (started)
            {
                var d = new byte[1024];
                try
                {
                    var size = socket.Receive(d);
                    msgHelper.Subpackage(d, size);
                }
                catch (SocketException e)
                {
                    if (Disconnected != null) Disconnected(e.ErrorCode);
                    if (e.ErrorCode == (int)TCPState.Disconnected) 
                        state = TCPState.Disconnected;
                    break;
                }
            }
        }
        void ProcessMessage(byte[] data)
        {
            TCPMessage msg = new TCPMessage();
            switch (data[0])
            {
                case (byte)TCPMessageType.Full:
                    if (Disconnected != null)
                        Disconnected((int)TCPState.Full);
                    state = TCPState.Disconnected;
                    break;
                case (byte)TCPMessageType.Delay:
                    SendMessage(TCPMessageType.Delay, new byte[0]);
                    break;
                case (byte)TCPMessageType.Normal:
                    msg.data = new byte[(int)data[1] * 256 + (int)data[2]];
                    for (int i = 0; i < msg.data.Length; ++i)
                        msg.data[i] = data[i + 3];
                    msg.type = TCPMessageType.Normal;
                    if (Received != null)
                        Received(msg);
                    break;
            }
        }
    }
}
