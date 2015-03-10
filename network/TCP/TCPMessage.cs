using System;

namespace GameNetwork
{
    public enum TCPMessageType : byte
    {
        Delay = 209,
        Full = 210,
        Normal = 213
    }
    public class TCPMessage
    {
        public TCPMessageType type;
        public byte[] data;
    }
}
