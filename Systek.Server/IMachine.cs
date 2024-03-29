﻿using Systek.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Server
{
    /// <summary>
    /// A computer that is connected to the Systek server.
    /// </summary>
    public interface IMachine : IDisposable
    {
        /// <summary>
        /// Gets the name of the machine.
        /// </summary>
        string MachineName { get; }

        /// <summary>
        /// Gets the machine identifier.
        /// </summary>
        int MachineID { get; }

        /// <summary>
        /// Gets the services that are running on this machine.
        /// </summary>
        Dictionary<string, int> Services { get; }

        /// <summary>
        /// Gets the Connection used to communicate with the agent
        /// </summary>
        IConnection NetConnection { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IMachine"/> is authenticated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if authenticated; otherwise, <c>false</c>.
        /// </value>
        bool Authenticated { get; }

        /// <summary>
        /// Authenticates this instance.
        /// </summary>
        /// <returns></returns>
        bool Authenticate();

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Updates this instance with the latest state.
        /// </summary>
        void BasicUpdate();
    }
}
