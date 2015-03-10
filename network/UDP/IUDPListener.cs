using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace GameNetwork
{
    public interface IUDPListener
    {
        void OnUDPException(int errorCode);
        void OnUDPReceived(IPEndPoint sender, byte[] data);
    }
}
