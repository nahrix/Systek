using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Net
{
    abstract class Connection : IConnection
    {
        public bool Active { get; protected set; }

        protected TcpClient Peer { get; set; }
        protected IPEndPoint Binding { get; set; }
        protected NetworkStream Stream { get; set; }

        public Connection(IPEndPoint binding)
        {
            Binding = binding;
        }

        public void Initialize()
        {
            Connect();
            new Thread(new ThreadStart(_Receive)).Start();
        }

        abstract protected void Connect();

        public void Send(IMessage msg)
        {
            
        }

        private void _Receive()
        {
            while(true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
