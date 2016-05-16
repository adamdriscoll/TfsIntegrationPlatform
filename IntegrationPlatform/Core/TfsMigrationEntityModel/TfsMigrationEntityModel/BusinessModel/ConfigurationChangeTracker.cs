// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.BusinessModel.VC;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    internal class ConfigurationChangeTracker
    {
        Configuration m_newConfig;
        Configuration m_currentActiveConfig;

        [Flags]
        public enum ConfigChangeImpactScope
        {
            None = 0,
            Session = 1,
            SessionGroup = Session << 1,
        }

        public ConfigurationChangeTracker(Configuration newConfig)
        {
            if (null == newConfig)
            {
                throw new ArgumentNullException("newConfig");
            }

            ImpactScope = ConfigChangeImpactScope.None;
            UpdatedSessionGroupId = Guid.Empty;
            UpdatedSessionIds = new List<Guid>();

            m_newConfig = newConfig;
            BusinessModelManager manager = new BusinessModelManager();
            m_currentActiveConfig = manager.LoadConfiguration(m_newConfig.SessionGroupUniqueId);
        }

        public ConfigChangeImpactScope ImpactScope
        {
            get;
            set;
        }

        public List<Guid> UpdatedSessionIds
        {
            get;
            private set;
        }

        public Guid UpdatedSessionGroupId
        {
            get;
            private set;
        }

        public void AnalyzeChangeImpact()
        {
            Debug.Assert(null != m_newConfig, "m_newConfig is NULL");
            if (null == m_currentActiveConfig)
            {
                // session group in newConfig has never been persisted
                // no need to analyze change impact
                return;
            }

            CompareSessions(m_newConfig.SessionGroup.Sessions, 
                m_currentActiveConfig.SessionGroup.Sessions);

            CompareMigrationSources(m_newConfig.SessionGroup.MigrationSources,
                m_currentActiveConfig.SessionGroup.MigrationSources);
        }

        private void SetImpactForSessionAdd()
        {
            this.ImpactScope |= ConfigChangeImpactScope.SessionGroup;
        }

        private void SetImpactForSessionChange(Guid sessionUniqueId)
        {
            this.ImpactScope |= ConfigChangeImpactScope.Session;
            if (!UpdatedSessionIds.Contains(sessionUniqueId))
            {
                UpdatedSessionIds.Add(sessionUniqueId);
            }
        }

        private void CompareMigrationSources(
            MigrationSourcesElement newMigrationSources,
            MigrationSourcesElement currMigrationSources)
        {
            foreach (var newMS in newMigrationSources.MigrationSource)
            {
                foreach (var currMS in currMigrationSources.MigrationSource)
                {
                    if (!string.IsNullOrEmpty(newMS.InternalUniqueId)
                        && !string.IsNullOrEmpty(currMS.InternalUniqueId)
                        && newMS.InternalUniqueId.Equals(currMS.InternalUniqueId, StringComparison.OrdinalIgnoreCase))
                    {
                        CompareAddins(newMS, newMS.Settings.Addins, currMS.Settings.Addins);
                    }
                }
            }
        }

        private void CompareAddins(MigrationSource newMS, SettingsAddins newMSAddins, SettingsAddins currMSAddins)
        {
            if (newMSAddins.Addin.Count != currMSAddins.Addin.Count)
            {
                Guid sessionUniqueId;
                if (!TryGetSessionUniqueId(m_newConfig, newMS, out sessionUniqueId))
                {
                    return;
                }
                else
                {
                    SetImpactForSessionChange(sessionUniqueId);
                    return;
                }
            }

            foreach (var newAddin in newMSAddins.Addin)
            {
                bool matching = false;
                foreach (var currAddin in currMSAddins.Addin)
                {
                    if (newAddin.ReferenceName.Equals(currAddin.ReferenceName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (AddinCustomSettingMatches(newAddin, currAddin))
                        {
                            matching = true;
                        }

                        break;
                    }
                }

                if (!matching)
                {
                    Guid sessionUniqueId;
                    if (!TryGetSessionUniqueId(m_newConfig, newMS, out sessionUniqueId))
                    {
                        return;
                    }
                    else
                    {
                        SetImpactForSessionChange(sessionUniqueId);
                        return;
                    }
                }
            }
        }

        private bool TryGetSessionUniqueId(Configuration config, MigrationSource migrationSource, out Guid sessionUniqueId)
        {
            sessionUniqueId = Guid.Empty;

            try
            {
                Guid migrationSourceUniqueId = new Guid(migrationSource.InternalUniqueId);

                foreach (var session in config.SessionGroup.Sessions.Session)
                {
                    Guid leftSource = new Guid(session.LeftMigrationSourceUniqueId);
                    Guid rightSource = new Guid(session.RightMigrationSourceUniqueId);

                    if (migrationSourceUniqueId.Equals(leftSource) || migrationSourceUniqueId.Equals(rightSource))
                    {
                        sessionUniqueId = session.SessionUniqueIdGuid;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool AddinCustomSettingMatches(AddinElement newAddin, AddinElement currAddin)
        {
            if (newAddin.CustomSettings.CustomSetting.Count != currAddin.CustomSettings.CustomSetting.Count)
            {
                return false;
            }

            foreach (var newSetting in newAddin.CustomSettings.CustomSetting)
            {
                bool settingMatches = false;
                foreach (var currSetting in currAddin.CustomSettings.CustomSetting)
                {
                    if (newSetting.SettingKey.Equals(currSetting.SettingKey, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!newSetting.SettingValue.Equals(currSetting.SettingValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                        else
                        {
                            settingMatches = true;
                            break;
                        }
                    }
                }

                if (!settingMatches)
                {
                    return false;
                }
            }

            return true;
        }

        private void CompareSessions(
            SessionsElement newConfigSessions,
            SessionsElement currentConfigSessions)
        {
            if (newConfigSessions.Session.Count > currentConfigSessions.Session.Count)
            {
                // a session is added
                SetImpactForSessionAdd();
                return;
            }

            foreach (var sessionInNewConfig in newConfigSessions.Session)
            {
                Session sessionInCurrentConfig = currentConfigSessions[sessionInNewConfig.SessionUniqueIdGuid];
                if (null == sessionInCurrentConfig)
                {
                    // the session in new config is a newly added one
                    SetImpactForSessionAdd();

                    // a session group level impact is set above - no need to proceed
                    return;
                }
            }

            foreach (var sessionInNewConfig in newConfigSessions.Session)
            {
                Session sessionInCurrentConfig = currentConfigSessions[sessionInNewConfig.SessionUniqueIdGuid];
                Debug.Assert(null != sessionInCurrentConfig, "sessionInCurrentConfig is NULL");
                CompareSession(sessionInNewConfig, sessionInCurrentConfig);
            }
        }
        
        /// <summary>
        /// Compares the session configs
        /// </summary>
        /// <param name="sessionInNewConfig"></param>
        /// <param name="sessionInCurrentConfig"></param>
        private void CompareSession(Session sessionInNewConfig, Session sessionInCurrentConfig)
        {
            Debug.Assert(sessionInNewConfig.SessionType == sessionInCurrentConfig.SessionType, "Session types do not match");

            CompareSessionFilters(sessionInCurrentConfig.SessionUniqueIdGuid, sessionInNewConfig.Filters, sessionInCurrentConfig.Filters);

            switch (sessionInNewConfig.SessionType)
            {
                case SessionTypeEnum.VersionControl:
                    CompareVCSessionSetting(sessionInCurrentConfig.SessionUniqueIdGuid, sessionInNewConfig.VCCustomSetting, sessionInCurrentConfig.VCCustomSetting);
                    break;
                case SessionTypeEnum.WorkItemTracking:
                    CompareWITSessionSetting(sessionInCurrentConfig.SessionUniqueIdGuid, sessionInNewConfig.WITCustomSetting, sessionInCurrentConfig.WITCustomSetting);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Compare the filters in the two versions of the configuration - raise session-level impact if there is mismatch
        /// </summary>
        /// <param name="newFiltersElement"></param>
        /// <param name="currFiltersElement"></param>
        private void CompareSessionFilters(
            Guid sessionUniqueId,
            FiltersElement newFiltersElement, 
            FiltersElement currFiltersElement)
        {
            if (newFiltersElement.FilterPair.Count != currFiltersElement.FilterPair.Count)
            {
                // a filter pair has been added/deleted
                SetImpactForSessionChange(sessionUniqueId);
                return;
            }

            foreach (var newFilterPair in newFiltersElement.FilterPair)
            {
                bool filterPairFound = false;
                FilterPair firstFoundFilterPair = null;
                foreach (var filter in newFilterPair.FilterItem)
                {
                    var filterQuery = currFiltersElement.FilterPair.Where(p =>
                        p.Neglect == newFilterPair.Neglect
                        && p.FilterItem.Where(f =>
                            f.FilterString == filter.FilterString
                            && f.MergeScope == filter.MergeScope
                            && f.MigrationSourceUniqueId == filter.MigrationSourceUniqueId
                            && f.PeerSnapshotStartPoint == filter.PeerSnapshotStartPoint
                            && f.SnapshotStartPoint == filter.SnapshotStartPoint).Count() > 0);

                    if (filterQuery.Count() == 0)
                    {
                        // a filter has been added or updated, i.e. new filter does not exist in current filter
                        break;
                    }

                    Debug.Assert(filterQuery.Count() == 1, "identical filter is found");
                    if (null == firstFoundFilterPair)
                    {
                        firstFoundFilterPair = filterQuery.First();
                    }
                    else
                    {
                        if (firstFoundFilterPair != filterQuery.First())
                        {
                            // a filter pair has been updated
                            break;
                        }
                        else
                        {
                            filterPairFound = true;
                            break;
                        }
                    }
                }

                if (!filterPairFound)
                {
                    SetImpactForSessionChange(sessionUniqueId);
                    break;
                }
            }
        }

        /// <summary>
        /// Compares WIT session custom settings - raise session-level impact when there is mismatch
        /// </summary>
        /// <param name="newWITSessionCustomSetting"></param>
        /// <param name="currWITSessionCustomSetting"></param>
        private void CompareWITSessionSetting(
            Guid sessionUniqueId,
            WITSessionCustomSetting newWITSessionCustomSetting,
            WITSessionCustomSetting currWITSessionCustomSetting)
        {
            CompareWITMappings(sessionUniqueId, newWITSessionCustomSetting.WorkItemTypes, currWITSessionCustomSetting.WorkItemTypes);

            CompareFieldMappings(sessionUniqueId, newWITSessionCustomSetting.FieldMaps, currWITSessionCustomSetting.FieldMaps);

            CompareValueMappings(sessionUniqueId, newWITSessionCustomSetting.ValueMaps, currWITSessionCustomSetting.ValueMaps);
        }

        private void CompareWITMappings(Guid sessionUniqueId, WorkItemTypes newWorkItemTypes, WorkItemTypes currWorkItemTypes)
        {
            if (newWorkItemTypes.WorkItemType.Count != currWorkItemTypes.WorkItemType.Count)
            {
                SetImpactForSessionChange(sessionUniqueId);
                return;
            }

            bool witMappingFound = true;
            foreach (var newWITMapping in newWorkItemTypes.WorkItemType)
            {
                var query = currWorkItemTypes.WorkItemType.Where(w =>
                    w.fieldMap == newWITMapping.fieldMap
                    && w.LeftWorkItemTypeName == newWITMapping.LeftWorkItemTypeName
                    && w.RightWorkItemTypeName == newWITMapping.RightWorkItemTypeName);

                if (query.Count() == 0)
                {
                    witMappingFound = false;
                    break;
                }
            }

            if (!witMappingFound)
            {
                SetImpactForSessionChange(sessionUniqueId);
            }
        }

        private void CompareFieldMappings(Guid sessionUniqueId, FieldMaps newFieldMaps, FieldMaps currFieldMaps)
        {
            if (newFieldMaps.FieldMap.Count != currFieldMaps.FieldMap.Count)
            {
                SetImpactForSessionChange(sessionUniqueId);
                return;
            }

            bool fieldMappingFound = true;
            foreach (var fieldMap in newFieldMaps.FieldMap)
            {
                var query = currFieldMaps.FieldMap.Where(f =>
                    f.name == fieldMap.name
                    && f.AggregatedFields.FieldsAggregationGroup.Count == fieldMap.AggregatedFields.FieldsAggregationGroup.Count
                    && f.MappedFields.MappedField.Count == fieldMap.MappedFields.MappedField.Count);

                if (query.Count() == 0)
                {
                    fieldMappingFound = false;
                    break;
                }

                if (!CompareAggregatedFields(fieldMap.AggregatedFields, query.First().AggregatedFields)
                    || !CompareMappedFields(fieldMap.MappedFields, query.First().MappedFields))
                {
                    fieldMappingFound = false;
                    break;
                }
            }

            if (!fieldMappingFound)
            {
                SetImpactForSessionChange(sessionUniqueId);
            }
        }

        private bool CompareMappedFields(MappedFields newMappedFields, MappedFields currMappedFields)
        {
            foreach (MappedField newField in newMappedFields.MappedField)
            {
                var query = currMappedFields.MappedField.Where(f =>
                    f.LeftName == newField.LeftName && f.MapFromSide == newField.MapFromSide
                    && f.RightName == newField.RightName && f.valueMap == newField.valueMap);

                if (query.Count() == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareAggregatedFields(AggregatedFields newAggregatedFields, AggregatedFields currAggregatedFields)
        {
            foreach (FieldsAggregationGroup newGroup in newAggregatedFields.FieldsAggregationGroup)
            {
                var query = currAggregatedFields.FieldsAggregationGroup.Where(g =>
                    g.Format == newGroup.Format && g.MapFromSide == newGroup.MapFromSide
                    && g.SourceField.Count == newGroup.SourceField.Count
                    && g.TargetFieldName == newGroup.TargetFieldName);

                if (query.Count() == 0)
                {
                    return false;
                }

                Debug.Assert(query.Count() == 1, "query.Count() != 1");
                FieldsAggregationGroup currGroup = query.First();
                foreach (var sourceField in newGroup.SourceField)            
                {
                    var sourceFldQuery = currGroup.SourceField.Where(f =>
                        f.Index == sourceField.Index
                        && f.SourceFieldName == sourceField.SourceFieldName
                        && f.valueMap == sourceField.valueMap);
                    if (sourceFldQuery.Count() == 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void CompareValueMappings(Guid sessionUniqueId, ValueMaps newValueMaps, ValueMaps currValueMaps)
        {
            if (newValueMaps.ValueMap.Count != currValueMaps.ValueMap.Count)
            {
                SetImpactForSessionChange(sessionUniqueId);
                return;
            }

            bool mappingsFound = true;
            foreach (var valueMap in newValueMaps.ValueMap)
            {
                var mapQuery = currValueMaps.ValueMap.Where(vm =>
                    vm.name == valueMap.name && vm.Value.Count == valueMap.Value.Count);
                if (mapQuery.Count() == 0)
                {
                    mappingsFound = false;
                    break;
                }

                Debug.Assert(mapQuery.Count() == 1, "mapQuery.Count() != 1");
                var currMap = mapQuery.First();
                foreach (var newValue in valueMap.Value)
                {
                    var valueQuery = currMap.Value.Where(v =>
                        v.LeftValue == newValue.LeftValue && v.RightValue == newValue.RightValue
                        && v.When.ConditionalSrcFieldName == newValue.When.ConditionalSrcFieldName
                        && v.When.ConditionalSrcFieldValue == newValue.When.ConditionalSrcFieldValue);

                    if (valueQuery.Count() == 0)
                    {
                        mappingsFound = false;
                        break;
                    }
                }

                if (!mappingsFound) break;
            }

            if (!mappingsFound)
            {
                SetImpactForSessionChange(sessionUniqueId);
            }
        }

        /// <summary>
        /// Compares VC session custom settings - currently the changes are considered transient, i.e. no impact
        /// </summary>
        /// <param name="newVCSessionCustomSetting"></param>
        /// <param name="currVCSessionCustomSetting"></param>
        private void CompareVCSessionSetting(
            Guid sessionUniqueId,
            VCSessionCustomSetting newVCSessionCustomSetting,
            VCSessionCustomSetting currVCSessionCustomSetting)
        {
            if (newVCSessionCustomSetting.BranchSettings.BranchSetting.Count != currVCSessionCustomSetting.BranchSettings.BranchSetting.Count
                || newVCSessionCustomSetting.Settings.Setting.Count != currVCSessionCustomSetting.Settings.Setting.Count)
            {
                SetImpactForSessionChange(sessionUniqueId);
                return;
            }
            
            foreach (var newSetting in newVCSessionCustomSetting.BranchSettings.BranchSetting)
            {
                bool matching = false;
                foreach (var currSetting in currVCSessionCustomSetting.BranchSettings.BranchSetting)
                {
                    if (newSetting.SourceBranch.Equals(currSetting.SourceBranch, StringComparison.OrdinalIgnoreCase)
                        && newSetting.SourceId.Equals(currSetting.SourceId, StringComparison.OrdinalIgnoreCase)
                        && newSetting.TargetBranch.Equals(currSetting.TargetBranch, StringComparison.OrdinalIgnoreCase))
                    {
                        matching = true;
                        break;
                    }
                }

                if (!matching)
                {
                    SetImpactForSessionChange(sessionUniqueId);
                    return;
                }
            }

            foreach (var newSetting in newVCSessionCustomSetting.Settings.Setting)
            {
                bool matching = false;
                foreach (var currSetting in currVCSessionCustomSetting.Settings.Setting)
                {
                    if (newSetting.SettingKey.Equals(currSetting.SettingKey, StringComparison.OrdinalIgnoreCase)
                        && newSetting.SettingValue.Equals(currSetting.SettingValue, StringComparison.OrdinalIgnoreCase))
                    {
                        matching = true;
                        break;
                    }
                }

                if (!matching)
                {
                    SetImpactForSessionChange(sessionUniqueId);
                    return;
                }
            }
        }
    }
}
