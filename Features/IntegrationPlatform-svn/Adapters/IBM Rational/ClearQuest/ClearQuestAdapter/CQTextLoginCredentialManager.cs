// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    /// <summary>
    /// class CQTextLoginCredentialManager
    /// </summary>
    /// <remarks>
    /// This credential manager extracts the login username and password
    /// information from the session configuration custom settings
    /// </remarks>
    internal class CQTextLoginCredentialManager : ICQLoginCredentialManager
    {
        #region ICQLoginCredientialManager Members

        public string UserName
        {
            get;
            private set;
        }

        public string Password
        {
            get;
            private set;
        }

        public string AdminUserName
        {
            get;
            private set;
        }

        public string AdminPassword
        {
            get;
            private set;
        }

        #endregion

        public CQTextLoginCredentialManager(
            NotifyingCollection<CustomSetting> settings)
        {
            UserName = null;
            Password = null;
            AdminUserName = string.Empty;   // admin session login is optional
            AdminPassword = string.Empty;   // admin session login is optional
            foreach (var setting in settings)
            {
                if (setting.SettingKey.Equals(ClearQuestConstants.UserNameKey))
                {
                    UserName = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.PasswordKey))
                {
                    Password = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.AdminUserNameKey))
                {
                    AdminUserName = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.AdminPasswordKey))
                {
                    AdminPassword = setting.SettingValue;
                }
            }

            if (UserName == null || Password == null)
            {
                throw new ClearQuestInvalidConfigurationException(ClearQuestResource.ClearQuest_Config_IncompleteLoginCredentialSetting);
            }
        }
    }
}
