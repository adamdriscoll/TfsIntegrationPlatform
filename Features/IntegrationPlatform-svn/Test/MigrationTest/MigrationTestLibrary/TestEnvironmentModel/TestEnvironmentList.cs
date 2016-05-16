// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TestEnvironmentList : ModelObject
    {
        private NotifyingCollection<TestEnvironment> __TestEnv;

        [XmlElement]
        public NotifyingCollection<TestEnvironment> TestEnv
        {
            get
            {
                if (__TestEnv == null)
                {
                    __TestEnv = new NotifyingCollection<TestEnvironment>();
                }
                return __TestEnv;
            }
        }

        public TestEnvironmentList()
        {
        }
    }
}
