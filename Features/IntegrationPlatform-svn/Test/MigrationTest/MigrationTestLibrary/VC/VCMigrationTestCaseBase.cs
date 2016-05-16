// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public abstract class VCMigrationTestCaseBase : MigrationTestBase
    {
        private IVCTestCaseAdapter m_sourceAdapter;
        private IVCTestCaseAdapter m_targetAdapter;
        private string m_currentDirectory;


        protected abstract void InitializeTestCase();
        protected abstract void VerifyMigration(bool AddOnBranchSourceNotFound);
        protected MigrationItemStrings m_extraFile;

        protected IVCTestCaseAdapter SourceAdapter
        {
            get
            {
                return m_sourceAdapter;
            }
        }

        protected IVCTestCaseAdapter TargetAdapter
        {
            get
            {
                return m_targetAdapter;
            }
        }

        protected char SrcPathSeparator
        {
            get
            {
                return m_sourceAdapter.PathSeparator;
            }
        }

        protected char TarPathSeparator
        {
            get
            {
                return m_targetAdapter.PathSeparator;
            }
        }

        protected Session VCSession
        {
            get;
            set;
        }
        
        [TestInitialize]
        public override void Initialize()
        {
            m_currentDirectory = Directory.GetCurrentDirectory();
            base.Initialize();

            TCAdapterEnvironment sourceTCAdapterEnv = TestEnvironment.SourceTCAdapterEnvironment;
            TCAdapterEnvironment targetTCAdapterEnv = TestEnvironment.TargetTCAdapterEnvironment;

            m_sourceAdapter = (IVCTestCaseAdapter)TestAdapterManager.LoadAdapter(new Guid(TestEnvironment.SourceTCAdapterEnvironment.AdapterRefName));
            m_targetAdapter = (IVCTestCaseAdapter)TestAdapterManager.LoadAdapter(new Guid(TestEnvironment.TargetTCAdapterEnvironment.AdapterRefName));

            m_sourceAdapter.Initialize(sourceTCAdapterEnv);
            m_targetAdapter.Initialize(targetTCAdapterEnv);

            MappingPair mapping = new MappingPair(m_sourceAdapter.FilterString, m_targetAdapter.FilterString);
            TestEnvironment.AddMapping(mapping);

            TestEnvironment.SourceWorkspaceLocalPath = m_sourceAdapter.WorkspaceLocalPath;
            TestEnvironment.TargetWorkspaceLocalPath = m_targetAdapter.WorkspaceLocalPath;

            // an extra file is usefull to make sure that the toolkit was not left in a bad state after migrating the scenario under test
            m_extraFile = new MigrationItemStrings("extraFile.txt", null, TestEnvironment, true);
            Trace.TraceInformation("Adding an extra file {0} -> {1}", m_extraFile.LocalPath, m_extraFile.ServerPath);

            SourceAdapter.AddFile(m_extraFile.LocalPath);

            InitializeTestCase();
            
            Trace.TraceInformation("Loaded VC test environment successfully");
        }

        [TestCleanup]
        public override void Cleanup()
        {
            SourceAdapter.Cleanup();
            TargetAdapter.Cleanup();
            Directory.SetCurrentDirectory(m_currentDirectory);
        }

        protected virtual void RunAndValidate(bool useExistingConfiguration, bool AddOnBranchSourceNotFound)
        {
            SourceAdapter.EditFile(m_extraFile.LocalPath);

            if (!useExistingConfiguration || Configuration == null)
            {
                // Generate a new configuration file
                Configuration = ConfigurationCreator.CreateConfiguration(TestConstants.ConfigurationTemplate.SingleVCSession, TestEnvironment);
                
                // Try moving foreach here ...
                foreach (var session in Configuration.SessionGroup.Sessions.Session)
                {
                    VCSession = session;
                    break;
                }

                ConfigurationCreator.CreateConfigurationFile(Configuration, ConfigurationFileName);
            }

            StartMigration();

            // Reset the current directory as VCServerdiff depends on binaries probe in the plugins folder
            Directory.SetCurrentDirectory(m_currentDirectory);
            VerifyMigration(AddOnBranchSourceNotFound);
        }

        protected void RunAndValidate(bool useExistingConfiguration)
        {
            RunAndValidate(useExistingConfiguration, false);
        }

        protected void RunAndValidate()
        {
            RunAndValidate(false, false);
        }
    }
}