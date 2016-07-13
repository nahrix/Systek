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
        /// <summary>
        /// Gets the name of the connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// If the logger fails to write to the database, it will need to fall back on a simpler
        /// file-based log to log its own error.
        /// </summary>
        public string DefaultLocalLogPath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public Logger(string connectionString, string defaultLocalLogPath)
        {
            ConnectionString = connectionString;
            DefaultLocalLogPath = defaultLocalLogPath;
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
                using (LoggingContext db = new LoggingContext(ConnectionString))
                {
                    tblSystemLog log = new tblSystemLog
                    {
                        tStamp = DateTime.Now,
                        typeID = type,
                        areaID = area,
                        serverID = server,
                        message = msg
                    };

                    db.tblSystemLog.Add(log);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                msg = "Logger threw an exception while trying to write a log.\n" + e.Message + "\n\n" + e.StackTrace
                    + "\nOriginal error: " + msg;
                FileLog(type, area, DefaultLocalLogPath, msg);
            }
        }

        /// <summary>
        /// Writes a log to the file system.
        /// </summary>
        /// <param name="type">The type of log being written.  These types are defined in tblType in the database.</param>
        /// <param name="area">The area being affected, as defined in tblAreaType.</param>
        /// <param name="filePath">The full path of the log file.</param>
        /// <param name="message">The content of the log to write to the file.</param>
        public void FileLog(int type, int area, string filePath, string message)
        {
            try
            {
                string logType;
                string areaType;
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");

                // Convert the type of message into something human-readable
                using (LoggingContext db = new LoggingContext(ConnectionString))
                {
                    logType = db.tblType.Find(type).name.ToUpper();
                    areaType = db.tblAreaType.Find(area).name.ToUpper();
                }

                StreamWriter file = new StreamWriter(filePath, true);

                // If an invalid log type is specified, log that too
                if (logType == null)
                {
                    file.WriteLine("[" + timeStamp + "] Logger: Type not found.  TypeID specified: " + type);
                    logType = "unknown";
                }

                // If an invalid area type is specified, log that too
                if (areaType == null)
                {
                    file.WriteLine("[" + timeStamp + "] Logger: AreaType not found.  AreaTypeID specified: " + areaType);
                    areaType = "unknown";
                }

                file.WriteLine("[" + timeStamp + "] (" + logType + ") <" + areaType + ">: " + message);
                file.Close();
            }
            catch (Exception)
            {
                // The logger failed, so this comment will serve as the epitaph for the poor, unlogged exception.
            }
        }
    }
}
