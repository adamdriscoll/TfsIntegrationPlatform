// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    public class CQLoginCredentialManagerFactory
    {
        /// <summary>
        /// Creates a credential manager based on the migration source configuration
        /// </summary>
        /// <param name="credManagementService"></param>
        /// <param name="migrationSourceConfig"></param>
        /// <returns>Created credential manager</returns>
        /// <exception cref="">ClearQuestAdapter.Exceptions.ClearQuestInvalidConfigurationException</exception>
        public static ICQLoginCredentialManager CreateCredentialManager(
            ICredentialManagementService credManagementService,
            MigrationSource migrationSourceConfig)
        {
            if (null != credManagementService &&
                credManagementService.IsMigrationSourceConfiguredToUseStoredCredentials(new Guid(migrationSourceConfig.InternalUniqueId)))
            {
                if (OSIsNotSupported())
                {
                    throw new System.NotSupportedException(ClearQuestResource.StoredCredentialNotSupported);
                }

                return new CQStoredCredentialManager(credManagementService, migrationSourceConfig);
            }
            else
            {
                foreach (var setting in migrationSourceConfig.CustomSettings.CustomSetting)
                {
                    if (setting.SettingKey.Equals(ClearQuestConstants.LoginCredentialSettingKey, StringComparison.Ordinal))
                    {
                        return GetTypedCredentialManager(setting.SettingValue, migrationSourceConfig);
                    }
                }

                throw new ClearQuestInvalidConfigurationException(ClearQuestResource.ClearQuest_Config_MissingLoginCredentialSettingType);
            }
        }

        public static bool OSIsNotSupported()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    switch (Environment.OSVersion.Version.Major)
                    {
                        case 3: // Windows NT 3.51
                        case 4: // Windows NT 4.0
                            return true;
                        case 5:
                            switch (Environment.OSVersion.Version.Minor)
                            {
                                case 0: // Windows 2000
                                case 1: // Windows XP
                                case 2: // Windows Server 2003
                                    return true;
                                default:
                                    return false;
                            }
                        default:
                            return false;
                    }
                default:
                    return true;
            }
        }

        private static ICQLoginCredentialManager GetTypedCredentialManager(
            string loginCredentialSettingValues,
            MigrationSource migrationSourceConfig)
        {
            if (string.IsNullOrEmpty(loginCredentialSettingValues))
            {
                throw new ClearQuestInvalidConfigurationException(ClearQuestResource.ClearQuest_Config_MissingLoginCredentialSettingType);
            }

            if (loginCredentialSettingValues.Equals(ClearQuestConstants.LoginCredentialSettingUseTextUsernamePasswordPairInConfig, StringComparison.Ordinal))
            {
                return new CQTextLoginCredentialManager(migrationSourceConfig.CustomSettings.CustomSetting);
            }
            else if (loginCredentialSettingValues.Equals(ClearQuestConstants.LoginCredentialSettingUseStoredCredential, StringComparison.Ordinal))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new ClearQuestInvalidConfigurationException(ClearQuestResource.ClearQuest_Config_UnknownLoginCredentialSettingType);
            }
        }
    }
}
