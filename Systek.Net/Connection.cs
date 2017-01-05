using System;
using System.Collections.Concurrent;
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


        /// <summary>
        /// Keeps track of whether this object has already been disposed.
        /// </summary>
        private bool _Disposed;

        /// <summary>
        /// Occurs when logging is required.
        /// </summary>
        private event LogEventHandler _LogEvent;

        /// <summary>
        /// Occurs when a Message needs processing.
        /// </summary>
        private event MessageEventHandler _MessageEvent;

        /// <summary>
        /// Keep track of the delegate, for later removal
        /// </summary>
        private LogEventHandler _LogHandler;

        /// <summary>
        /// Keep track of the delegate, for later removal
        /// </summary>
        private MessageEventHandler _MessageHandler;

        /// <summary>
        /// The socket that this machine will be connected to
        /// </summary>
        private TcpClient _Peer { get; set; }

        /// <summary>
        /// The stream that will read/write data between agent and server
        /// </summary>
        private NetworkStream _NetStream { get; set; }

        /// <summary>
        /// Used for diagnostics.  Keeps track of the number of active class instantiations.
        /// </summary>
        private static int _ObjectCount = 0;
        
        /// <summary>
        /// Keeps the status of synchronous communications.  If the value (Message) is null,
        /// then this 
        /// </summary>
        private ConcurrentDictionary<int, Message> _SynchronizedMessages;

        /// <summary>
        /// The thread for receiving messages from the connected peer.
        /// </summary>
        private Thread _ReceiveThread;

        private string location;


        /// <summary>
        /// The size of the message header
        /// </summary>
        private const short HEADER_SIZE = sizeof(int);

        /// <summary>
        /// The maximum possible size of a Message
        /// </summary>
        private const int MESSAGE_MAX = 65535;

        /// <summary>
        /// The default value used for the Timeout property above
        /// </summary>
        private const int DEFAULT_TIMEOUT = 5000;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peer">Remote machine that this is connecting to.</param>
        /// <param name="logHandler">The function for handling log events.</param>
        /// <param name="messageHandler">The message handler.</param>
        public Connection(TcpClient peer, LogEventHandler logHandler, MessageEventHandler messageHandler, string loc)
        {
            location = loc;

            ++_ObjectCount;
            Connected = false;
            VerboseLogging = false;
            _Peer = peer;
            _LogHandler = logHandler;
            _MessageHandler = messageHandler;
            _LogEvent += logHandler;
            _MessageEvent += messageHandler;
            Timeout = DEFAULT_TIMEOUT;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Connection"/> class.
        /// </summary>
        ~Connection()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeManaged"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposeManaged)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposeManaged)
            {
                _Peer?.Dispose();
                _Peer = null;

                _NetStream?.Dispose();
                _NetStream = null;
            }

            Connected = false;

            if (_SynchronizedMessages != null)
            {
                _SynchronizedMessages.Clear();
            }
            _SynchronizedMessages = null;

            --_ObjectCount;
            if (VerboseLogging)
            {
                _LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Connection disposed.  ObjectCount: " + _ObjectCount.ToString()));
            }

            _MessageEvent -= _MessageHandler;
            _MessageEvent = null;
            _MessageHandler = null;

            _LogEvent -= _LogHandler;
            _LogEvent = null;
            _LogHandler = null;

            _Disposed = true;
        }

        /// <summary>
        /// Starts the message listener.
        /// </summary>
        public void Initialize()
        {
            // Terminate the receive thread if it's already running, so a new thread can be created.
            if (_ReceiveThread != null)
            {
                Close();
            }

            _SynchronizedMessages = new ConcurrentDictionary<int, Message>();
            _NetStream = _Peer.GetStream();
            _NetStream.ReadTimeout = Timeout;

            Connected = true;
            _ReceiveThread = new Thread(new ThreadStart(_Receive));
            _ReceiveThread.Start();

            Connected = _Peer.Connected;

            if (VerboseLogging)
            {
                _LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "New connection created.  ObjectCount: " + _ObjectCount.ToString()));
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
                _LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Connection is closing down."));
            }

            Connected = false;
            _NetStream?.Flush();
            _NetStream?.Dispose();
            _Peer?.Close();
        }

        /// <summary>
        /// Sends a Message through the stream to the connected peer.
        /// </summary>
        /// <param name="msg">The Message to send.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">msg;Message size is too large (> 65535 bytes).</exception>
        public void Send(Message msg)
        {
            Object lockObj = new Object();
            lock (lockObj)
            {
                // Build the serialized message and header
                byte[] messageData = _Serialize(msg);
                byte[] headerData = BitConverter.GetBytes(messageData.Length);

                // Validate message size
                if (messageData.Length > MESSAGE_MAX)
                {
                    _LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Message to send is too large. Message size: "
                        + messageData.Length.ToString() + ".  Max message size: " + MESSAGE_MAX.ToString()));
                }

                // Send the header and the message
                _NetStream.Write(headerData, 0, headerData.Length);
                _NetStream.Write(messageData, 0, messageData.Length);
            }
        }

        /// <summary>
        /// Runs in its own thread.  Listens for data on the stream, translates that data to Message objects,
        /// and processes the Messages.
        /// </summary>
        private void _Receive()
        {
            // Listen while the connection is up.
            while (Connected)
            {
                try
                {
                    // This buffer stores the incoming data from the network stream, to be converted into a Message object
                    byte[] inputBuffer;

                    // Read the header, which currently is just a short declaring the size of the upcoming message, in bytes.
                    // This first read operation does not time out before the first byte, because messages may occur between large intervals.
                    _NetStream.ReadTimeout = System.Threading.Timeout.Infinite;
                    if (!_ReadStream(HEADER_SIZE, out inputBuffer))
                    {
                        // If the read failed while we expected to be connected, then log the failure, and handle our side of the closure
                        // gracefully.
                        if (Connected)
                        {
                            _LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "The connection was closed unexpectedly."));
                            _MessageEvent?.Invoke(new Message { Type = MessageType.CLOSE });
                        }

                        Close();
                        break;
                    }

                    // Interpret the header as an int representing the amount of bytes incoming for the Message,
                    // and verify the requested size of the Message is valid
                    int bytesToRead = BitConverter.ToInt32(inputBuffer, 0);
                    if (bytesToRead > MESSAGE_MAX)
                    {
                        _LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Message is too large to receive.  Size requested: "
                            + bytesToRead + " bytes."));
                        Close();
                        break;
                    }

                    // Read the message
                    if (!_ReadStream(bytesToRead, out inputBuffer))
                    {
                        // If the read failed while we expected to be connected, then log the failure, and handle our side of the closure
                        // gracefully.
                        if (Connected)
                        {
                            _LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "The connection was closed unexpectedly."));
                            _MessageEvent?.Invoke(new Message { Type = MessageType.CLOSE });
                        }

                        Close();
                        break;
                    }

                    // Convert the byte array into a Message object
                    Message msg = (Message)_Deserialize(inputBuffer);

                    // Check for replies to a synchronous message.
                    if (msg.Synchronized && _SynchronizedMessages.ContainsKey(msg.SyncId))
                    {
                        Message originalMsg;
                        _SynchronizedMessages.TryGetValue(msg.SyncId, out originalMsg);
                        _SynchronizedMessages.TryUpdate(msg.SyncId, msg, originalMsg);
                        return;
                    }
                    else
                    {
                        // Pass the message to the message processor
                        _MessageEvent?.Invoke(msg);
                    }
                }
                catch (Exception e)
                {
                    // If we expected to be connected, log the failure, and close our side of the connection gracefully
                    if (Connected)
                    {
                        Connected = false;
                        _LogEvent?.Invoke(new LogEventArgs(Type.ERROR, AreaType.NET_LIB, "Exception caught while receiving data from peer.", e));
                        _MessageEvent?.Invoke(new Message { Type = MessageType.CLOSE });
                    }
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
                int bytesRead = _NetStream.Read(inputBuffer, totalBytesRead, bytesToRead - totalBytesRead);

                // This will occur if the socket has been closed
                if (bytesRead == 0)
                {
                    if (VerboseLogging)
                    {
                        _LogEvent?.Invoke(new LogEventArgs(Type.INFO, AreaType.NET_LIB, "Connection to peer has been closed."));
                    }
                    return false;
                }

                totalBytesRead += bytesRead;
                _NetStream.ReadTimeout = Timeout;
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
