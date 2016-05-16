// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    public partial class CustomSettingsElement
    {
        public bool ContainsKey(string settingKey)
        {
            if (string.IsNullOrEmpty(settingKey))
            {
                throw new ArgumentNullException("settingKey");
            }

            foreach (CustomSetting setting in this.CustomSetting)
            {
                if (SettingContainsKey(settingKey, setting))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSettingValue(string settingKey, out CustomSetting setting)
        {
            if (string.IsNullOrEmpty(settingKey))
            {
                throw new ArgumentNullException("settingKey");
            }

            foreach (CustomSetting cs in this.CustomSetting)
            {
                if (SettingContainsKey(settingKey, cs))
                {
                    setting = cs;
                    return true;
                }
            }

            setting = null;
            return false;
        }

        [XmlIgnore]
        public CustomSetting this[string settingKey]
        {
            get
            {
                CustomSetting retVal;
                if (TryGetSettingValue(settingKey, out retVal))
                {
                    return retVal;
                }
                else
                {
                    throw new IndexOutOfRangeException(
                        string.Format("Cannot find custom setting with the key '{0}'", settingKey));
                }
            }
        }

        private static bool SettingContainsKey(string settingKey, CustomSetting setting)
        {
            return settingKey.Equals(setting.SettingKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}
