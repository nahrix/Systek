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
        public IConnection NetConnection { get; private set; }

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
        private int _ReconnectWait;

        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger _Log;

        /// <summary>
        /// Indicates whether logs should be verbose or not
        /// </summary>
        private bool _VerboseLogging = false;

        /// <summary>
        /// Gets or sets the singleton instance.
        /// </summary>
        private static Core _Instance;

        /// <summary>
        /// Describes the localhost ID when logging, as defined in tblServer
        /// </summary>
        private const int LOCALHOST = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Core"/> class.  Privatized because this class is a singleton.
        /// </summary>
        private Core()
        {
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
        public bool Initialize(IPEndPoint remoteEndPoint)
        {
            try
            {
                bool parseResults = true;
                parseResults = parseResults && bool.TryParse(ConfigurationManager.AppSettings["VerboseLogging"], out _VerboseLogging);
                parseResults = parseResults && int.TryParse(ConfigurationManager.AppSettings["ReconnectWait"], out _ReconnectWait);
                string logPath = ConfigurationManager.AppSettings["LocalLogPath"];
                parseResults = parseResults && (logPath != null);

                // Initialization fails if any of the AppSettings values failed to load
                if (!parseResults)
                {
                    return false;
                }

                _Log = new Logger("AgentLogContext", logPath, "AgentCore");

                Running = true;
                Thread connector = new Thread(() => _Connector(remoteEndPoint));
                connector.Start();

                return true;
            }
            catch (Exception e)
            {
                Server?.Close();

                string message = "There was an exception thrown when trying to initialize the Agent:\n"
                    + e.Message + "\n\n" + e.StackTrace;
                _Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, message);

                return false;
            }
        }

        /// <summary>
        /// Stops the connection.
        /// </summary>
        public void Shutdown()
        {
            Running = false;
            _Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent shutdown requested.");
            NetConnection?.Close();
        }

        /// <summary>
        /// To be run as its own thread.  Monitors the connection, and rebuilds it if it goes down.
        /// </summary>
        private void _Connector(IPEndPoint remoteEndPoint)
        {
            // This thread should run until the class' Shutdown function is called.
            do
            {
                try
                {
                    // Rebuild the connection if it's down
                    if (!NetConnection?.Connected ?? true)
                    {
                        _Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service is attempting to connect " +
                            " to server, at: IP " + remoteEndPoint.Address.ToString() + ", Port " + remoteEndPoint.Port.ToString());
                        Server = new TcpClient();
                        Server.Connect(remoteEndPoint);
                        NetConnection = new Connection(Server, _LogHandler, _MessageHandler);
                        NetConnection.VerboseLogging = _VerboseLogging;
                        NetConnection.Initialize();
                        Running = true;
                        _Log.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service connected to server successfully.");
                    }

                    // Wait before the next check, to minimize CPU usage
                    Thread.Sleep(_ReconnectWait);
                }
                catch (Exception e)
                {
                    string message = "There was an exception thrown when trying to connect the Agent to the Server:\n" + e.Message
                        + "\n\n" + e.StackTrace;
                    _Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, message);

                    // Longer timeout to retry if the server appears to be down, to avoid log spam
                    Thread.Sleep(_ReconnectWait * 100);
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
            _Log.TblSystemLog(e.Type, e.AreaType, LOCALHOST, message);
        }

        /// <summary>
        /// Handles messages sent from the server.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        private void _MessageHandler(Message msg)
        {
            try
            {
                if (_VerboseLogging)
                {
                    _Log.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, LOCALHOST, "Agent is handling a message from server, " +
                        "of type: " + msg.Type.ToString());

                    Console.WriteLine("Message from server of type: " + Enum.GetName(msg.Type.GetType(), msg.Type));
                }

                switch (msg.Type)
                {
                    // A message with type UPDATE_BASIC is handled as a request, from the server, for this agent
                    // to provide some basic information about the client.  The data is sent back with the same
                    // UPDATE_BASIC message type.
                    case MessageType.UPDATE_BASIC:
                        NetConnection.Send(new Message()
                        {
                            Type = MessageType.UPDATE_BASIC,
                            Update = new UpdateData()
                            {
                                HostName = Environment.MachineName,
                                AuthKey = ConfigurationManager.AppSettings["AuthKey"]
                            }
                        });
                        break;

                    case MessageType.COMMAND:
                        break;

                    case MessageType.CLOSE:
                        break;

                    case MessageType.FAIL:
                        break;

                    case MessageType.LOG:
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _Log.TblSystemLog(Type.ERROR, AreaType.AGENT_MESSAGE_HANDLER, LOCALHOST, "Error while processing a message from the server.\n\n" + e.Message);
            }
        }
    }
}
