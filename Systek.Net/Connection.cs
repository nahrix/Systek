﻿using System;
using System.Collections.Generic;
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
        /// This library doesn't implement a logger, so the caller passes in a delgate
        /// </summary>
        /// <param name="typeID">Type of log, definted in tblType (1 for error, 2 for info, etc)</param>
        /// <param name="message">The message to write to the log</param>
        public delegate void Logger(int typeID, string message);

        private event LogEventHandler LogEvent;         // Occurs when logging is required.
        private event ExecuteEventHandler ExecuteEvent; // Occurs when a CommandSet needs to be executed.
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
        /// <param name="executeHandler">The function for handling the execution events.</param>
        public Connection(TcpClient peer, LogEventHandler logHandler, ExecuteEventHandler executeHandler)
        {
            Connected = false;
            Peer = peer;
            LogEvent += logHandler;
            ExecuteEvent += executeHandler;
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
            NetStream.Close();
            Peer.Close();
            Connected = false;
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

                    _TranslateMessage(msg);
                }
                catch (Exception e)
                {
                    // Only log if the failure was unexpected; ie, during an active connection.
                    if (Connected)
                    {
                        LogEvent?.Invoke(this, new LogEventArgs(1, "Exception caught while receiving data from peer.", DateTime.Now, e));
                    }
                    Connected = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Processes a Message, handling all of the control commands defined in
        /// the Systek.Net.MessageType enum.
        /// </summary>
        /// <param name="msg">The message to be translated.</param>
        private void _TranslateMessage(Message msg)
        {
            try
            {
                switch(msg.Type)
                {
                    // The Agent or Server projects will handle the events generated by COMMANDs
                    case MessageType.COMMAND:
                        ExecuteEvent?.Invoke(this, new ExecuteEventArgs(msg.CmdSet, msg.Sequence));
                        break;

                    // Elegantly close the connection
                    case MessageType.CLOSE:
                        Close();
                        break;

                    // TODO:  Handle each FAIL case
                    case MessageType.FAIL:
                        break;

                    // The Agent or Server projects will handle the events generated by LOGs
                    case MessageType.LOG:
                        LogEvent?.Invoke(this, new LogEventArgs(msg.LogType, msg.Msg, DateTime.Now));
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                LogEvent?.Invoke(this, new LogEventArgs(1, "Failed to process a message.", DateTime.Now, e));
            }
        }

        /// <summary>
        /// Serializes an object into a byte array
        /// </summary>
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
