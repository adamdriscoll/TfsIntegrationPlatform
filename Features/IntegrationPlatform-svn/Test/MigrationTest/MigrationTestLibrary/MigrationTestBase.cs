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
                return TestContext.TestName + ".xml";
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

        [TestInitialize]
        public virtual void Initialize()
        {
            Configuration = null;
            TestStartTime = DateTime.Now;
            TestEnvironment = MigrationTestEnvironmentFactory.CreateMigrationTestEnvironment(TestContext.TestName);
            TestEnvironment.Initialize(TestProjectName);

            Microsoft.TeamFoundation.Migration.Toolkit.Constants.PluginsFolderName = ".";
            m_adapterManager = new AdapterManager(new DirectoryInfo(Microsoft.TeamFoundation.Migration.Toolkit.Constants.PluginsFolderName));

            TestEnvironment.SourceTCAdapterEnvironment.TestName = TestContext.TestName;
            TestEnvironment.TargetTCAdapterEnvironment.TestName = TestContext.TestName;

            TestEnvironment.SourceTCAdapterEnvironment.TestStartTime = TestStartTime;
            TestEnvironment.TargetTCAdapterEnvironment.TestStartTime = TestStartTime;
        }

        [TestCleanup]
        public virtual void Cleanup()
        {
        }

        protected void StartMigration()
        {
            Trace.TraceInformation("==================== MigrationApp BEGIN ====================");
            Trace.TraceInformation("Start Time: " + DateTime.Now.ToString());
            MigrationApp.Start(ConfigurationFileName);
            Trace.TraceInformation("End Time: " + DateTime.Now.ToString());
            Trace.TraceInformation("==================== MigrationApp END   ====================");
        }
    }
}