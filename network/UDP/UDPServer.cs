using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameNetwork
{
    public class UDPServer
    {
        public delegate void ReceivedEventHandler(IPEndPoint sender, byte[] data);
        public delegate void ExceptionEventHandler(int errorCode);
        public event ReceivedEventHandler Received;
        public event ExceptionEventHandler Exception;

        Thread waiter;

        IPEndPoint ipe;
        Socket socket;

        bool started;
        public UDPServer(IUDPListener lsn, int port)
        {
            if (lsn != null)
            {
                Received += lsn.OnUDPReceived;
                Exception += lsn.OnUDPException;
            }
            ipe = new IPEndPoint(IPAddress.Any, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            socket.Bind(ipe);
        }
        void Send(byte[] data, string ip, int port)
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            socket.SendTo(data, ipe);
        }
        public void SendMessage(byte[] data, string ip, int port)
        {
            byte[] temp = new byte[data.Length + 2];
            temp[0] = (byte)(data.Length / 256);
            temp[1] = (byte)(data.Length % 256);
            for (int i = 0; i < data.Length; i++)
                temp[i + 2] = data[i];
            Send(temp, ip, port);
        }
        public void SendMessage(byte[] data, IPEndPoint target)
        {
            SendMessage(data, target.Address.ToString(), target.Port);
        }
        public void Start()
        {
            if (!started)
            {
                started = true;
                waiter = new Thread(Receive);
                waiter.Start();
            }
        }
        public void Close()
        {
            started = false;
            if (waiter != null)
                waiter.Abort();
        }
        void Receive()
        {
            while (started)
            {
                var d = new byte[1024];
                EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    socket.ReceiveFrom(d, ref sender);
                    byte[] data = new byte[(int)d[0] * 256 + (int)d[1]];
                    for (int i = 0; i < data.Length; ++i)
                        data[i] = d[i + 2];
                    if (Received != null)
                        Received((IPEndPoint)sender, data);
                }
                catch (SocketException e)
                {
                    if (Exception != null)
                        Exception(e.ErrorCode);
                    break;
                }
            }
        }
    }
}
