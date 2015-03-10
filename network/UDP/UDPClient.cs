using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameNetwork
{
    public class UDPClient
    {
        public delegate void ReceivedEventHandler(IPEndPoint sender, byte[] data);
        public delegate void ExceptionEventHandler(int errorCode);
        public event ReceivedEventHandler Received;
        public event ExceptionEventHandler Exception;

        Thread waiter;
        
        Socket socket;
        IPEndPoint ipe;

        bool started;
        public UDPClient(IUDPListener lsn, string ip, int port)
        {
            if (lsn != null)
            {
                Received += lsn.OnUDPReceived;
                Exception += lsn.OnUDPException;
            }
            ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        }
        public void SetServerIP(string ip)
        {
            ipe = new IPEndPoint(IPAddress.Parse(ip), ipe.Port);
        }
        void Send(byte[] data)
        {
            socket.SendTo(data, ipe);
        }
        public void SendMessage(byte[] data)
        {
            byte[] temp = new byte[data.Length + 2];
            temp[0] = (byte)(data.Length / 256);
            temp[1] = (byte)(data.Length % 256);
            for (int i = 0; i < data.Length; i++)
                temp[i + 2] = data[i];
            Send(temp);
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
                byte[] d = new byte[1024];
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
