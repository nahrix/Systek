using System.ServiceProcess;

namespace Systek.Server
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
                new ServerService()
            };
            ServiceBase.Run(ServicesToRun);
#else
            new ServerService().Initialize();
#endif
        }
    }
}
