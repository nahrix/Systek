using Systek.Utility;
using System;
using System.Configuration;
using System.Net;
using System.ServiceProcess;

namespace Systek.Agent
{
    /// <summary>
    /// The base Windows Service used to keep this agent running.  The actual implementation of agent features
    /// are contained in the <see cref="Core"/> class, while this class just manages the service itself.
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    public partial class AgentService : ServiceBase
    {
        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        /// <summary>
        /// The ID of the localhost, as defined in tblServer
        /// </summary>
        private const int LOCALHOST = 1;

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
            Core.Instance?.Shutdown();
            Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service stopped successfully.");
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
                string logPath = ConfigurationManager.AppSettings["LocalLogPath"];

                IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);

                // Initialize the Agent Core, and shutdown the service if the initialization fails
                if (!Core.Instance.Initialize(remoteEndPoint))
                {
                    Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Unable to initialize agent");
                    Stop();
                    return;
                }

                Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service started successfully.");
            }
            catch (Exception e)
            {
                Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Exception thrown while trying to initialize:\n" + e.Message + "\n\n" + e.StackTrace);
                Stop();
                return;
            }
        }
    }
}
