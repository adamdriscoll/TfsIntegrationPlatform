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
    public class TestProject : ModelObject
    {
        private string __Name;

        [XmlAttribute]
        public string Name
        {
            get
            {
                return __Name;
            }
            set
            {
                if (value != __Name)
                {
                    string oldValue = __Name;
                    __Name = value;
                    this.RaisePropertyChangedEvent("Name", oldValue, value);
                }
            }
        }

        private string __DefaultTestEnvironmentId;

        [XmlAttribute]
        public string DefaultTestEnvironmentId
        {
            get
            {
                return __DefaultTestEnvironmentId;
            }
            set
            {
                if (value != __DefaultTestEnvironmentId)
                {
                    string oldValue = __DefaultTestEnvironmentId;
                    __DefaultTestEnvironmentId = value;
                    this.RaisePropertyChangedEvent("DefaultTestEnvironmentId", oldValue, value);
                }
            }
        }

        [XmlAttribute]
        public string ConfigurationFile { get; set; }
    }
}
