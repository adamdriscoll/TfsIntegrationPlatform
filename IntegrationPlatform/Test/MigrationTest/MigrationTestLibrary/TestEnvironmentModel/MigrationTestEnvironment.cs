// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using System.IO;

namespace MigrationTestLibrary
{
    public class MigrationTestEnvironment
    {
        public delegate void Customize(MigrationTestEnvironment env, Configuration config);

        public event Customize CustomActions;

        /// <summary>
        /// Type of Migration Test to run (OneWay, TwoWayLeft, or TwoWayRight)
        /// Default is OneWay
        /// </summary>
        public MigrationTestType MigrationTestType = MigrationTestType.OneWay;

        public TestProjectList TestProjectList = new TestProjectList();

        public List<EndPoint> EndPointList = new List<EndPoint>();

        private TestProject TestProject { get; set; }

        public string ConfigurationFile
        {
            get { return TestProject.ConfigurationFile; }
        }

        [XmlIgnore]
        public WorkFlowType WorkFlowType { get; set; }

        [XmlIgnore]
        public EndPoint LeftEndPoint { get; private set; }

        [XmlIgnore]
        public EndPoint RightEndPoint { get; private set; }

        [XmlIgnore]
        public string TestName { get; set; }

        [XmlIgnore]
        public Dictionary<string, string> SnapshotStartPoints = new Dictionary<string, string>();

        [XmlIgnore]
        public int SnapshotBatchSize { get; set; }

        [XmlIgnore]
        public List<MappingPair> Mappings = new List<MappingPair>();

        [XmlIgnore]
        public string FirstSourceServerPath
        {
            get
            {
                Debug.Assert(Mappings.Count > 0);
                return Mappings[0].SourcePath;
            }
        }

        [XmlIgnore]
        public string FirstTargetServerPath
        {
            get
            {
                Debug.Assert(Mappings.Count > 0);
                return Mappings[0].TargetPath;
            }
        }

        [XmlIgnore]
        public string SourceWorkspaceLocalPath { get; set; }

        [XmlIgnore]
        public string TargetWorkspaceLocalPath { get; set; }

        public EndPoint SourceEndPoint
        {
            get
            {
                if (MigrationTestType == MigrationTestLibrary.MigrationTestType.TwoWayRight)
                {
                    return RightEndPoint;
                }
                else
                {
                    return LeftEndPoint;
                }
            }
        }

        public EndPoint TargetEndPoint
        {
            get
            {
                if (MigrationTestType == MigrationTestLibrary.MigrationTestType.TwoWayRight)
                {
                    return LeftEndPoint;
                }
                else
                {
                    return RightEndPoint;
                }
            }
        }

        public void CustomizeConfiguration(Configuration config)
        {
            if (null != CustomActions)
            {
                CustomActions(this, config);
            }
        }

        public void AddMapping(MappingPair mapping)
        {
            Mappings.Add(mapping);
        }

        internal void Initialize(string testProjectName)
        {
            if (MigrationTestType == MigrationTestType.OneWay)
            {
                WorkFlowType = new WorkFlowType()
                {
                    DirectionOfFlow = DirectionOfFlow.Unidirectional,
                    Frequency = Frequency.ContinuousManual,
                    SyncContext = SyncContext.Disabled,
                };
            }
            else
            {
                WorkFlowType = new WorkFlowType()
                {
                    DirectionOfFlow = DirectionOfFlow.Bidirectional,
                    Frequency = Frequency.ContinuousManual,
                    SyncContext = SyncContext.Disabled,
                };
            }

            foreach (TestProject p in TestProjectList.TestProject)
            {
                if (testProjectName.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    TestProject = p;
                    Trace.TraceInformation("Using Configuration File '{0}' for test project {1}", TestProject.ConfigurationFile, testProjectName);
                    break;
                }
            }

            foreach (EndPoint e in EndPointList)
            {
                if (String.Equals(e.ID, TestProject.LeftEndPointID, StringComparison.OrdinalIgnoreCase))
                {
                    LeftEndPoint = e;
                }
                if (String.Equals(e.ID, TestProject.RightEndPointID, StringComparison.OrdinalIgnoreCase))
                {
                    RightEndPoint = e;
                }
            }

            if (TestProject == null)
            {
                throw new Exception(String.Format("Test Project {0} is not defined in MigrationTestEnvironment", testProjectName));
            }

            if (LeftEndPoint == null)
            {
                throw new Exception(String.Format("Test Project {0} has an undefined Left EndPoint in MigrationTestEnvironment", TestProject.Name));
            }

            if (RightEndPoint == null)
            {
                throw new Exception(String.Format("Test Project {0} has an undefined Right EndPoint in MigrationTestEnvironment", TestProject.Name));
            }

            if (LeftEndPoint == RightEndPoint)
            {
                throw new Exception(String.Format("Test Project {0} has the same endpoint '{1}' defined for both ends in MigrationTestEnvironment", TestProject.Name, LeftEndPoint.ID));
            }

            LeftEndPoint.Initialize();
            RightEndPoint.Initialize();

            // Friendly names must be unique since we use them as ServerIdentifiers so append text to make it clear which side is which
            LeftEndPoint.FriendlyName += " (Left,";
            RightEndPoint.FriendlyName += " (Right,";
            TargetEndPoint.FriendlyName += "Target)";
            SourceEndPoint.FriendlyName += "Source)";

            Trace.TraceInformation("------ MigrationTestEnvironment details ------");
            Trace.TraceInformation("MigrationType: {0}", MigrationTestType);
            Trace.TraceInformation("ConfigurationFile: {0}", ConfigurationFile);
            Trace.TraceInformation(LeftEndPoint.ToString());
            Trace.TraceInformation(RightEndPoint.ToString());
            Trace.TraceInformation("----------------------------------------------");
        }

