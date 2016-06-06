using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Utility
{
    /// <summary>
    /// General interface for writing logs to a file, the Windows event log, or the database.
    /// </summary>
    public class Logger
    {
        private static Logger Singleton;        // The logger only needs a single instance

        /// <summary>
        /// Constructor.  Empty for now.
        /// </summary>
        private Logger()
        {

        }

        /// <summary>
        /// Used to get the singleton instance of this class.
        /// </summary>
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

        /// <summary>
        /// Writes a log to TblSystemLog in the database.
        /// </summary>
        /// <param name="type">The ID of the type of log (error/info/etc)</param>
        /// <param name="area">The ID of the area in code being logged.</param>
        /// <param name="server">The ID of the server generating the log.</param>
        /// <param name="msg">The log's content (error message, stack trace, etc)</param>
        public void TblSystemLog(int type, int area, int server, string msg)
        {
            try
            {
                using (LoggingContext db = new LoggingContext())
                {
                    tblSystemLog log = new tblSystemLog
                    {
                        typeID = type,
                        areaID = area,
                        serverID = server,
                        message = msg
                    };

                    db.tblSystemLog.Add(log);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                // If the logger throws an error.. Oh god... Cry silently?
            }
        }

        /// <summary>
        /// Writes a log to the file system.
        /// </summary>
        /// <param name="type">The type of log being written.  These types are defined in tblType in the database.</param>
        /// <param name="filePath">The full path of the log file.</param>
        /// <param name="message">The content of the log to write to the file.</param>
        public void FileLog(int type, string filePath, string message)
        {
            string logType;
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");

            try
            {
                // Convert the type of message into something human-readable
                using (LoggingContext db = new LoggingContext())
                {
                    logType = db.tblType.Find(type).name.ToUpper();
                }

                StreamWriter file = new StreamWriter(filePath, true);

                // If an invalid log type is specified, log that too
                if (logType == null)
                {
                    file.WriteLine("[" + timeStamp + "] Logger: Type not found.  TypeID specified: " + type);
                    file.WriteLine("[" + timeStamp + "] (Unknown): " + message);
                    file.Close();
                    return;
                }

                
                file.WriteLine("[" + timeStamp + "] " + logType + ": " + message);
                file.Close();
            }
            catch (Exception)
            {
                // The logger failed, so this comment will serve as the epitaph for the poor, unlogged exception.
            }
        }
    }
}
