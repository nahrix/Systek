﻿using System.ServiceProcess;

namespace Systek.Agent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if (!DEBUG)
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new AgentService()
            };
            ServiceBase.Run(ServicesToRun);
#else
            new AgentService().Initialize();
#endif
        }
    }
}
