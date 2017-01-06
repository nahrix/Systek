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
    /// <param name="e">The <see cref="LogEventArgs" /> instance containing the event data.</param>
    public delegate void LogEventHandler(LogEventArgs e);

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
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the area in the code that is being affected, as defined in tblAreaType
        /// </summary>
        public AreaType AreaType { get; private set; }

        /// <summary>
        /// The exception to be logged, if any.
        /// </summary>
        public Exception ExceptionDetail { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventArgs" /> class.
        /// </summary>
        /// <param name="type">The type of log, defined in tblType.</param>
        /// <param name="area">The area.</param>
        /// <param name="msg">The message to be logged.</param>
        /// <param name="e">The exception data related to this log, if any.</param>
        public LogEventArgs(Type type, AreaType area, string msg, Exception e = null)
        {
            Type = type;
            AreaType = area;
            Message = msg;
            ExceptionDetail = e;
        }
    }

    /// <summary>
    /// Required for message processing.
    /// </summary>
    /// <param name="msg">The message to be processed.</param>
    public delegate void MessageEventHandler(Message msg);
}
