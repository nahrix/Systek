using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// Required for log events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="LogEventArgs" /> instance containing the event data.</param>
    public delegate void LogEventHandler(object sender, LogEventArgs e);

    /// <summary>
    /// Custom event arguments for logging events
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// The message that describes what to be logged.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the type of log to write, as defined in tblType.
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// The time stamp when the event occurred.
        /// </summary>
        public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// The exception to be logged, if any.
        /// </summary>
        public Exception ExceptionDetail { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventArgs" /> class.
        /// </summary>
        /// <param name="type">The type of log, defined in tblType.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="time">The current time.</param>
        /// <param name="e">The exception data related to this log, if any.</param>
        public LogEventArgs(int type, string msg, DateTime time, Exception e = null)
        {
            Type = type;
            Message = msg;
            TimeStamp = time;
            ExceptionDetail = e;
        }
    }

    /// <summary>
    /// Required for execution events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ExecuteEventArgs" /> instance containing the event data.</param>
    /// <returns>
    ///   <c>true</c> if the execution completed successfully, or <c>false</c> if not.
    /// </returns>
    public delegate bool ExecuteEventHandler(object sender, ExecuteEventArgs e);

    /// <summary>
    /// Custom event arguments for command-execution events.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ExecuteEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the CommandSet to be executed.
        /// </summary>
        public ICommandSet CmdSet { get; private set; }

        /// <summary>
        /// Gets the sequence number that represents where in the CommandSet to begin execution.
        /// </summary>
        public int Sequence { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteEventArgs"/> class.
        /// </summary>
        /// <param name="cmds">The CMDS.</param>
        /// <param name="seq">The seq.</param>
        public ExecuteEventArgs(ICommandSet cmds, int seq = 1)
        {
            CmdSet = cmds;
            Sequence = seq;
        }
    }
}
