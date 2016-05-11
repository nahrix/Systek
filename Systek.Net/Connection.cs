using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Net
{
    abstract class Connection : IConnection
    {
        public bool Active { get; protected set; }
        public int Timeout { get; set; }

        protected TcpClient Peer { get; set; }
        protected IPEndPoint Binding { get; set; }
        protected NetworkStream NetStream { get; set; }
        protected List<Message> Messages { get; set; }
        protected Mutex MessageMutex { get; set; }

        public Connection(IPEndPoint binding)
        {
            Timeout = 5;
            Binding = binding;
            MessageMutex = new Mutex();
        }

        public void Initialize()
        {
            NetStream = new NetworkStream(Peer.Client, true);
            new Thread(new ThreadStart(_Receive)).Start();
        }

        public void Send(Message msg)
        {
            short size = (short)Marshal.SizeOf(typeof(Message));
            byte[] data;
            
            // Send the serialized header
            data = Serialize(size);
            NetStream.Write(data, 0, data.Length);

            // Send the serialized message
            data = Serialize(msg);
            NetStream.Write(data, 0, data.Length);
        }

        public List<Message> GetMessages()
        {
            MessageMutex.WaitOne();

            List<Message> messages = new List<Message>();
            foreach (Message msg in Messages)
            {
                messages.Add(msg);
            }
            Messages.Clear();

            MessageMutex.ReleaseMutex();

            return messages;
        }

        protected void _Receive()
        {
            int bytesRead;
            int bytesToRead;
            int headerSize = sizeof(short);
            byte[] input = new byte[65535];
            Message msg;

            while (true)
            {
                bytesRead = 0;
                bytesToRead = 0;
                Array.Clear(input, 0, input.Length);
                NetStream.ReadTimeout = System.Threading.Timeout.Infinite;

                // Read the header, which currently is just a short declaring the size of the upcoming message, in bytes
                while (bytesRead <= headerSize)
                {
                    bytesRead += NetStream.Read(input, bytesRead, headerSize - bytesRead);
                }

                // Interpret the message size as a short, and reset the variables for another read
                bytesToRead = (short)Deserialize(input);
                bytesRead = 0;
                Array.Clear(input, 0, headerSize);
                
                // Read the message
                while (bytesRead <= bytesToRead)
                {
                    bytesRead += NetStream.Read(input, bytesRead, bytesToRead - bytesRead);
                }
                msg = (Message)Deserialize(input);

                // Add the message into the queue of messages to be read
                MessageMutex.WaitOne();
                Messages.Add(msg);
                MessageMutex.ReleaseMutex();
            }
        }

        // Serializes an object into a byte array
        protected static byte[] Serialize(object input)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(stream, input);
                return stream.ToArray();
            }
        }

        // Deserialzes a byte array into an object
        protected static object Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                return (new BinaryFormatter()).Deserialize(stream);
            }
        }

    }
}
