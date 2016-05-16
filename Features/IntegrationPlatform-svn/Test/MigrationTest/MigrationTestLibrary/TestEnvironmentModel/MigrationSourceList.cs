// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class MigrationSourceList : ModelObject
    {
        private NotifyingCollection<TestMigrationSource> __Source;

        [XmlElement]
        public NotifyingCollection<TestMigrationSource> Source
        {
            get
            {
                if (__Source == null)
                {
                    __Source = new NotifyingCollection<TestMigrationSource>();
                }
                return __Source;
            }
        }

        public MigrationSourceList()
        {
        }
    }
}
