using System;
using System.Collections.Generic;
using System.Text;

namespace GameNetwork
{
    class MessageHelper
    {
        List<byte> buffer;
        public delegate void ProcessMessage(byte[] data);
        public ProcessMessage action;
        public MessageHelper(ProcessMessage action)
        {
            this.action = action;
            buffer = new List<byte>();
        }
        public void Subpackage(byte[] data, int size)
        {
            for (int i = 0; i < size; ++i)
            {
                //Receive the rest of message.
                if (buffer.Count > 0)
                {
                    if (!Enum.IsDefined(typeof(TCPMessageType), data[i]))
                    {
                        buffer.Add(data[i]);
                        continue;
                    }
                    else
                        CheckBuffer();
                }
                if (Enum.IsDefined(typeof(TCPMessageType), data[i]))
                {
                    //If it has a full header
                    if (i + 3 > size) break;
                    var length = (int)data[i + 1] * 256 + (int)data[i + 2];
                    //If the length in the size of data
                    if (i + 3 + length > size)
                    {
                        var part = new byte[size - i];
                        for (int k = 0; k < size - i; ++k)
                            part[k] = data[i + k];
                        buffer.AddRange(part);
                        break;
                    }
                    else
                    {
                        //Received a complete message
                        var copy = new byte[3 + length];
                        for (int k = 0; k < 3 + length; ++k)
                            copy[k] = data[i + k];
                        action(copy);
                        i += 3 + length;
                    }
                }
            }
        }
        void CheckBuffer()
        {
            if (buffer.Count >= 3)
            {
                var length = (int)buffer[1] * 256 + (int)buffer[2];
                if (2 + length < buffer.Count)
                {
                    //Received a complete message
                    var copy = new byte[3 + length];
                    for (int k = 0; k < 3 + length; ++k)
                        copy[k] = buffer[k];
                    action(copy);
                    buffer.RemoveRange(0, 3 + length);
                }
            }
        }
    }
}
