// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TCCustomSettings : ModelObject
    {
        private NotifyingCollection<Setting> __Setting;

        [XmlElement]
        public NotifyingCollection<Setting> Setting
        {
            get
            {
                if (__Setting == null)
                {
                    __Setting = new NotifyingCollection<Setting>();
                }
                return __Setting;
            }
        }

        public TCCustomSettings()
        {
        }
    }
}
