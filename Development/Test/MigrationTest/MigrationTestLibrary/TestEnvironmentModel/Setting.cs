// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;
using System;

namespace MigrationTestLibrary
{
    public class Setting
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Value { get; set; }

        public Setting()
        {
        }

        public override string ToString()
        {
            return String.Format("Setting Key={0} Value={1}", Key, Value);
        }
    }
}
