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
        protected NetworkStream NetStream { get; set; }
        protected List<Message> Messages { get; set; }
        protected Mutex MessageMutex { get; set; }

        public Connection(IPEndPoint binding)
        {
            Binding = binding;
            MessageMutex = new Mutex();
        }

        public void Initialize()
        {
            Connect();
            NetStream = new NetworkStream(Peer.Client);
            new Thread(new ThreadStart(_Receive)).Start();
        }

        public void Send(Message msg)
        {
            
        }

        public List<Message> GetMessages()
        {
            MessageMutex.WaitOne();

            List<Message> messages = new List<Message>();
            foreach (Message msg in Messages)
            {
                messages.Add(msg);
            }
            Messages.Clear();

            MessageMutex.ReleaseMutex();

            return messages;
        }

        protected void _Receive()
        {
            while(true)
            {
                NetStream.
                Thread.Sleep(100);
            }
        }

        abstract protected void Connect();
    }
}
