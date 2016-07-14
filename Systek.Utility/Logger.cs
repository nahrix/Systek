using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string LogPath { get; private set; }

        /// <summary>
        /// Gets the name of the log.
        /// </summary>
        public string LogName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="logPath">The default local log path.</param>
        /// <param name="logName">Name of the log.</param>
        public Logger(string connectionString, string logPath, string logName)
        {
            ConnectionString = connectionString;
            LogPath = logPath;
            LogName = logName;
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
                        tStamp = DateTime.UtcNow,
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
                FileLog(type, area, msg);
            }
        }

        /// <summary>
        /// Writes a log to the file system.
        /// </summary>
        /// <param name="type">The type of log being written.  These types are defined in tblType in the database.</param>
        /// <param name="area">The area being affected, as defined in tblAreaType.</param>
        /// <param name="message">The content of the log to write to the file.</param>
        public void FileLog(int type, int area, string message)
        {
            try
            {
                string logType;
                string areaType;
                string timeStamp = DateTime.UtcNow.ToString("HH:mm:ss");
                string filePath = LogPath + "\\" + LogName + "_" + DateTime.UtcNow.ToString("yyyyMMdd_hh") + ".txt";

                try
                {
                    // Convert the type of message into something human-readable
                    using (LoggingContext db = new LoggingContext(ConnectionString))
                    {
                        logType = db.tblType.Find(type).name.ToUpper();
                        areaType = db.tblAreaType.Find(area).name.ToUpper();
                    }
                }
                // If table lookup fails, proceed with unknown values
                catch (Exception)
                {
                    logType = null;
                    areaType = null;
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
                    file.WriteLine("[" + timeStamp + "] Logger: AreaType not found.  AreaTypeID specified: " + area);
                    areaType = "unknown";
                }

                file.WriteLine("[" + timeStamp + "] (" + logType + ") <" + areaType + ">: " + message);
                file.Close();
            }
            catch (Exception e)
            {
                // If all else fails, attempt to write to the local Windows Event Log
                System.Diagnostics.EventLog appLog = new System.Diagnostics.EventLog();
                appLog.Source = "Systek Server";
                
                appLog.WriteEntry("Logger failed to write a log with exception:\n" + e.Message + "\n\n" + e.StackTrace + "\nOriginal message: " + message,
                    EventLogEntryType.Error);
            }
        }
    }
}
