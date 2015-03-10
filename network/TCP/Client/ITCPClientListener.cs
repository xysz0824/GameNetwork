using System;
using System.Collections.Generic;

using System.Text;

namespace GameNetwork
{
    public interface ITCPClientListener
    {
        void OnTCPConnecting();
        void OnTCPConnected();
        void OnTCPDisconnected(int errorCode);
        void OnTCPReceived(TCPMessage message);
    }
}
