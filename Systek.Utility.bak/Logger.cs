using System;
using System.Collections.Generic;
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
        /// <param name="typeID">The ID of the type of log (error/info/etc)</param>
        /// <param name="areaID">The ID of the area in code being logged.</param>
        /// <param name="serverID">The ID of the server generating the log.</param>
        /// <param name="message">The log's content (error message, stack trace, etc)</param>
        public void TblSystemLog(int type, int area, int server, string msg)
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

        /// <summary>
        /// Writes a log to the file system.
        /// </summary>
        /// <param name="filePath">The full path of the log file.</param>
        /// <param name="message">The content of the log to write to the file.</param>
        public void FileLog(int type, string filePath, string message)
        {
            string logType;

            switch (type)
            {
                case 1:
                    logType = "ERROR";
                    break;
                case 2:
                    logType = "INFO";
                    break;
                case 3:
                    logType = "OTHER";
                    break;
            }


        }
    }
}
