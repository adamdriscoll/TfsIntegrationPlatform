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
    public class TestProject
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string LeftEndPointID { get; set; }

        [XmlAttribute]
        public string RightEndPointID { get; set; }

        [XmlAttribute]
        public string ConfigurationFile { get; set; }
    }
}