        /// <summary>
        /// Returns the FilterItem for the Target system
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        public FilterItem GetTargetFilterItem(FilterPair pair)
        {
            if (Guid.Equals(new Guid(pair.FilterItem[0].MigrationSourceUniqueId), TargetEndPoint.InternalUniqueID))
            {
                return pair.FilterItem[0];
            }
            else if (Guid.Equals(new Guid(pair.FilterItem[1].MigrationSourceUniqueId), TargetEndPoint.InternalUniqueID))
            {
                return pair.FilterItem[1];
            }

            throw new Exception(String.Format("Failed to find MigrationSourceUniqueId {0} in FilterPair {1}", TargetEndPoint.InternalUniqueID, pair));
        }

        /// <summary>
        /// Returns the FilterItem for the Source system
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        public FilterItem GetSourceFilterItem(FilterPair pair)
        {
            if (Guid.Equals(new Guid(pair.FilterItem[0].MigrationSourceUniqueId), SourceEndPoint.InternalUniqueID))
            {
                return pair.FilterItem[0];
            }
            else if (Guid.Equals(new Guid(pair.FilterItem[1].MigrationSourceUniqueId), SourceEndPoint.InternalUniqueID))
            {
                return pair.FilterItem[1];
            }

            throw new Exception(String.Format("Failed to find MigrationSourceUniqueId {0} in FilterPair {1}", SourceEndPoint.InternalUniqueID, pair));
        }

        public SourceSideTypeEnum GetSourceSideTypeEnum()
        {
            if (MigrationTestType == MigrationTestLibrary.MigrationTestType.TwoWayRight)
            {
                return SourceSideTypeEnum.Right;
            }
            else
            {
                return SourceSideTypeEnum.Left;
            }
        }

        public SourceSideTypeEnum GetTargetSideTypeEnum()
        {
            if (MigrationTestType == MigrationTestLibrary.MigrationTestType.TwoWayRight)
            {
                return SourceSideTypeEnum.Left;
            }
            else
            {
                return SourceSideTypeEnum.Right;
            }
        }

        internal MigrationSource GetTargetMigrationSource(Configuration config)
        {
            MigrationSource zero = config.SessionGroup.MigrationSources.MigrationSource[0];
            MigrationSource one = config.SessionGroup.MigrationSources.MigrationSource[1];

            if (Guid.Equals(new Guid(zero.InternalUniqueId), TargetEndPoint.InternalUniqueID))
            {
                return zero;
            }
            else if (Guid.Equals(new Guid(one.InternalUniqueId), TargetEndPoint.InternalUniqueID))
            {
                return one;
            }

            throw new Exception(String.Format("Failed to find TargetMigrationSource {0} in {1} or {2}", TargetEndPoint.InternalUniqueID, zero, one));
        }

        /// <summary>
        /// Create a new Value and populate left/right with the correct source/target values
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        internal Value NewValue(string source, string target)
        {
            Value value = new Value();

            if (MigrationTestType == MigrationTestType.TwoWayRight)
            {
                value.LeftValue = target;
                value.RightValue = source;
            }
            else
            {
                value.LeftValue = source;
                value.RightValue = target;
            }
            return value;
        }

        internal MappedField NewMappedField(string source, string target)
        {
            MappedField field = new MappedField();

            if (MigrationTestType == MigrationTestType.TwoWayRight)
            {
                field.LeftName = target;
                field.RightName = source;
                field.MapFromSide = SourceSideTypeEnum.Right;
            }
            else
            {
                field.LeftName = source;
                field.RightName = target;
                field.MapFromSide = SourceSideTypeEnum.Left;
            }
            return field;
        }

        // constants
        public const string DefaultTestEnvironmentVariableName = "MigrationTestEnvironment";
        public const string DefaultEnvironmentFileName = "MigrationTestEnvironment.xml";

        public static MigrationTestEnvironment Load(string testName)
        {
            return MigrationTestEnvironment.Load(testName, Environment.GetEnvironmentVariable(DefaultTestEnvironmentVariableName));
        }

        public static MigrationTestEnvironment Load(string testName, string fileName)
        {
            MigrationTestEnvironment env = null;

            if (String.IsNullOrEmpty(fileName))
            {
                fileName = DefaultEnvironmentFileName;
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(MigrationTestEnvironment));
                    env = serializer.Deserialize(fs) as MigrationTestEnvironment;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to load {0}", fileName);
                throw ex;
            }

            if (env != null)
            {
                env.TestName = testName;
            }

            return env;
        }
    }
}
