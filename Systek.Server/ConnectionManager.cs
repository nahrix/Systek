using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Server
{
    class ConnectionManager
    {
        private List<IMachine> _Machines;

        /// <summary>
        /// The singleton instance
        /// </summary>
        private ConnectionManager _Instance;

        /// <summary>
        /// Prevents a default instance of the <see cref="ConnectionManager"/> class from being created.
        /// </summary>
        private ConnectionManager()
        {
            _Machines = new List<IMachine>();
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public ConnectionManager GetInstance()
        {
            if (_Instance == null)
            {
                _Instance = new ConnectionManager();
            }

            return _Instance;
        }

        /// <summary>
        /// Adds the specified machine to the list.
        /// </summary>
        /// <param name="machine">The machine to add.</param>
        /// <returns></returns>
        public bool Add(IMachine machine)
        {
            if (machine == null)
            {
                return false;
            }

            _Machines.Add(machine);
            return true;
        }

        /// <summary>
        /// Removes the machine from the list, based on its hostname and IP address.
        /// </summary>
        /// <param name="hostname">The hostname of the machine to remove.</param>
        /// <param name="ip">The ip endpoint of the machine to remove.</param>
        /// <returns></returns>
        public bool Remove(string hostname, IPEndPoint ip)
        {
            return true;
        }
    }
}
