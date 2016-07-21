﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Represents whether the connection is active or not
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Gets or sets a value indicating whether verbose logs should be written.
        /// </summary>
        /// <value>
        ///   <c>true</c> if verbose logs should be written; otherwise, <c>false</c>.
        /// </value>
        bool VerboseLogging { get; set; }

        /// <summary>
        /// The time, in milliseconds, for how long to wait for an expected message before timing out
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Starts the message listener.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Closes the connection.
        /// </summary>
        void Close();

        /// <summary>
        /// Sends a Message through the stream to the connected peer.
        /// </summary>
        /// <param name="msg">The Message to send.</param>
        void Send(Message msg);
    }
}
