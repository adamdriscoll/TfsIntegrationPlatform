// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TestAdapterList : ModelObject
    {
        private TestAdapter __TestAdapter;

        [XmlElement]
        public TestAdapter TestAdapter
        {
            get
            {
                if (__TestAdapter == null)
                {
                    __TestAdapter = new TestAdapter();
                    this.RaisePropertyChangedEvent("TestAdapter", null, __TestAdapter);
                }
                return __TestAdapter;
            }
            set
            {
                if (value != __TestAdapter)
                {
                    TestAdapter oldValue = __TestAdapter;
                    __TestAdapter = value;
                    this.RaisePropertyChangedEvent("TestAdapter", oldValue, value);
                }
            }
        }

        public TestAdapterList()
        {
        }
    }
}
