using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// Defines what type of command is to be executed
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// A windows console command; ie, the kind executed from the command prompt.
        /// </summary>
        CONSOLE = 1,

        /// <summary>
        /// A Powershell command
        /// </summary>
        POWERSHELL = 2,

        /// <summary>
        /// A SQL command
        /// </summary>
        SQL = 3
    }

    /// <summary>
    /// Defines the current status of the command
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>
        /// The command executed successfully
        /// </summary>
        SUCCCESS = 1,

        /// <summary>
        /// The command failed during execution
        /// </summary>
        FAIL = 2,

        /// <summary>
        /// The command has not been executed yet
        /// </summary>
        NOT_EXECUTED = 3
    }

    /// <summary>
    /// An instruction sent from one machine to another.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the type of the command.
        /// </summary>
        CommandType CmdType { get; }

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
        Dictionary<string, string> Parameters { get; }

        /// <summary>
        /// Gets or sets the status of the command.
        /// </summary>
        CommandStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the output returned after running the command.
        /// </summary>
        List<string> Output { get; set; }
    }
}
