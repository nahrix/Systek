using Systek.Net;
using Systek.Utility;
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">Port number that the server will listen on.</param>
        public Connector(int port)
        {
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
            
            while (true)
            {
                TcpClient agent = listener.AcceptTcpClient();
                new Machine(agent);
            }
        }
    }
}
