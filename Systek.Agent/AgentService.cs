using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;

namespace Systek.Agent
{
    public partial class AgentService : ServiceBase
    {
        public AgentService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Initialize();
        }

        protected override void OnStop()
        {
        }

        /// <summary>
        /// Starts up the connection to the remote server.
        /// </summary>
        public void Initialize()
        {
            int port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
            IPAddress ip = IPAddress.Parse(ConfigurationManager.AppSettings["serverIP"]);

            IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);

            new Connector(remoteEndPoint).Initialize();
        }
    }
}
