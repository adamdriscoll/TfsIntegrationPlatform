// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class Setting : ModelObject
    {
        private string __SettingKey;

        [XmlAttribute]
        public string SettingKey
        {
            get
            {
                return __SettingKey;
            }
            set
            {
                if (value != __SettingKey)
                {
                    string oldValue = __SettingKey;
                    __SettingKey = value;
                    this.RaisePropertyChangedEvent("SettingKey", oldValue, value);
                }
            }
        }

        private string __SettingValue;

        [XmlAttribute]
        public string SettingValue
        {
            get
            {
                return __SettingValue;
            }
            set
            {
                if (value != __SettingValue)
                {
                    string oldValue = __SettingValue;
                    __SettingValue = value;
                    this.RaisePropertyChangedEvent("SettingValue", oldValue, value);
                }
            }
        }

        public Setting()
        {
        }
    }
}
