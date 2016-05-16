// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class LinkConfigurationLookupService
    {
        internal LinkConfigurationLookupService(LinkingElement linkingElement)
        {
            m_linkTypeMaps = new Dictionary<Guid, Dictionary<string, string>>();

            if (linkingElement.LinkTypeMappings != null
                && linkingElement.LinkTypeMappings.LinkTypeMapping != null)
            {
                foreach (LinkTypeMapping linkTypeMap in linkingElement.LinkTypeMappings.LinkTypeMapping)
                {
                    var leftSourceId = new Guid(linkTypeMap.LeftMigrationSourceUniqueId);
                    var rightSourceId = new Guid(linkTypeMap.RightMigrationSourceUniqueId);

                    if (!m_linkTypeMaps.ContainsKey(leftSourceId))
                    {
                        m_linkTypeMaps.Add(leftSourceId, new Dictionary<string, string>());
                    }
                    if (!m_linkTypeMaps.ContainsKey(rightSourceId))
                    {
                        m_linkTypeMaps.Add(rightSourceId, new Dictionary<string, string>());
                    }

                    if (m_linkTypeMaps[leftSourceId].ContainsKey(linkTypeMap.LeftLinkType))
                    {
                        m_linkTypeMaps[leftSourceId].Remove(linkTypeMap.LeftLinkType);
                    }
                    m_linkTypeMaps[leftSourceId].Add(linkTypeMap.LeftLinkType, linkTypeMap.RightLinkType);

                    if (m_linkTypeMaps[rightSourceId].ContainsKey(linkTypeMap.RightLinkType))
                    {
                        m_linkTypeMaps[rightSourceId].Remove(linkTypeMap.RightLinkType);
                    }
                    m_linkTypeMaps[rightSourceId].Add(linkTypeMap.RightLinkType, linkTypeMap.LeftLinkType);
                }
            }

            IsLinkingDisabled = false;
            foreach (CustomSetting setting in linkingElement.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(Constants.DisableLinking))
                {
                    string disableLinkingSettingValueStr = setting.SettingValue;
                    if (string.IsNullOrEmpty(disableLinkingSettingValueStr))
                    {
                        break;
                    }

                    bool disableLinkSettingValue;
                    if (!bool.TryParse(disableLinkingSettingValueStr, out disableLinkSettingValue))
                    {
                        break;
                    }

                    IsLinkingDisabled = disableLinkSettingValue;
                }
            }
        }

        public string FindMappedLinkType(Guid sourceId, string sourceLinkTypeReferenceName)
        {
            if (m_linkTypeMaps.ContainsKey(sourceId) && m_linkTypeMaps[sourceId].ContainsKey(sourceLinkTypeReferenceName))
            {
                return m_linkTypeMaps[sourceId][sourceLinkTypeReferenceName];
            }
            
            return sourceLinkTypeReferenceName;
        }

        internal bool IsLinkingDisabled
        {
            get;
            private set;
        }

        private readonly Dictionary<Guid, Dictionary<string, string>> m_linkTypeMaps;
    }
}