using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameNetwork
{
    public class TCPClientSocket
    {
        public event TCPServer.DisconnectedEventHandler Disconnected;
        public event TCPServer.ReceivedEventHandler Received;
        
        public System.Diagnostics.Stopwatch delaywatch;
        long delay;
        public long Delay { get { return delay; } }
        TCPState state;
        public TCPState State { get { return state; } }
        TCPServer server;
        Socket client;
        public Socket Client { get { return client; } }
        Thread waiter;
        public IPEndPoint IPEndPoint { get { return (IPEndPoint)client.RemoteEndPoint; } }
        bool started;
        MessageHelper msgHelper;
        public TCPClientSocket(TCPServer server_T, Socket client_s)
        {
            msgHelper = new MessageHelper(ProcessMessage);
            server = server_T;
            client = client_s;
            state = TCPState.Connected;
            waiter = new Thread(Receive);
            started = true;
            waiter.Start();
        }
        public void Send(byte[] data)
        {
            client.Send(data);
        }
        public void SafeClose()
        {
            started = false;
            state = TCPState.Disconnected;
            waiter.Abort();
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        void Receive()
        {
            while (started)
            {
                var d = new byte[1024];
                try
                {
                    var size = client.Receive(d, d.Length, 0);
                    msgHelper.Subpackage(d, size);
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == (int)TCPState.Disconnected)
                        state = TCPState.Disconnected;
                    else
                        state = TCPState.Error;
                    if (Disconnected != null) Disconnected(this);
                    server.SafeClose(this);
                    break;
                }
            }
        }
        void ProcessMessage(byte[] data)
        {
            TCPMessage msg = new TCPMessage();
            switch (data[0])
            {
                case (byte)TCPMessageType.Delay:
                    if (delaywatch != null)
                    {
                        delaywatch.Stop();
                        delay = delaywatch.ElapsedMilliseconds;
                        delaywatch.Reset();
                    }
                    msg.type = TCPMessageType.Delay;
                    if (Received != null) Received(this, msg);
                    break;
                case (byte)TCPMessageType.Normal:
                    msg.data = new byte[(int)data[1] * 256 + (int)data[2]];
                    for (int i = 0; i < msg.data.Length; ++i)
                        msg.data[i] = data[i + 3];
                    msg.type = TCPMessageType.Normal;
                    if (Received != null) Received(this, msg);
                    break;
            }
        }
    }
}
