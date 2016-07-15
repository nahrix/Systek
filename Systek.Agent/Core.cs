using Systek.Net;
using Systek.Utility;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Agent
{
    /// <summary>
    /// The core agent service.  Maintains a TCP connection, and handles messages from the server.
    /// </summary>
    public class Core
    {
        /// <summary>
        /// Gets the IConnection that handles low-level communications to the server.
        /// </summary>
        public IConnection AgentConnection { get; private set; }

        /// <summary>
        /// Gets the TCP socket to the server.
        /// </summary>
        public TcpClient Server { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Core"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running { get; private set; }

        /// <summary>
        /// Gets the path to the log files.
        /// </summary>
        public string LogPath { get; private set; }

        /// <summary>
        /// Gets the period of time between reconnect checks.
        /// </summary>
        public int ReconnectWait { get; private set; }

        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        /// <summary>
        /// The
        /// </summary>
        private bool VerboseLogging = false;

        /// <summary>
        /// Gets or sets the singleton instance.
        /// </summary>
        private static Core _Instance { get; set; }

        // Used to describe the server ID when logging
        private const int LOCALHOST = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Core"/> class.  Privatized because this class is a singleton.
        /// </summary>
        private Core()
        {
            Boolean.TryParse(ConfigurationManager.AppSettings["VerboseLogging"], out VerboseLogging);
            Log = new Logger("AgentLogContext", ConfigurationManager.AppSettings["LocalLogPath"], "AgentCore");
            ReconnectWait = Int32.Parse(ConfigurationManager.AppSettings["ReconnectWait"]);
            Running = false;
        }

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <returns>This instance.</returns>
        public static Core Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Core();
                }

                return _Instance;
            }
        }

        /// <summary>
        /// Initializes the connection to the specified remote end point.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point.</param>
        public void Initialize(IPEndPoint remoteEndPoint)
        {
            try
            {
                Running = true;
                Thread connector = new Thread(() => _Connector(remoteEndPoint));
                connector.Start();
            }
            catch (Exception e)
            {
                Server?.Close();
                Running = false;

                string message = "There was an exception thrown when trying to initialize the Agent:\n" + e.Message
                    + "\n\n" + e.StackTrace;
                Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, message);
            }
        }

        /// <summary>
        /// Stops the connection.
        /// </summary>
        public void Shutdown()
        {
            Running = false;
            Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent shutdown requested.");
            Server?.Close();
        }

        /// <summary>
        /// To be run as its own thread.  Monitors the connection, and rebuilds it if it goes down.
        /// </summary>
        private void _Connector(IPEndPoint remoteEndPoint)
        {
            // This thread should run until the class' Stop function is called.
            do
            {
                try
                {
                    // Rebuild the connection if it's down
                    if (!AgentConnection?.Connected ?? true)
                    {
                        Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service is attempting to connect to server, at:"
                            + "IP " + remoteEndPoint.Address.ToString() + ", Port " + remoteEndPoint.Port.ToString());
                        Server = new TcpClient();
                        Server.Connect(remoteEndPoint);
                        AgentConnection = new Connection(Server, _LogHandler, _MessageHandler);
                        AgentConnection.VerboseLogging = VerboseLogging;
                        AgentConnection.Initialize();
                        Running = true;
                        Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service connected to server successfully.");
                    }

                    // Wait before the next check, to minimize CPU usage
                    Thread.Sleep(ReconnectWait);
                }
                catch (Exception e)
                {
                    string message = "There was an exception thrown when trying to connect the Agent to the Server:\n" + e.Message
                        + "\n\n" + e.StackTrace;
                    Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, message);
                    //Thread.Sleep(ReconnectWait * 100);  // Longer timeout to retry if the server appears to be down, to avoid log spam
                }
            } while (Running);
        }

        /// <summary>
        /// Handles logging events passed from the Net library.
        /// </summary>
        /// <param name="e">The <see cref="LogEventArgs"/> instance containing the event data.</param>
        private void _LogHandler(LogEventArgs e)
        {
            string message = "Agent log handler: " + e.Message;

            if (e.ExceptionDetail != null)
            {
                message += "\n" + e.ExceptionDetail.Message + "\n\n" + e.ExceptionDetail.StackTrace;
            }
            Log.TblSystemLog(e.Type, e.AreaType, LOCALHOST, message);
        }

        /// <summary>
        /// Handles messages sent from the server.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        private void _MessageHandler(Message msg)
        {

        }
    }
}
