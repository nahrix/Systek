using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Server
{
    interface IManagedServer : IMachine
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="IMachine"/> is currently processing commands.
        /// It is used to avoid overlapping asynchronous execution.
        /// </summary>
        /// <value>
        ///   <c>true</c> if processing; otherwise, <c>false</c>.
        /// </value>
        bool Processing { get; }

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <param name="cmdSetId">The command set identifier.</param>
        /// <param name="runSet">The run set.</param>
        void RunCommand(int cmdSetId, int[] runSet = null);

        /// <summary>
        /// Updates 
        /// </summary>
        void Update();
    }
}
