using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Systek.Utility;
using System.Linq;
using Systek;

namespace Systek.UnitTests
{
    /// <summary>
    /// Summary description for UtilityTests
    /// </summary>
    [TestClass]
    public class UtilityTests
    {
        // The path to where the test logs will be saved
        private const string LogPath = "C:\\dev\\logs\\systek";

        // The name of the connection string that will be read from the app.config file
        private const string ConnectionString = "LoggingContext";

        // Describes the server ID that will be used for logging
        private const int LOCALHOST = 1;

        public UtilityTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        /// Tests the logging functions.  Ensures a single unique entry is logged for each
        /// logging mechanism (database, file, windows event log, etc)
        /// </summary>
        [TestMethod]
        public void LoggerTest()
        {
            try
            {
                Logger Log = new Logger(ConnectionString, LogPath, "UnitTest");
                string guid = Guid.NewGuid().ToString();
                string message = "Testing the logger from the UtilityTest unit test class. GUID: " + guid;

                Log.TblSystemLog(Type.ERROR, AreaType.UNIT_TEST, LOCALHOST, message);
                Log.FileLog(Type.ERROR, AreaType.UNIT_TEST, message);

                using (LoggingContext db = new LoggingContext())
                {
                    string[] log = (from l in db.tblSystemLog
                                    where l.message.Equals(message)
                                    select l.message).ToArray();
                    Assert.IsTrue(log.Length == 1);
                    Assert.AreEqual(log[0], message);
                }

            }
            catch (Exception e)
            {
                throw new AssertFailedException("Exception thrown while trying to execute Logger test.", e);
            }
        }
    }
}
