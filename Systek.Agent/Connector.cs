using Systek.Net;
using Systek.Utility;
using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Systek.Agent
{
    /// <summary>
    /// Maintains the agent service's connection to the server.
    /// </summary>
    public class Connector
    {
        private IPEndPoint RemoteEndPoint { get; set; }     // End point of the remote server this agent is connecting to
        private TcpClient Peer { get; set; }                // The TCP socket between the agent and server
        private IConnection AgentConnection { get; set; }   // An abstraction of the TCPClient, including Message handling
        private bool Running { get; set; }                  // Represents whether this connection should be running or not
        private string LogPath { get; set; }                // Path to the log files

        private const int CONNECTION_CHECK_WAIT = 5000;     // The default amount of time, in ms, to wait between checking connectivity

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="remote">Endpoint of the remote host the agent will connect to.</param>
        public Connector(IPEndPoint remote)
        {
            RemoteEndPoint = remote;
        }

        /// <summary>
        /// Flags the connection thread to end, and cleans up resources.
        /// </summary>
        public void Stop()
        {
            AgentConnection.Close();
            Running = false;
        }

        /// <summary>
        /// Initializes the connection members, and starts a new thread to monitor the connection.
        /// </summary>
        public void Initialize()
        {
            try
            {
                LogPath = ConfigurationManager.AppSettings["logPath"];
                Peer = new TcpClient();
                AgentConnection = new Connection(Peer, _LogHandler, _MessageHandler);
                Running = true;

                Thread connector = new Thread(new ThreadStart(_Connector));
            }
            catch (Exception)
            {
                Running = false;
                throw;
            }
        }

        // Handles log events
        private void _LogHandler(LogEventArgs e)
        {
            string message = e.Message;
            
            if (e.ExceptionDetail != null)
            {
                message += "\n" + e.ExceptionDetail.Message + "\n" + e.ExceptionDetail.StackTrace;
            }
            Logger.Instance.FileLog(e.Type, LogPath, message);
        }

        // Handles execution events
        private void _MessageHandler(Message msg)
        {
            
        }

        /// <summary>
        /// To be run as its own thread.  Monitors the connection, and rebuilds it if it goes down.
        /// </summary>
        private void _Connector()
        {
            // This thread should run until the class' Stop function is called.
            while (true)
            {
                // Calling the Stop function flags Running to false, terminating this thread
                if (!Running)
                {
                    return;
                }

                // Rebuild the connection if it's down
                if (!AgentConnection.Connected)
                {
                    Peer.Connect(RemoteEndPoint);
                    AgentConnection = new Connection(Peer, _LogHandler, _MessageHandler);
                    AgentConnection.Initialize();
                }

                // Wait before the next check, to minimize resource footprint
                Thread.Sleep(CONNECTION_CHECK_WAIT);
            }
        }
    }
}
