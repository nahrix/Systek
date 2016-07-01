﻿using Systek.Utility;
using System;
using System.Configuration;
using System.Net;
using System.ServiceProcess;

namespace Systek.Agent
{
    /// <summary>
    /// The base Windows Service used to keep this agent running.  The actual implementation of agent features
    /// contained in the <see cref="Core"/> class, while this class just manages the service itself.
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    public partial class AgentService : ServiceBase
    {
        /// <summary>
        /// Gets the agent core class, which manages the more important functions of the agent.
        /// </summary>
        public Core AgentCore { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentService"/> class.
        /// </summary>
        public AgentService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service
        /// by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically).
        /// Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            Initialize();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service
        /// by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
        }

        /// <summary>
        /// Starts up the connection to the remote server.
        /// </summary>
        public void Initialize()
        {
            int port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
            IPAddress ip = IPAddress.Parse(ConfigurationManager.AppSettings["serverIP"]);
            string logPath = ConfigurationManager.AppSettings["logPath"];

            IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);

            AgentCore.Initialize(remoteEndPoint);

            if (!AgentCore.Running)
            {
                Logger.Instance.FileLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, logPath, "Unable to initialize agent");
            }
        }

        /// <summary>
        /// Shuts down the service, and does any necessary cleanup.
        /// </summary>
        public void Shutdown()
        {
            AgentCore.Shutdown();
        }
    }
}
