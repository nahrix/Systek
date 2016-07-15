using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Systek.Net
{
    /// <summary>
    /// Base implementation of Systek.Net.IConnection.
    /// </summary>
    /// <seealso cref="Systek.Net.IConnection" />
    public class Connection : IConnection
    {
        /// <summary>
        /// Represents whether the connection is active or not
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// The time, in milliseconds, for how long to wait for an expected message before timing out
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether verbose logs should be written.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this library should write verbose logs; otherwise, <c>false</c>.
        /// </value>
        public bool VerboseLogging { get; set; }

        private event LogEventHandler LogEvent;         // Occurs when logging is required.
        private event MessageEventHandler MessageEvent; // Occurs when a Message needs processing.
        private TcpClient Peer { get; set; }            // The socket that this machine will be connected to
        private NetworkStream NetStream { get; set; }   // The stream that will read/write data between agent and server
        private const short HEADER_SIZE = sizeof(int);  // The size of the message header
        private const int MESSAGE_MAX = 65535;          // The maximum possible size of a Message
        private const int DEFAULT_TIMEOUT = 5000;       // The default value used for the Timeout property above

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peer">Remote machine that this is connecting to.</param>
        /// <param name="logHandler">The function for handling log events.</param>
        /// <param name="messageHandler">The message handler.</param>
        public Connection(TcpClient peer, LogEventHandler logHandler, MessageEventHandler messageHandler)
        {
            Connected = false;
            VerboseLogging = false;
            Peer = peer;
            LogEvent += logHandler;
            MessageEvent += messageHandler;
            Timeout = DEFAULT_TIMEOUT;
        }

        /// <summary>
        /// Starts the message listener.
        /// </summary>
        public void Initialize()
        {
            NetStream = Peer.GetStream();
            new Thread(new ThreadStart(_Receive)).Start();
            Connected = Peer.Connected;
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            Connected = false;
            NetStream.Close();
            Peer.Close();
        }

        /// <summary>
        /// Sends a Message through the stream to the connected peer.
        /// </summary>
        /// <param name="msg">The Message to send.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">msg;Message size is too large (> 65535 bytes).</exception>
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
        /// Runs in its own thread.  Listens for data on the stream, translates that data to Message objects,
        /// and processes the Messages.
        /// </summary>
        private void _Receive()
        {
            // Listen forever.  Loop exits if an exception is caught
            while (true)
            {
                try
                {
                    Message msg;                                // Incoming Message to be added to the Message queue
                    int bytesRead;                              // How many bytes have been read so far
                    int bytesToRead;                            // How many bytes remain to be read
                    byte[] headerInput = new byte[HEADER_SIZE]; // Message header data; size is static
                    byte[] messageInput;                        // Message data

                    // Reset the variables for a fresh Message
                    bytesRead = 0;
                    bytesToRead = 0;
                    Array.Clear(headerInput, 0, headerInput.Length);
                    NetStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    if (VerboseLogging)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Reading for header."));
                    }

                    // Read the header, which currently is just a short declaring the size of the upcoming message, in bytes
                    while (bytesRead < HEADER_SIZE)
                    {
                        bytesRead += NetStream.Read(headerInput, bytesRead, HEADER_SIZE - bytesRead);
                    }

                    // Interpret the header as an int representing the amount of bytes incoming for the Message,
                    // and verify the requested size of the Message is valid
                    bytesToRead = BitConverter.ToInt32(headerInput, 0);
                    if (bytesToRead > MESSAGE_MAX)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Message is too large to receive.  Size requested: "
                            + bytesToRead + " bytes."));
                        Close();
                        break;
                    }

                    bytesRead = 0;
                    Array.Clear(headerInput, 0, HEADER_SIZE);
                    messageInput = new byte[bytesToRead];  // Size of the incoming message is determined by the header
                    NetStream.ReadTimeout = Timeout;

                    if (VerboseLogging)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Reading for message of size: " + bytesToRead.ToString()));
                    }

                    // Read the message, and translate into a Message object
                    while (bytesRead < bytesToRead)
                    {
                        bytesRead += NetStream.Read(messageInput, bytesRead, bytesToRead - bytesRead);
                    }
                    msg = (Message)_Deserialize(messageInput);

                    // Pass the message to the message processor
                    MessageEvent?.Invoke(msg);
                }
                catch (Exception e)
                {
                    // Only log if the failure was unexpected; ie, during an active connection.
                    if (Connected)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Exception caught while receiving data from peer.", e));
                        Close();
                    }
                    Connected = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Serializes an object into a byte array
        /// </summary>
        /// <param name="input">The object to be serialized.</param>
        /// <returns></returns>
        private static byte[] _Serialize(object input)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(stream, input);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserialzes a byte array into an object
        /// </summary>
        /// <param name="data">The byte array to be deserialized.</param>
        /// <returns></returns>
        private static object _Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                return (new BinaryFormatter()).Deserialize(stream);
            }
        }

    }
}
