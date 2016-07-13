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
        /// Port number that the server will listen on
        /// </summary>
        public int Port { get; private set; }

        // Used to describe the server ID when logging
        private const int SYSTEK_SERVER = 2;

        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">Port number that the server will listen on.</param>
        public Connector(int port)
        {
            Log = new Logger("ServerLogContext", ConfigurationManager.AppSettings["localLogPath"]);
            Port = port;
        }

        /// <summary>
        /// Starts up the listening thread.
        /// </summary>
        public void Initialize()
        {
            new Thread(new ThreadStart(_Listen)).Start();
        }

        /// <summary>
        /// To be run in its own thread.  Listens for agent connection requests, and builds
        /// Machine objects from the TcpClients.
        /// </summary>
        private void _Listen()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                listener.Start();
            }
            catch(Exception e)
            {
                string message = "Systek server threw exception while trying to start the TcpListener.\n" + e.Message + "\n\n" + e.StackTrace;
                Log.TblSystemLog(Type.ERROR, AreaType.SERVER_TCP_LISTENER, SYSTEK_SERVER, message);
                return;
            }
            
            while (true)
            {
                try
                {
                    TcpClient agentSocket = listener.AcceptTcpClient();
                    IMachine agentMachine = new Machine(agentSocket);
                    agentMachine.Initialize();

                    if (!agentMachine.NetConnection.Connected)
                    {
                        Log.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, SYSTEK_SERVER, "New agent connection failed to initialize.");
                    }
                }
                catch (Exception e)
                {
                    string message = "Systek server threw exception while creating new agent connection.\n" + e.Message + "\n\n" + e.StackTrace;
                    Log.TblSystemLog(Type.ERROR, AreaType.SERVER_TCP_LISTENER, SYSTEK_SERVER, message);
                }
            }
        }
    }
}
