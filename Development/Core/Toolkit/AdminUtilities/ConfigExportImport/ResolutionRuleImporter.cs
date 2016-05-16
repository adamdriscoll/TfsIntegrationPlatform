// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class ResolutionRuleImporter
    {
        Dictionary<Guid, Guid> m_oldRuleRefNameToNewOne = new Dictionary<Guid, Guid>();
        Dictionary<int, ConfigConflictResolutionRuleScope> m_oldStorageIdToNewEdmScope = 
            new Dictionary<int, ConfigConflictResolutionRuleScope>();
        Dictionary<int, ConfigConflictResolutionAction> m_oldStorageIdToNewEdmAction =
            new Dictionary<int, ConfigConflictResolutionAction>();
        Dictionary<int, Provider> m_oldStorageIdToNewEdmProvider =
            new Dictionary<int, Provider>();
        Dictionary<int, ConfigConflictType> m_oldStorageIdToNewEdmConflictType =
            new Dictionary<int, ConfigConflictType>();

        TfsMigrationConsolidatedDBEntities m_context = TfsMigrationConsolidatedDBEntities.CreateInstance();

        public void Import(SerializableConflictResolutionRuleCollection ruleCollection)
        {
            Debug.Assert(null != ruleCollection, "ruleCollection is NULL");
            foreach (var rule in ruleCollection.Rules)
            {
                ImportRule(rule);
            }
        }

        private void ImportRule(SerializableConflictResolutionRule rule)
        {
            if (!m_oldRuleRefNameToNewOne.ContainsKey(rule.ReferenceName))
            {
                ConfigConflictType edmConflictType = ImportConflictType(rule.ConflictType);
                ConfigConflictResolutionAction edmResolutionAction = ImportResolutionAction(rule.ResolutionAction);
                ConfigConflictResolutionRuleScope edmRuleScope = ImportScope(rule.Scope);

                Guid newRuleRefName = Guid.NewGuid();
                string newRuleDataXmlDocString = rule.RuleDataXmlDocString.Replace(rule.ReferenceName.ToString(), newRuleRefName.ToString());
                ConfigConflictResolutionRule edmRule = ConfigConflictResolutionRule.CreateConfigConflictResolutionRule(
                    0, newRuleRefName, rule.ScopeInfoUniqueId, rule.SourceInfoUniqueId, newRuleDataXmlDocString, DateTime.Now, rule.Status);
                m_context.AddToConfigConflictResolutionRuleSet(edmRule);

                edmRule.ConflictType = edmConflictType;
                edmRule.ResolutionAction = edmResolutionAction;
                edmRule.RuleScope = edmRuleScope;

                m_context.TrySaveChanges();

                m_oldRuleRefNameToNewOne.Add(rule.ReferenceName, newRuleRefName);
            }
        }

        private ConfigConflictResolutionRuleScope ImportScope(SerializableResolutionRuleScope serializableResolutionRuleScope)
        {
            if (m_oldStorageIdToNewEdmScope.ContainsKey(serializableResolutionRuleScope.StorageId))
            {
                return m_oldStorageIdToNewEdmScope[serializableResolutionRuleScope.StorageId];
            }
            else
            {
                var query = from s in m_context.ConfigConflictResolutionRuleScopeSet
                            where s.Scope.Equals(serializableResolutionRuleScope.Scope)
                            select s;

                ConfigConflictResolutionRuleScope edmScope;
                if (query.Count() > 0)
                {
                    edmScope = query.First();
                }
                else
                {
                    edmScope = ConfigConflictResolutionRuleScope.CreateConfigConflictResolutionRuleScope(
                        0, serializableResolutionRuleScope.Scope);
                    m_context.AddToConfigConflictResolutionRuleScopeSet(edmScope);
                    m_context.TrySaveChanges();
                }
                m_oldStorageIdToNewEdmScope.Add(serializableResolutionRuleScope.StorageId, edmScope);
                return edmScope;
            }
        }

        private ConfigConflictResolutionAction ImportResolutionAction(SerializableResolutionAction serializableResolutionAction)
        {
            if (m_oldStorageIdToNewEdmAction.ContainsKey(serializableResolutionAction.StorageId))
            {
                return m_oldStorageIdToNewEdmAction[serializableResolutionAction.StorageId];
            }
            else
            {
                var edmProvider = ImportProvider(serializableResolutionAction.Provider);

                IQueryable<ConfigConflictResolutionAction> query;
                if (edmProvider == null)
                {
                    query = from a in m_context.ConfigConflictResolutionActionSet
                            where a.ReferenceName.Equals(serializableResolutionAction.ReferenceName)
                            && a.Provider == null
                            select a;
                }
                else
                {
                    query = from a in m_context.ConfigConflictResolutionActionSet
                            where a.ReferenceName.Equals(serializableResolutionAction.ReferenceName)
                            && a.Provider.ReferenceName.Equals(edmProvider.ReferenceName)
                            select a;
                }

                ConfigConflictResolutionAction edmAction;
                if (query.Count() > 0)
                {
                    edmAction = query.First();
                }
                else
                {
                    edmAction = ConfigConflictResolutionAction.CreateConfigConflictResolutionAction(
                       0, serializableResolutionAction.ReferenceName, serializableResolutionAction.FriendlyName);
                    edmAction.IsActive = serializableResolutionAction.IsActive;
                    edmAction.Provider = edmProvider;

                    m_context.AddToConfigConflictResolutionActionSet(edmAction);
                    m_context.TrySaveChanges();
                }

                m_oldStorageIdToNewEdmAction.Add(serializableResolutionAction.StorageId, edmAction);
                return edmAction;
            }
        }

        private Provider ImportProvider(SerializableProvider serializableProvider)
        {
            if (null == serializableProvider)
            {
                return null;
            }

            if (m_oldStorageIdToNewEdmProvider.ContainsKey(serializableProvider.StorageId))
            {
                return m_oldStorageIdToNewEdmProvider[serializableProvider.StorageId];
            }
            else
            {
                var query = from p in m_context.ProviderSet
                            where p.ProviderVersion.Equals(serializableProvider.ProviderVersion)
                            && p.ReferenceName.Equals(serializableProvider.ReferenceName)
                            select p;

                Provider edmProvider;
                if (query.Count() > 0)
                {
                    edmProvider = query.First();
                }
                else
                {
                    edmProvider = Provider.CreateProvider(0, serializableProvider.FriendlyName, serializableProvider.ReferenceName);
                    edmProvider.ProviderVersion = serializableProvider.ProviderVersion;

                    m_context.AddToProviderSet(edmProvider);
                    m_context.TrySaveChanges();
                }

                m_oldStorageIdToNewEdmProvider.Add(serializableProvider.StorageId, edmProvider);
                return edmProvider;
            }
        }

        private ConfigConflictType ImportConflictType(SerializableConflictType serializableConflictType)
        {
            if (m_oldStorageIdToNewEdmConflictType.ContainsKey(serializableConflictType.StorageId))
            {
                return m_oldStorageIdToNewEdmConflictType[serializableConflictType.StorageId];
            }
            else
            {
                Provider edmProvider = ImportProvider(serializableConflictType.Provider);

                Guid referenceName = serializableConflictType.ReferenceName;
                IQueryable<ConfigConflictType> query;                
                if (edmProvider == null)
                {
                    query = from ct in m_context.ConfigConflictTypeSet
                            where ct.Provider == null
                            && ct.ReferenceName.Equals(referenceName)
                            select ct;
                }
                else
                {
                    query = from ct in m_context.ConfigConflictTypeSet
                            where ct.Provider.ReferenceName.Equals(edmProvider.ReferenceName)
                            && ct.ReferenceName.Equals(referenceName)
                            select ct;
                }

                ConfigConflictType edmConflictType;
                if (query.Count() > 0)
                {
                    edmConflictType = query.First();
                }
                else
                {
                    edmConflictType = ConfigConflictType.CreateConfigConflictType(
                       0, serializableConflictType.ReferenceName, serializableConflictType.FriendlyName);
                    edmConflictType.DescriptionDoc = serializableConflictType.DescriptionDoc;
                    edmConflictType.IsActive = serializableConflictType.IsActive;
                    edmConflictType.Provider = edmProvider;

                    m_context.AddToConfigConflictTypeSet(edmConflictType);
                    m_context.TrySaveChanges();
                }
                m_oldStorageIdToNewEdmConflictType.Add(serializableConflictType.StorageId, edmConflictType);
                return edmConflictType;
            }
        }
    }
}
