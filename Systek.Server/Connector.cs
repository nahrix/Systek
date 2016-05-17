using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Server
{
    public class Connector
    {
        public int Port { get; private set; }

        public Connector(int port)
        {
            Port = port;
        }

        public void Initialize()
        {
            new Thread(new ThreadStart(_Listen)).Start();
        }

        public void _Listen()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);

            while (true)
            {
                listener.AcceptTcpClient();
            }
        }
    }
}
