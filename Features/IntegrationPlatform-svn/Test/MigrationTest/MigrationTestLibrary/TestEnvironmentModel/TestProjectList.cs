// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TestProjectList : ModelObject
    {
        private NotifyingCollection<TestProject> __TestProject;

        [XmlElement]
        public NotifyingCollection<TestProject> TestProject
        {
            get
            {
                if (__TestProject == null)
                {
                    __TestProject = new NotifyingCollection<TestProject>();
                }
                return __TestProject;
            }
        }

        public TestProjectList()
        {
        }
    }
}
