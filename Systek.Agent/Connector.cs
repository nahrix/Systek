using Systek.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Agent
{
    class Connector
    {
        private IPEndPoint RemoteEndPoint { get; set; }     // End point of the remote server this agent is connecting to
        private TcpClient Peer { get; set; }                // The TCP socket between the agent and server
        private Connection Agent { get; set; }              // An abstraction of the TCPClient, including Message handling
        private bool Running { get; set; }                  // Represents whether this connection should be running or not

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
            Agent.Close();
            Running = false;
        }

        /// <summary>
        /// Initializes the connection members, and starts a new thread to monitor the connection.
        /// </summary>
        public void Initialize()
        {
            try
            {
                Peer = new TcpClient();
                Agent = new Connection(Peer);
                Running = true;

                Thread connector = new Thread(new ThreadStart(_Connector));
            }
            catch (Exception)
            {
                Running = false;
                throw;
            }
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
                if (!Agent.Connected)
                {
                    Peer.Connect(RemoteEndPoint);
                    Agent = new Connection(Peer);
                    Agent.Initialize();
                }

                // Wait before the next check, to minimize resource footprint
                Thread.Sleep(CONNECTION_CHECK_WAIT);
            }
        }
    }
}
