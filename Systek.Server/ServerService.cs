﻿using Systek.Utility;
using System;
using System.Configuration;
using System.ServiceProcess;

namespace Systek.Server
{
    /// <summary>
    /// The object that represents the Windows service to be run for this Server.
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    public partial class ServerService : ServiceBase
    {
        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        // Used to describe the server ID for logging
        private const int SYSTEK_SERVER = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerService"/> class.
        /// </summary>
        public ServerService()
        {
            Log = new Logger("ServerLogContext", ConfigurationManager.AppSettings["LocalLogPath"], "ServerService");
            InitializeComponent();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            Initialize();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            Connector.Instance?.Stop();
        }

        /// <summary>
        /// Initializes this service.
        /// </summary>
        public void Initialize()
        {
            try
            {
                int port = Int32.Parse(ConfigurationManager.AppSettings["Port"]);

                Connector.Port = port;
                Connector.Instance.Initialize();
            }
            catch (Exception e)
            {
                string message = "Exception thrown while trying to initialize server:\n" + e.Message + "\n\n" + e.StackTrace;
                Log.TblSystemLog(Type.ERROR, AreaType.SERVER_INITIALIZATION, SYSTEK_SERVER, message);
            }
        }
    }
}
