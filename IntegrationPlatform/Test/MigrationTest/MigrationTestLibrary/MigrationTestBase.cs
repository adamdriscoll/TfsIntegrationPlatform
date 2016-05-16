// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public abstract class MigrationTestBase
    {
        protected abstract string TestProjectName { get; }

        private TestContext testContextInstance;

        protected MigrationTestEnvironment TestEnvironment { get; set; }

        protected Configuration Configuration { get; set; }

        protected string ConfigurationFileName
        {
            get
            {
                return TestName + ".xml";
            }
        }

        protected DateTime TestStartTime { get; set; }

        private AdapterManager m_adapterManager;
        public AdapterManager TestAdapterManager
        {
            get
            {
                return m_adapterManager;
            }
        }

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

        public string TestName { get; set; }

        [TestInitialize]
        public virtual void Initialize()
        {
            Configuration = null;
            TestStartTime = DateTime.Now;

            if (String.IsNullOrEmpty(TestName))
            {
                // this is a MSTest test
                TestName = TestContext.TestName;
                Microsoft.TeamFoundation.Migration.Toolkit.Constants.PluginsFolderName = ".";
            }

            TestEnvironment = MigrationTestEnvironment.Load(TestName);
            TestEnvironment.Initialize(TestProjectName);

            DirectoryInfo[] directories = new DirectoryInfo[]
            {
               new DirectoryInfo(Microsoft.TeamFoundation.Migration.Toolkit.Constants.PluginsFolderName),
               new DirectoryInfo("."),
            };

            m_adapterManager = new AdapterManager(directories);

            TestEnvironment.LeftEndPoint.TestName = TestName;
            TestEnvironment.RightEndPoint.TestName = TestName;
        }

        [TestCleanup]
        public virtual void Cleanup()
        {
        }

        protected void StartMigration()
        {
            Trace.TraceInformation("==================== MigrationApp BEGIN ====================");
            Trace.TraceInformation("Start Time: " + DateTime.Now.ToString());
            Trace.TraceInformation("Configuration File: {0}", ConfigurationFileName);
            MigrationApp.Start(ConfigurationFileName);
            Trace.TraceInformation("End Time: " + DateTime.Now.ToString());
            Trace.TraceInformation("==================== MigrationApp END   ====================");
        }
    }
}