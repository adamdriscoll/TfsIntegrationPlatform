// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public class ExistingRuleViewModel : RuleViewModelBase
    {
        private static Dictionary<int, RTConflictType> s_conflictTypeTable = new Dictionary<int, RTConflictType>();
        private static Dictionary<int, RTResolutionAction> s_resolutionActionTable = new Dictionary<int, RTResolutionAction>();
        private static Dictionary<Guid, RTMigrationSource> s_migrationSourceTable = new Dictionary<Guid, RTMigrationSource>();
        private static Dictionary<int, string> s_scopeTable;
        static ExistingRuleViewModel()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                s_scopeTable = context.RTConflictRuleScopeSet.ToDictionary(x => x.Id, x => x.Scope);
            }
        }

        public ExistingRuleViewModel(RTResolutionRule rule, ApplicationViewModel appViewModel)
            : base(appViewModel)
        {
            m_rule = rule;

            int conflictTypeId = (int)m_rule.ConflictTypeReference.EntityKey.EntityKeyValues.First().Value;
            if (!s_conflictTypeTable.ContainsKey(conflictTypeId))
            {
                lock (s_conflictTypeTable)
                {
                    if (!s_conflictTypeTable.ContainsKey(conflictTypeId))
                    {
                        m_rule.ConflictTypeReference.Load();
                        s_conflictTypeTable.Add(conflictTypeId, m_rule.ConflictType);
                    }
                }
            }

            if (!s_migrationSourceTable.ContainsKey(m_rule.SourceInfoUniqueId))
            {
                lock (s_migrationSourceTable)
                {
                    if (!s_migrationSourceTable.ContainsKey(m_rule.SourceInfoUniqueId))
                    {
                        RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                        var v = from s in context.RTMigrationSourceSet
                                where s.UniqueId.Equals(m_rule.SourceInfoUniqueId)
                                select s;
                        s_migrationSourceTable.Add(m_rule.SourceInfoUniqueId, v.First());
                    }
                }
            }
            m_migrationSource = s_migrationSourceTable[m_rule.SourceInfoUniqueId];

            int resolutionActionId = (int)m_rule.ResolutionActionReference.EntityKey.EntityKeyValues.First().Value;
            if (!s_resolutionActionTable.ContainsKey(resolutionActionId))
            {
                lock (s_resolutionActionTable)
                {
                    if (!s_resolutionActionTable.ContainsKey(resolutionActionId))
                    {
                        m_rule.ResolutionActionReference.Load();
                        s_resolutionActionTable.Add(resolutionActionId, m_rule.ResolutionAction);
                    }
                }
            }
            ResolutionAction = s_resolutionActionTable[resolutionActionId];

            SetConflictManager(m_rule.ScopeInfoUniqueId, m_rule.SourceInfoUniqueId, s_conflictTypeTable[conflictTypeId].ReferenceName);

            int scopeId = (int)m_rule.ScopeReference.EntityKey.EntityKeyValues.First().Value;
            if (!s_scopeTable.ContainsKey(scopeId))
            {
                lock (s_scopeTable)
                {
                    if (!s_scopeTable.ContainsKey(scopeId))
                    {
                        m_rule.ScopeReference.Load();
                        s_scopeTable.Add(scopeId, m_rule.Scope.Scope);
                    }
                }
            }
            Scope = s_scopeTable[scopeId];
        }

        public int Id
        {
            get
            {
                return m_rule.Id;
            }
        }

        private RTMigrationSource m_migrationSource;
        public Guid ScopeId
        {
            get
            {
                return m_rule.ScopeInfoUniqueId;
            }
        }
        public override Guid SourceId
        {
            get
            {
                return m_rule.SourceInfoUniqueId;
            }
        }
        public string MigrationSource
        {
            get
            {
                return m_migrationSource.FriendlyName;
            }
        }
        public DateTime CreationTime
        {
            get
            {
                return m_rule.CreationTime;
            }
        }


        public override ResolutionAction SelectedResolutionAction
        {
            get
            {
                if (m_selectedResolutionAction == null)
                {
                    m_rule.ResolutionActionReference.Load();
                    SelectedResolutionAction = ResolutionActions.Where(x => x.ReferenceName.Equals(m_rule.ResolutionAction.ReferenceName)).Single();
                }
                return m_selectedResolutionAction;
            }
            set
            {
                base.SelectedResolutionAction = value;
                SetDataFields();
                
                OnPropertyChanged("CanSave");
            }
        }

        private void SetDataFields()
        {
            ConfliceResolutionRuleSerializer ruleSerializer = new ConfliceResolutionRuleSerializer();
            ConflictResolutionRule crRule = ruleSerializer.Deserialize(m_rule.RuleData);

            foreach (ObservableDataField dataField in ObservableDataFields)
            {
                if (crRule.DataField != null)
                {
                    dataField.DefaultFieldValue = crRule.DataFieldDictionary[dataField.FieldName];
                }
            }
        }

        public override string Description
        {
            get
            {
                if (base.Description == null)
                {
                    ConfliceResolutionRuleSerializer ruleSerializer = new ConfliceResolutionRuleSerializer();
                    ConflictResolutionRule crRule = ruleSerializer.Deserialize(m_rule.RuleData);
                    if (crRule.RuleDescription == null)
                    {
                        base.Description = string.Empty;
                    }
                    else
                    {
                        base.Description = crRule.RuleDescription;
                    }
                }
                return base.Description;
            }
            set
            {
                base.Description = value;
                OnPropertyChanged("CanSave");
            }
        }

        public RTResolutionAction ResolutionAction { get; private set; }

        public bool IsChanged
        {
            get
            {
                if (m_rule.Scope == null)
                {
                    m_rule.ScopeReference.Load();
                }
                return !(m_rule.Scope.Scope.Equals(Scope) && m_rule.ResolutionAction.ReferenceName.Equals(SelectedResolutionAction.ReferenceName) && !DataFieldsIsChanged);
            }
        }

        public override bool CanSave
        {
            get
            {
                return IsChanged && base.CanSave;
            }
        }

        public ICollection<RTConflict> ResolvedConflicts
        {
            get
            {
                RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                return context.RTConflictSet.Where(x => x.ResolvedByRule.Id == m_rule.Id).ToList();
            }
        }

        public void RemoveFromDB()
        {
            ConfliceResolutionRuleSerializer ruleSerializer = new ConfliceResolutionRuleSerializer();
            ConflictResolutionRule crRule = ruleSerializer.Deserialize(m_rule.RuleData);

            ConflictManagementServiceProxy.ResolutionRule.ObsoleteResolutionRule(crRule);
        }

        public IEnumerable<ConflictResolutionResult> SaveToDB()
        {
            if (CanSave)
            {
                // create new rule
                ConflictResolutionRule newRule = SelectedResolutionAction.NewRule(Scope, Description, DataFields.ToDictionary(x => x.FieldName, x => x.FieldValue));

                // save new rule
                int newRuleId = m_conflictManager.SaveNewResolutionRule(ConflictType, newRule);
                newRule.InternalId = newRuleId;
                try
                {
                    // try to resolve existing conflicts
                    IEnumerable<ConflictResolutionResult> results = m_conflictManager.ResolveExistingConflictWithExistingRule(newRule);

                    // remove old rule from db
                    RemoveFromDB();

                    // get new rule from db
                    RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                    var v = from r in context.RTResolutionRuleSet
                            where r.Id == newRuleId
                            select r;
                    m_rule = v.First();

                    SetDataFields();
                    OnPropertyChanged("CreationTime");
                    OnPropertyChanged("ResolutionAction");
                    OnPropertyChanged("Scope");
                    OnPropertyChanged("CanSave");

                    m_appViewModel.SetResolvedConflicts(results, newRule.InternalId);

                    return results;
                }
                catch (ConflictManagementGeneralException)
                {
                    throw new Exception("Could not save rule.");
                }
            }
            else
            {
                throw new Exception("Invalid scope");
            }
        }

        private RTResolutionRule m_rule;
    }
}
