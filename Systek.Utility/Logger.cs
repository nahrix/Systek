using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Utility
{
    public class Logger
    {
        private static Logger Singleton;

        private Logger()
        {

        }

        public static Logger Instance
        {
            get
            {
                if (Singleton == null)
                {
                    Singleton = new Logger();
                }

                return Singleton;
            }
        }

        public void Log(int typeID, int areaID, string server, string message)
        {

        }
    }
}
