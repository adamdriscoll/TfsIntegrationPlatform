// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public class MigrationTestEnvironment : ModelObject
    {
        public delegate void Customize(Configuration config);

        public event Customize CustomActions;

        [XmlIgnore]
        private TestEnvironment m_activeTestEnvironment;

        [XmlIgnore]
        private List<MappingPair> m_mappings = new List<MappingPair>();

        [XmlIgnore]
        private Dictionary<string, string> m_snapshotStartPoints = new Dictionary<string, string>();

        [XmlIgnore]
        private int m_snapshotBatchSize = 0;

        [XmlIgnore]
        // Migration direction - by default from left migration source to right migration source
        private bool m_isLeftToRightWorkflow = true;

        [XmlIgnore]
        // Migration direction - by default from left migration source to right migration source
        private string m_testProjectName = string.Empty;

        [XmlIgnore]
        public string ConfigurationFile { get; set; }

        #region properties
        [XmlIgnore]
        public TestEnvironment Environment
        {
            get
            {
                if (m_activeTestEnvironment == null)
                {
                    string testEnvId = String.Empty;
                    foreach (TestProject p in TestProjectList.TestProject)
                    {
                        if (m_testProjectName.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            testEnvId = p.DefaultTestEnvironmentId;
                            Trace.TraceInformation("Using Environment ID {0} for test project {1}", testEnvId, p.Name);
                            ConfigurationFile = p.ConfigurationFile;
                            Trace.TraceInformation("Using Configuration File '{0}' for test project {1}", ConfigurationFile, p.Name);
                            break;
                        }
                    }

                    foreach (TestEnvironment env in TestEnvironmentList.TestEnv)
                    {
                        if (String.Equals(env.Id, testEnvId, StringComparison.OrdinalIgnoreCase))
                        {
                            m_activeTestEnvironment = env;
                            Trace.TraceInformation("Found Environment ID {0} Source ID {1} Target ID {2}", env.Id, env.SourceMigrationSourcdId, env.TargetMigrationSourceId);
                            break;
                        }
                    }
                }

                return m_activeTestEnvironment;
            }
        }

        [XmlIgnore]
        public WorkFlowType WorkFlowType { get; set; }

        private TestMigrationSource m_source;
        private TestMigrationSource m_target;

        [XmlIgnore]
        public TestMigrationSource SourceMigrationSource
        {
            get
            {
                if (m_source == null)
                {
                    foreach (TestMigrationSource e in MigrationSourceList.Source)
                    {
                        if (e.Id == Environment.SourceMigrationSourcdId)
                        {
                            Trace.WriteLine(String.Format("SourceMigration URL: {0}", e.TCAdapterEnv.ServerUrl));
                            m_source = e;
                            break;
                        }
                    }
                }

                return m_source;
            }
        }

        [XmlIgnore]
        public TestMigrationSource TargetMigrationSource
        {
            get
            {
                if (m_target == null)
                {
                    foreach (TestMigrationSource e in MigrationSourceList.Source)
                    {
                        if (e.Id == Environment.TargetMigrationSourceId)
                        {
                            Trace.WriteLine(String.Format("TargetMigration URL: {0}", e.TCAdapterEnv.ServerUrl));
                            m_target = e;
                        }
                    }
                }

                return m_target;
            }
        }

        [XmlIgnore]
        public string SourceWorkspaceName
        {
            get
            {
                return SourceMigrationSource.TCAdapterEnv.WorkspaceName;
            }
        }

        [XmlIgnore]
        public string TargetWorkspaceName
        {
            get
            {
                return TargetMigrationSource.TCAdapterEnv.WorkspaceName;
            }
        }

        [XmlIgnore]
        public string SourceServerUrl
        {
            get
            {
                return SourceMigrationSource.TCAdapterEnv.ServerUrl;
            }
        }

        [XmlIgnore]
        public string TargetServerUrl
        {
            get
            {
                return TargetMigrationSource.TCAdapterEnv.ServerUrl;
            }
        }

        [XmlIgnore]
        public string SourceTeamProject
        {
            get
            {
                return SourceMigrationSource.TCAdapterEnv.TeamProject;
            }
        }

        [XmlIgnore]
        public string TargetTeamProject
        {
            get
            {
                return TargetMigrationSource.TCAdapterEnv.TeamProject;
            }
        }

        [XmlIgnore]
        public TFSVersionEnum SourceTFSVersion
        {
            get
            {
                return SourceMigrationSource.TFSVersion;
            }
        }

        [XmlIgnore]
        public TFSVersionEnum TargetTFSVersion
        {
            get
            {
                return TargetMigrationSource.TFSVersion;
            }
        }

        [XmlIgnore]
        public string SourceProviderRefName
        {
            get
            {
                Trace.TraceInformation("SourceProviderRefName: {0}", SourceMigrationSource.ProviderRefName);
                return SourceMigrationSource.ProviderRefName;
            }
        }

        [XmlIgnore]
        public string TargetProviderRefName
        {
            get
            {
                Trace.TraceInformation("TargetProviderRefName: {0}", TargetMigrationSource.ProviderRefName);
                return TargetMigrationSource.ProviderRefName;
            }
        }

        [XmlIgnore]
        public string TestName { get; set; }

        [XmlIgnore]
        public Dictionary<string, string> SnapshotStartPoints
        {
            get
            {
                return m_snapshotStartPoints;
            }
            set
            {
                m_snapshotStartPoints = value;
            }
        }

        [XmlIgnore]
        public int SnapshotBatchSize
        {
            get
            {
                return m_snapshotBatchSize;
            }
            set
            {
                m_snapshotBatchSize = value;
            }
        }

        [XmlIgnore]
        public List<MappingPair> Mappings
        {
            get
            {
                return m_mappings;
            }
        }

        [XmlIgnore]
        public string FirstSourceServerPath
        {
            get
            {
                Debug.Assert(m_mappings.Count > 0);
                return m_mappings[0].SourcePath;
            }
        }

        [XmlIgnore]
        public string FirstTargetServerPath
        {
            get
            {
                Debug.Assert(m_mappings.Count > 0);
                return m_mappings[0].TargetPath;
            }
        }

        [XmlIgnore]
        public string SourceWorkspaceLocalPath
        {
            get;
            set;
        }

        [XmlIgnore]
        public string TargetWorkspaceLocalPath
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsLeftToRightWorkflow
        {
            get
            {
                return m_isLeftToRightWorkflow;
            }
            set
            {
                m_isLeftToRightWorkflow = value;
            }
        }

        [XmlIgnore]
        public TCAdapterEnvironment SourceTCAdapterEnvironment
        {
            get
            {
                return SourceMigrationSource.TCAdapterEnv;
            }
        }

        [XmlIgnore]
        public TCAdapterEnvironment TargetTCAdapterEnvironment
        {
            get
            {
                return TargetMigrationSource.TCAdapterEnv;
            }
        }
        #endregion

        public void CustomizeConfiguration(Configuration config)
        {
            if (null != CustomActions)
            {
                CustomActions(config);
            }
        }

        public void AddMapping(MappingPair mapping)
        {
            Mappings.Add(mapping);
        }

        internal void Initialize(string testProjectName)
        {
            WorkFlowType = new WorkFlowType();
            WorkFlowType.DirectionOfFlow = DirectionOfFlow.Unidirectional;
            WorkFlowType.Frequency = Frequency.ContinuousManual;
            WorkFlowType.SyncContext = SyncContext.Unidirectional;
            IsLeftToRightWorkflow = true;

            m_testProjectName = testProjectName;
        }

        [XmlIgnore]
        private TestProjectList __TestProjectList;

        public TestProjectList TestProjectList
        {
            get
            {
                if (__TestProjectList == null)
                {
                    __TestProjectList = new TestProjectList();
                    this.RaisePropertyChangedEvent("TestProjectList", null, __TestProjectList);
                }
                return __TestProjectList;
            }
            set
            {
                if (value != __TestProjectList)
                {
                    TestProjectList oldValue = __TestProjectList;
                    __TestProjectList = value;
                    this.RaisePropertyChangedEvent("TestProjectList", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private TestAdapterList __TestAdapterList;

        public TestAdapterList TestAdapterList
        {
            get
            {
                if (__TestAdapterList == null)
                {
                    __TestAdapterList = new TestAdapterList();
                    this.RaisePropertyChangedEvent("TestAdapterList", null, __TestAdapterList);
                }
                return __TestAdapterList;
            }
            set
            {
                if (value != __TestAdapterList)
                {
                    TestAdapterList oldValue = __TestAdapterList;
                    __TestAdapterList = value;
                    this.RaisePropertyChangedEvent("TestAdapterList", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private TestEnvironmentList __TestEnvironmentList;

        public TestEnvironmentList TestEnvironmentList
        {
            get
            {
                if (__TestEnvironmentList == null)
                {
                    __TestEnvironmentList = new TestEnvironmentList();
                    this.RaisePropertyChangedEvent("TestEnvironmentList", null, __TestEnvironmentList);
                }
                return __TestEnvironmentList;
            }
            set
            {
                if (value != __TestEnvironmentList)
                {
                    TestEnvironmentList oldValue = __TestEnvironmentList;
                    __TestEnvironmentList = value;
                    this.RaisePropertyChangedEvent("TestEnvironmentList", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private MigrationSourceList __MigrationSourceList;

        public MigrationSourceList MigrationSourceList
        {
            get
            {
                if (__MigrationSourceList == null)
                {
                    __MigrationSourceList = new MigrationSourceList();
                    this.RaisePropertyChangedEvent("MigrationSourceList", null, __MigrationSourceList);
                }
                return __MigrationSourceList;
            }
            set
            {
                if (value != __MigrationSourceList)
                {
                    MigrationSourceList oldValue = __MigrationSourceList;
                    __MigrationSourceList = value;
                    this.RaisePropertyChangedEvent("MigrationSourceList", oldValue, value);
                }
            }
        }

        public MigrationTestEnvironment()
        {
        }
    }
}
