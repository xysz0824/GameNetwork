using System;
using System.Collections.Generic;

using System.Text;

namespace GameNetwork
{
    public interface ITCPServerListener
    {
        void OnTCPJoined(TCPClientSocket client);
        void OnTCPDisconnected(TCPClientSocket client);
        void OnTCPReceived(TCPClientSocket sender, TCPMessage message);
    }
}
