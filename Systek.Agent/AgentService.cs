using Systek.Utility;
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
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentService"/> class.
        /// </summary>
        public AgentService()
        {
            Log = new Logger("AgentLogContext", ConfigurationManager.AppSettings["LocalLogPath"], "AgentService");
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
            try
            {
                int port = Int32.Parse(ConfigurationManager.AppSettings["Port"]);
                IPAddress ip = IPAddress.Parse(ConfigurationManager.AppSettings["ServerIP"]);
                string logPath = ConfigurationManager.AppSettings["LogPath"];

                IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);

                AgentCore.Initialize(remoteEndPoint);

                if (!AgentCore.Running)
                {
                    Log.FileLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, "Unable to initialize agent");
                }
            }
            catch (Exception e)
            {
                Log.FileLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, "Exception thrown while trying to initialize:\n" + e.Message + "\n\n" + e.StackTrace);
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
