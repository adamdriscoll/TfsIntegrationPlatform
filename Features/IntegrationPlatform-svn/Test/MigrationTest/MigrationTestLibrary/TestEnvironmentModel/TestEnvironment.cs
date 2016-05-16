// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TestEnvironment : ModelObject
    {
        private IEnumerable<TestMigrationSource> m_migrationSourceList;

        [XmlIgnore]
        public IEnumerable<TestMigrationSource> TestMigrationSourceList
        {
            get
            {
                return m_migrationSourceList;
            }
            set
            {
                m_migrationSourceList = value;
            }
        }

        private string __Id;

        [XmlAttribute]
        public string Id
        {
            get
            {
                return __Id;
            }
            set
            {
                if (value != __Id)
                {
                    string oldValue = __Id;
                    __Id = value;
                    this.RaisePropertyChangedEvent("Id", oldValue, value);
                }
            }
        }

        private string __TestProject;

        [XmlAttribute]
        public string TestProject
        {
            get
            {
                return __TestProject;
            }
            set
            {
                if (value != __TestProject)
                {
                    string oldValue = __TestProject;
                    __TestProject = value;
                    this.RaisePropertyChangedEvent("TestProject", oldValue, value);
                }
            }
        }

        private string __SourceMigrationSourcdId;

        [XmlAttribute]
        public string SourceMigrationSourcdId
        {
            get
            {
                return __SourceMigrationSourcdId;
            }
            set
            {
                if (value != __SourceMigrationSourcdId)
                {
                    string oldValue = __SourceMigrationSourcdId;
                    __SourceMigrationSourcdId = value;
                    this.RaisePropertyChangedEvent("SourceMigrationSourcdId", oldValue, value);
                }
            }
        }

        private string __TargetMigrationSourceId;

        [XmlAttribute]
        public string TargetMigrationSourceId
        {
            get
            {
                return __TargetMigrationSourceId;
            }
            set
            {
                if (value != __TargetMigrationSourceId)
                {
                    string oldValue = __TargetMigrationSourceId;
                    __TargetMigrationSourceId = value;
                    this.RaisePropertyChangedEvent("TargetMigrationSourceId", oldValue, value);
                }
            }
        }

        public TestEnvironment()
        {
        }
    }
}
