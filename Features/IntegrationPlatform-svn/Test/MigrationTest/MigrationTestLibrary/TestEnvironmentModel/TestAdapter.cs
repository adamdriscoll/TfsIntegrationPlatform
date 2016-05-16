// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TestAdapter : ModelObject
    {
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

        [XmlIgnore]
        private string __FriendlyName;

        [XmlAttribute(AttributeName = "FriendlyName", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string FriendlyName
        {
            get
            {
                return __FriendlyName;
            }
            set
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
        }

        public TestAdapter()
        {
        }
    }
}
