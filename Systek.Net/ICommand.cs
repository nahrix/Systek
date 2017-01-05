using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// An instruction sent from one machine to another.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The position in the queue of commands to be run within the CommandSet.
        /// </summary>
        int Sequence { get; }

        /// <summary>
        /// The command to be executed.
        /// </summary>
        string Cmd { get; }

        /// <summary>
        /// The parameters for the command to be executed, if any.
        /// </summary>
        List<string> Parameters { get; }
    }
}
