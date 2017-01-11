using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Server
{
    /// <summary>
    /// Maintains a list of active connections, so that the IMachines can be accessed by various criteria.
    /// </summary>
    public class ConnectionManager
    {
        private List<IMachine> _Machines;

        /// <summary>
        /// The singleton instance
        /// </summary>
        private static ConnectionManager _Instance;

        /// <summary>
        /// A thread for the _PruneConnections function to run in.
        /// </summary>
        private Thread _Prune;

        /// <summary>
        /// The amount of time to wait, in ms, between each pruning cycle.
        /// </summary>
        private const int PRUNE_WAIT = 5000;

        /// <summary>
        /// Prevents a default instance of the <see cref="ConnectionManager"/> class from being created.
        /// </summary>
        private ConnectionManager()
        {
            _Machines = new List<IMachine>();
            _Prune = new Thread(new ThreadStart(_PruneConnections));
            _Prune.Name = "Pruning thread";
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static ConnectionManager GetInstance()
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
            if (_Prune.ThreadState == ThreadState.Unstarted || _Prune.ThreadState == ThreadState.Stopped)
            {
                _Prune.Start();
            }

            return true;
        }

        /// <summary>
        /// Removes the machine from the list, based on its hostname and IP address.
        /// </summary>
        /// <param name="machineID">The machine identifier, as defined by ID in tblServer.</param>
        /// <returns></returns>
        public bool Remove(int machineID)
        {
            try
            {
                foreach (IMachine machine in _Machines)
                {
                    if (machine.MachineID == machineID)
                    {
                        _Machines.Remove(machine);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Log this if it becomes relevant
            }
            return false;
        }

        /// <summary>
        /// Attempts to get a machine from the list of connected machines, based on its MachineID.
        /// </summary>
        /// <param name="machineID">The machine identifier, as defined by ID in tblServer.</param>
        /// <returns>The connected machine.  If no connected machine exists in the list, then null is returned.</returns>
        public IMachine GetMachine(int machineID)
        {
            IMachine returnServer = null;

            try
            {
                foreach (IMachine machine in _Machines)
                {
                    if (machine.MachineID == machineID)
                    {
                        returnServer = machine;
                    }
                }
            }
            catch (Exception)
            {
                // TODO: add logging to this class, if it ever becomes relevant
            }
            return returnServer;
        }

        /// <summary>
        /// Checks all connections in the list, and removes them if they're disconnected.
        /// </summary>
        private void _PruneConnections()
        {
            while(_Machines.Count > 0)
            {
                for (int i = _Machines.Count - 1; i >= 0; i--)
                {
                    if (!_Machines[i].NetConnection.Connected)
                    {
                        _Machines.Remove(_Machines[i]);
                    }
                }

                Thread.Sleep(PRUNE_WAIT);
            }
        }
    }
}
