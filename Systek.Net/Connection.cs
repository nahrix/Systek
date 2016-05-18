using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Systek.Net
{
    /// <summary>
    /// General representation of a connection, that handles sending and receiving IMessage objects.
    /// </summary>
    /// <remarks>
    /// This class does not maintain its own connection.
    /// It is the responsibility of the caller to pass in a TcpClient and check for connectivity.
    /// If the connection is inactive, this object should be destroyed, and a new one created.
    /// </remarks>
    public class Connection : IConnection
    {
        public bool Connected { get; private set; }         // Represents whether the connection is active or not
        public int Timeout { get; set; }                    // The time, in milliseconds, for how long to wait for an expected message before timing out
        public Exception LastError { get; private set; }    // The last exception thrown in the _Receive thread

        public delegate void Logger(int typeID, int areaID,
            int serverID, string message);                  // This library doesn't implement a logger, so the caller passes in a delgate

        private Logger Log { get; set; }                    // The logger function passed in by the caller
        private TcpClient Peer { get; set; }                // The socket that this machine will be connected to
        private NetworkStream NetStream { get; set; }       // The stream that will read/write data between agent and server
        private List<Message> Messages { get; set; }        // The queue of messages that have already been read from the stream
        private Mutex MessageMutex { get; set; }            // Lock for the Messages list, since it will be accessed by multiple threads

        private const short HEADER_SIZE = sizeof(int);      // The size of the message header
        private const int MESSAGE_MAX = 65535;              // The maximum possible size of a Message
        private const int DEFAULT_TIMEOUT = 5000;           // The default value used for the Timeout property above

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peer">Remote machine that this is connecting to.</param>
        public Connection(TcpClient peer, Logger log)
        {
            Peer = peer;
            Log = log;
            Connected = false;
            Timeout = DEFAULT_TIMEOUT;
            MessageMutex = new Mutex();
            Connected = Peer.Connected;
        }

        /// <summary>
        /// Starts the message listener.
        /// </summary>
        public void Initialize()
        {
            Messages = new List<Message>();
            NetStream = Peer.GetStream();
            new Thread(new ThreadStart(_Receive)).Start();
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            NetStream.Close();
            Connected = false;
        }

        /// <summary>
        /// Sends a Message through the stream to the connected peer.
        /// </summary>
        /// <param name="msg">The Message to send.</param>
        public void Send(Message msg)
        {
            // Build the serialized message and header
            byte[] messageData = _Serialize(msg);
            byte[] headerData = BitConverter.GetBytes(messageData.Length);

            // Validate message size
            if (messageData.Length > MESSAGE_MAX)
            {
                throw new ArgumentOutOfRangeException("msg", "Message size is too large (> 65535 bytes).");
            }
            
            // Send the header
            NetStream.Write(headerData, 0, headerData.Length);

            // Send the message
            NetStream.Write(messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Get the queue of Messages received from the connected peer, and clear the existing queue.
        /// </summary>
        /// <returns>The queue of Messages.</returns>
        public List<Message> GetMessages()
        {
            // This will hold a copy of of the queue
            List<Message> messages = new List<Message>();

            try
            {
                // Lock access to the Message queue, and copy everything into messages,
                // then clear the queue
                MessageMutex.WaitOne();
                foreach (Message msg in Messages)
                {
                    messages.Add(msg);
                }
                Messages.Clear();
            }
            finally
            {
                // Allow access to the Message queue
                MessageMutex.ReleaseMutex();
            }

            return messages;
        }

        /// <summary>
        /// Runs in its own thread, listening for data on the stream, translating that data to Message objects,
        /// and filling the Message queue with the translated Messages.
        /// </summary>
        private void _Receive()
        {
            // Listen forever.  Loop is exited with return statements if an exception is caught.
            while (true)
            {
                Message msg;    // Incoming Message to be added to the Message queue

                try
                {
                    int bytesRead;                                  // How many bytes have been read so far
                    int bytesToRead;                                // How many bytes remain to be read
                    byte[] headerInput = new byte[HEADER_SIZE];     // Message header data; size is static
                    byte[] messageInput;                            // Message data

                    // Reset the variables for a fresh Message
                    bytesRead = 0;
                    bytesToRead = 0;
                    Array.Clear(headerInput, 0, headerInput.Length);
                    NetStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    // Read the header, which currently is just a short declaring the size of the upcoming message, in bytes
                    while (bytesRead < HEADER_SIZE)
                    {
                        bytesRead += NetStream.Read(headerInput, bytesRead, HEADER_SIZE - bytesRead);
                    }

                    // Interpret the message size as an int, and reset the variables for another read
                    bytesToRead = BitConverter.ToInt32(headerInput, 0);
                    bytesRead = 0;
                    Array.Clear(headerInput, 0, HEADER_SIZE);
                    messageInput = new byte[bytesToRead];  // Size of the incoming message is determined by the header
                    NetStream.ReadTimeout = Timeout;

                    // Read the message, and translate into a Message object
                    while (bytesRead < bytesToRead)
                    {
                        bytesRead += NetStream.Read(messageInput, bytesRead, bytesToRead - bytesRead);
                    }
                    msg = (Message)_Deserialize(messageInput);
                }
                catch (Exception e)
                {
                    // Save the exception as a property of this class, since it will be lost when the thread terminates
                    LastError = e;
                    Connected = false;
                    return;
                }

                // Add the message into the queue of messages to be read
                try
                {
                    MessageMutex.WaitOne();
                    Messages.Add(msg);
                }
                catch (Exception e)
                {
                    // Save the exception as a property of this class, since it will be lost when the thread terminates
                    LastError = e;
                    Connected = false;
                    return;
                }
                finally
                {
                    MessageMutex.ReleaseMutex();
                }
            }
        }

        // Serializes an object into a byte array
        private static byte[] _Serialize(object input)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(stream, input);
                return stream.ToArray();
            }
        }

        // Deserialzes a byte array into an object
        private static object _Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                return (new BinaryFormatter()).Deserialize(stream);
            }
        }

    }
}
