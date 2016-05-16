// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TCAdapterEnvironment
    {
        [XmlIgnore]
        public string TestName { get; set; }

        [XmlIgnore]
        public DateTime TestStartTime { get; set; }

        [XmlAttribute]
        public string AdapterRefName { get; set; }

        public string ServerUrl { get; set; }
        public string TeamProject { get; set; }
        public string WorkspaceName { get; set; }
        public string VobName { get; set; }
        public string ViewName { get; set; }
        public string UncStorageLocation { get; set; }
        public string LocalStorageLocation { get; set; }
    }

}
