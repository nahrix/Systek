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
        private static int ObjectCount = 0;             // Used for diagnostics.  Keeps track of the number of active class instantiations.

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peer">Remote machine that this is connecting to.</param>
        /// <param name="logHandler">The function for handling log events.</param>
        /// <param name="messageHandler">The message handler.</param>
        public Connection(TcpClient peer, LogEventHandler logHandler, MessageEventHandler messageHandler)
        {
            ++ObjectCount;
            Connected = false;
            VerboseLogging = false;
            Peer = peer;
            LogEvent += logHandler;
            MessageEvent += messageHandler;
            Timeout = DEFAULT_TIMEOUT;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Connection"/> class.
        /// </summary>
        ~Connection()
        {
            --ObjectCount;
            if (VerboseLogging)
            {
                LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Connection destroyed.  ObjectCount: " + ObjectCount.ToString()));
            }
        }

        /// <summary>
        /// Starts the message listener.
        /// </summary>
        public void Initialize()
        {
            NetStream = Peer.GetStream();
            NetStream.ReadTimeout = Timeout;
            new Thread(new ThreadStart(_Receive)).Start();
            Connected = Peer.Connected;

            if (VerboseLogging)
            {
                LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "New connection created.  ObjectCount: " + ObjectCount.ToString()));
            }
        }

        /// <summary>
        /// Gracefully closes the connection.
        /// </summary>
        public void Close()
        {
            if (!Connected)
            {
                return;
            }

            if (VerboseLogging)
            {
                LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Connection is closing down."));
            }

            Connected = false;
            NetStream.Dispose();
            NetStream = null;
            Peer.Close();
            Peer = null;
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
                LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Message to send is too large. Message size: "
                    + messageData.Length.ToString() + ".  Max message size: " + MESSAGE_MAX.ToString()));
            }
            
            // Send the header and the message
            NetStream.Write(headerData, 0, headerData.Length);
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
                    // This buffer stores the incoming data from the network stream, to be converted into a Message object
                    byte[] inputBuffer;

                    // Read the header, which currently is just a short declaring the size of the upcoming message, in bytes.
                    // This first read operation does not time out before the first byte, because messages may occur between large intervals.
                    NetStream.ReadTimeout = System.Threading.Timeout.Infinite;
                    if (!_ReadStream(HEADER_SIZE, out inputBuffer))
                    {
                        // If the read failed while we expected to be connected, then log the failure, and handle our side of the closure
                        // gracefully.
                        if (Connected)
                        {
                            LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "The connection was closed unexpectedly."));
                            MessageEvent?.Invoke(new Message { Type = MessageType.CLOSE });
                        }

                        Close();
                        break;
                    }

                    // Interpret the header as an int representing the amount of bytes incoming for the Message,
                    // and verify the requested size of the Message is valid
                    int bytesToRead = BitConverter.ToInt32(inputBuffer, 0);
                    if (bytesToRead > MESSAGE_MAX)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Message is too large to receive.  Size requested: "
                            + bytesToRead + " bytes."));
                        Close();
                        break;
                    }

                    // Read the message
                    if (!_ReadStream(bytesToRead, out inputBuffer))
                    {
                        Close();
                        break;
                    }

                    // Convert the byte array into a Message object
                    Message msg = (Message)_Deserialize(inputBuffer);

                    // Pass the message to the message processor
                    MessageEvent?.Invoke(msg);
                }
                catch (Exception e)
                {
                    // If we expected to be connected, log the failure, and close our side of the connection gracefully
                    if (Connected)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Exception caught while receiving data from peer.", e));
                        MessageEvent?.Invoke(new Message { Type = MessageType.CLOSE });
                    }
                    Close();
                    break;
                }
            }
        }

        /// <summary>
        /// Reads a specified number of bytes from the network stream into a byte array.
        /// </summary>
        /// <param name="bytesToRead">The number of bytes to read.</param>
        /// <param name="inputBuffer">The buffer to hold the input from the network stream.</param>
        /// <returns></returns>
        private bool _ReadStream(int bytesToRead, out byte[] inputBuffer)
        {
            // Initialize the input buffer
            inputBuffer = new byte[bytesToRead];

            // Keep track of the total number of bytes read, in case the read occurs in multiple passes
            int totalBytesRead = 0;

            // Loop read operation until the specified number of bytes have been read from the network stream
            while (totalBytesRead < bytesToRead)
            {
                int bytesRead = NetStream.Read(inputBuffer, totalBytesRead, bytesToRead - totalBytesRead);

                // This will occur if the socket has been closed
                if (bytesRead == 0)
                {
                    if (VerboseLogging)
                    {
                        LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Connection to peer has been closed."));
                    }
                    return false;
                }

                totalBytesRead += bytesRead;
                NetStream.ReadTimeout = Timeout;
            }

            return true;
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
