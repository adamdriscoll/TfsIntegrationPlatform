// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class CommentDecorationService : ICommentDecorationService
    {
        // CustomSettingKeyNames
        private const string c_platformCommentSuffixType = "PlatformCommentSuffixType";
        private const string c_useUrlInPlatformCommentSuffix = "UseUrlInPlatformCommentSuffix";
        private const string c_customCommentSuffix = "CustomCommentSuffix";

        private ConfigurationService m_configurationService;
        private PlatformCommentSuffixType m_commentSuffixType;
        private bool m_useUrlInPlatformCommentSuffix;
        private string m_customCommentSuffix;

        public CommentDecorationService(RuntimeSession session, IServiceContainer serviceContainer)
        {
            m_configurationService = serviceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            Debug.Assert(m_configurationService != null);

            m_commentSuffixType = PlatformCommentSuffixType.Verbose;
            m_customCommentSuffix = string.Empty;

            foreach (CustomSetting setting in m_configurationService.SessionGroup.CustomSettings.CustomSetting)
            {
                if (string.Equals(setting.SettingKey, c_platformCommentSuffixType, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(setting.SettingValue, PlatformCommentSuffixType.Minimal.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        m_commentSuffixType = PlatformCommentSuffixType.Minimal;
                    }
                    continue;
                }

                if (string.Equals(setting.SettingKey, c_useUrlInPlatformCommentSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(setting.SettingValue, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        m_useUrlInPlatformCommentSuffix = true;
                    }
                    continue;
                }

                if (string.Equals(setting.SettingKey, c_customCommentSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    m_customCommentSuffix = setting.SettingValue;
                    continue;
                }
            }

        }

        #region ICommentDecorationService implementation

        /// <summary>
        /// Get the suffix to be appended to ChangeGroup comments for ChangeGroups migrated by the Integration Platform.
        /// The returned string may or may not include the arguments passed in depending on the Session configuration setting for PlatformCommentSuffixType,
        /// and will include any CustomCommentSuffix specified in the Session configuration
        /// </summary>
        /// <param name="source">A string that identifies the peer server from which the ChangeGroup was migrated</param>
        /// <param name="id">An Id that identifies corresponding item on the peer server</param>
        /// <returns></returns>
        public string GetChangeGroupCommentSuffix(string id)
        {
            if (m_commentSuffixType == PlatformCommentSuffixType.Minimal)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0};{1}{2}",
                    Constants.PlatformCommentSuffixMarker, m_customCommentSuffix, Constants.PlatformCommentSuffixMarkerEnd);
            }
            else
            {
                string sourceIdentifier = m_useUrlInPlatformCommentSuffix ?
                    m_configurationService.PeerMigrationSource.ServerUrl : m_configurationService.PeerMigrationSource.FriendlyName;
                if (string.IsNullOrEmpty(sourceIdentifier))
                {
                    sourceIdentifier = MigrationToolkitResources.UnknownSource;
                }

                return string.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.VerbosePlatformCommentSuffixFormat,
                    Constants.PlatformCommentSuffixMarker, sourceIdentifier, id, m_customCommentSuffix, Constants.PlatformCommentSuffixMarkerEnd);
            }
        }

        public string AddToChangeGroupCommentSuffix(string currentCommentOrSuffix, string additionalText)
        {
            if (m_commentSuffixType == PlatformCommentSuffixType.Minimal)
            {
                return currentCommentOrSuffix;
            }
            else
            {
                return currentCommentOrSuffix + "(" + additionalText + ")";
            }
        }

        /// <summary>
        /// Returns true if and only if the comment string argument contains an Integration Platform comment suffix
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool HasPlatformCommentSuffix(string comment)
        {
            return comment.Contains(Constants.PlatformCommentSuffixMarker);
        }
        #endregion
    }
}
