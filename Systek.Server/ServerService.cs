using System;
using System.Configuration;
using System.ServiceProcess;

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
