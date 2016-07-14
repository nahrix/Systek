using Systek.Net;
using Systek.Utility;
using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Systek.Server
{
    /// <summary>
    /// Listens for connection requests from Agents, and builds IConnections from the TcpClients.
    /// </summary>
    public class Connector
    {
        /// <summary>
        /// Port number that the server will listen on.  Needs to be set before an instance of
        /// this class can be created.
        /// </summary>
        public static int Port { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Connector"/> is running.
        /// </summary>
        public bool Running { get; private set; }

        // Used to describe the server ID when logging
        private const int SYSTEK_SERVER = 2;

        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        /// <summary>
        /// The singleton instance
        /// </summary>
        private static Connector _Instance = null;

        private TcpListener Listener;

        /// <summary>
        /// Constructor.
        /// </summary>
        private Connector()
        {
            Log = new Logger("ServerLogContext", ConfigurationManager.AppSettings["LocalLogPath"], "ServerConnector");
            Listener = new TcpListener(IPAddress.Any, Port);
        }

        /// <summary>
        /// Gets the singleton instance of this object.
        /// </summary>
        /// <returns>The singleton instance of this object </returns>
        public static Connector Instance
        {
            get
            {
                if (Port == 0)
                {
                    return null;
                }

                if (_Instance == null)
                {
                    _Instance = new Connector();
                }

                return _Instance;
            }
        }

        /// <summary>
        /// Starts up the listening thread.
        /// </summary>
        public void Initialize()
        {
            new Thread(new ThreadStart(_Listen)).Start();
        }

        /// <summary>
        /// Stops this instance from listening for more connections.
        /// </summary>
        public void Stop()
        {
            Running = false;
            Listener.Stop();
        }

        /// <summary>
        /// To be run in its own thread.  Listens for agent connection requests, and builds
        /// Machine objects from the TcpClients.
        /// </summary>
        private void _Listen()
        {
            try
            {
                Listener.Start();
            }
            catch(Exception e)
            {
                string message = "Systek server threw exception while trying to start the TcpListener.\n" + e.Message + "\n\n" + e.StackTrace;
                Log.TblSystemLog(Type.ERROR, AreaType.SERVER_TCP_LISTENER, SYSTEK_SERVER, message);
                return;
            }

            Running = true;
            
            while (Running)
            {
                try
                {
                    TcpClient agentSocket = Listener.AcceptTcpClient();
                    IMachine agentMachine = new Machine(agentSocket);
                    agentMachine.Initialize();

                    if (!agentMachine.NetConnection.Connected)
                    {
                        Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, SYSTEK_SERVER, "New agent connection failed to initialize.");
                    }
                }
                catch (Exception e)
                {
                    if (Running)
                    {
                        string message = "Systek server threw exception while creating new agent connection.\n" + e.Message + "\n\n" + e.StackTrace;
                        Log.TblSystemLog(Type.ERROR, AreaType.SERVER_TCP_LISTENER, SYSTEK_SERVER, message);
                    }
                }
            }
        }
    }
}
