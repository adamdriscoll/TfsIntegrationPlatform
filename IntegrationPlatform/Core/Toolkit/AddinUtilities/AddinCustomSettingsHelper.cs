// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static class AddinCustomSettingsHelper
    {
        /// <summary>
        /// This method allows an Addin implementation to easily retrieve any CustomSettings defined for the Addin from the Configuration
        /// </summary>
        /// <param name="configurationService">The ConfigurationService which the Addin can obtain from the ToolkitServiceContainer</param>
        /// <param name="addinReferenceName">The reference name of the Addin making the call</param>
        /// <param name="customSettingsKeysAndValues">Add Dictionary of name/value pairs; the caller should fill in the keys with 
        /// the expected CustomSetting names and may also set default values in the values of each pair.
        /// For each of the named CustomSettings that is found in the configuration for the Addin, the value in the Dictionary will be
        /// replaced with the CustomSetting value.</param>
        public static void GetAddinCustomSettings(
            ConfigurationService configurationService,
            string addinReferenceName,
            Dictionary<string, string> customSettingsKeysAndValues)
        {
            // BEGIN SUPPORT FOR COMPATABILITY WITH OLD CONFIG FILES
            foreach (CustomSetting setting in configurationService.MigrationSource.CustomSettings.CustomSetting)
            {
                if (customSettingsKeysAndValues.ContainsKey(setting.SettingKey))
                {
                    customSettingsKeysAndValues[setting.SettingKey] = setting.SettingValue;
                }
            }
            // END SUPPORT FOR COMPATABILITY WITH OLD CONFIG FILES

            // Settings in the new format will override those in the old location if both are present
            AddinElement requestedAddinElement = null;
            foreach (AddinElement addinElement in configurationService.MigrationSource.Settings.Addins.Addin)
            {
                if (string.Equals(addinElement.ReferenceName, addinReferenceName, StringComparison.OrdinalIgnoreCase))
                {
                    requestedAddinElement = addinElement;
                    break;
                }
            }
            if (requestedAddinElement != null)
            {
                foreach (CustomSetting setting in requestedAddinElement.CustomSettings.CustomSetting)
                {
                    if (customSettingsKeysAndValues.ContainsKey(setting.SettingKey))
                    {
                        customSettingsKeysAndValues[setting.SettingKey] = setting.SettingValue;
                    }
                }
            }
        }
    }
}
