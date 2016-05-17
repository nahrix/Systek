using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Server
{
    public partial class ServerService : ServiceBase
    {
        public ServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }

        public void Initialize()
        {
            int port = Int32.Parse(ConfigurationManager.AppSettings["port"]);

            new Connector(port).Initialize();
        }
    }
}
