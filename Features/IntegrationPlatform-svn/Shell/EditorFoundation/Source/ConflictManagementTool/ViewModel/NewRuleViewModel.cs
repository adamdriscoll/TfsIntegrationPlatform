// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public class NewRuleViewModel : RuleViewModelBase
    {
        private ExistingRuleViewModel m_basedOnRule;

        public NewRuleViewModel(ApplicationViewModel appViewModel, ExistingRuleViewModel basedOnRule)
            : base(appViewModel)
        {
            m_basedOnRule = basedOnRule;
            SessionGroupUniqueId = appViewModel.SessionGroupUniqueId;
            
            if (m_basedOnRule != null)
            {
                SelectedSession = Sessions.FirstOrDefault(x => x.UniqueId.Equals(m_basedOnRule.ScopeId));
                SelectedSource = Sources.FirstOrDefault(x => x.UniqueId.Equals(m_basedOnRule.SourceId));
                SelectedConflictType = ConflictTypes.FirstOrDefault(x => x.ReferenceName.Equals(m_basedOnRule.ConflictType.ReferenceName));
                Scope = m_basedOnRule.Scope;
                Description = m_basedOnRule.Description;
                SelectedResolutionAction = ResolutionActions.FirstOrDefault(x => x.ReferenceName.Equals(m_basedOnRule.ResolutionAction.ReferenceName));
            }
            else
            {
                SelectedSession = Sessions.FirstOrDefault();
                Scope = string.Empty;
            }
        }

        public Guid SessionGroupUniqueId { get; private set; }

        public IEnumerable<LightWeightSession> Sessions
        {
            get
            {
                if (m_sessions == null)
                {
                    m_sessions = new List<LightWeightSession>();
                    foreach (Session session in m_appViewModel.Config.SessionGroup.Sessions.Session)
                    {
                        m_sessions.Add(new LightWeightSession(session));
                    }
                }

                return m_sessions;
            }
        }
        public override Guid SourceId
        {
            get
            {
                return SelectedSource.UniqueId;
            }
        }
        public LightWeightSession SelectedSession
        {
            get
            {
                return m_selectedSession;
            }
            set
            {
                m_selectedSession = value;

                SelectedSource = Sources.First();
                OnPropertyChanged("SelectedSource");
                OnPropertyChanged("Sources");
            }
        }

        public IEnumerable<LightWeightSource> Sources
        {
            get
            {
                m_sources = SelectedSession.Sources;
                return m_sources;
            }
        }

        public LightWeightSource SelectedSource
        {
            get
            {
                return m_selectedSource;
            }
            set
            {
                m_selectedSource = value;

                m_conflictTypes = null;

                if (m_selectedConflictType != null)
                {
                    ResolutionAction selectedResolutionAction = m_selectedResolutionAction;
                    ConflictType selectedConflictType = ConflictTypes.FirstOrDefault(x => x.ReferenceName.Equals(m_selectedConflictType.ReferenceName));
                    if (selectedConflictType != null && selectedResolutionAction != null)
                    {
                        SelectedConflictType = selectedConflictType;
                        SelectedResolutionAction = ResolutionActions.First(x => x.ReferenceName.Equals(selectedResolutionAction.ReferenceName));
                        OnPropertyChanged("SelectedResolutionAction");
                    }
                    else
                    {
                        SelectedConflictType = ConflictTypes.FirstOrDefault();
                        OnPropertyChanged("SelectedConflictType");
                        OnPropertyChanged("ConflictTypes");
                    }
                }
                else
                {
                    SelectedConflictType = ConflictTypes.FirstOrDefault();
                    OnPropertyChanged("SelectedConflictType");
                    OnPropertyChanged("ConflictTypes");
                }
            }
        }

        private static readonly Guid[] s_omittedConflictTypes = new Guid[]
        {
            new GenericConflictType().ReferenceName,
            new ChainOnBackloggedItemConflictType().ReferenceName,
            Constants.witGeneralConflictTypeRefName,
            Constants.witInsufficientPermissionConflictTypeRefName,
            Constants.workItemTypeNotExistConflictTypeRefName
        };

        public IEnumerable<ConflictType> ConflictTypes
        {
            get
            {
                if (m_conflictTypes == null)
                {
                    m_conflictTypes = new HashSet<ConflictType>(new ConflictTypeComparer());
                    IEnumerable<ConflictManager> conflictManagers = m_appViewModel.Sync.GetConflictManagers(SelectedSession.UniqueId, SelectedSource.UniqueId);
                    foreach (var manager in conflictManagers)
                    {
                        foreach (ConflictType conflictType in manager.RegisteredConflictTypes.Values)
                        {
                            if (!s_omittedConflictTypes.Contains(conflictType.ReferenceName))
                            {
                                m_conflictTypes.Add(conflictType);
                            }
                        }
                    }
                }
                return m_conflictTypes;
            }
        }
        
        public ConflictType SelectedConflictType
        {
            get
            {
                return m_selectedConflictType;
            }
            set
            {
                m_selectedConflictType = value;
                if (SelectedConflictType != null)
                {
                    SetConflictManager(SelectedSession.UniqueId, SelectedSource.UniqueId, SelectedConflictType.ReferenceName);
                }
                else
                {
                    m_conflictManager = null;
                    ConflictType = null;
                }
            }
        }

        public override bool CanSave
        {
            get
            {
                return base.CanSave && SelectedSource != null && ConflictType != null && SelectedResolutionAction != null;
            }
        }

        public IEnumerable<ConflictResolutionResult> Save()
        {
            if (CanSave)
            {
                ConflictResolutionRule newRule = SelectedResolutionAction.NewRule(Scope, Description, DataFields.ToDictionary(x => x.FieldName, x => x.FieldValue));
                int newRuleId = m_conflictManager.SaveNewResolutionRule(ConflictType, newRule);

                newRule.InternalId = newRuleId;
                try
                {
                    // try to resolve existing conflicts
                    IEnumerable<ConflictResolutionResult> results = m_conflictManager.ResolveExistingConflictWithExistingRule(newRule);

                    m_appViewModel.SetResolvedConflicts(results, newRule.InternalId);

                    // get new rule from db
                    RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                    var v = from r in context.RTResolutionRuleSet
                            where r.Id == newRuleId
                            select r;
                    if (v.Count() > 0)
                    {
                        m_appViewModel.Rules.Insert(0, new ExistingRuleViewModel(v.First(), m_appViewModel));
                    }

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

        private IEnumerable<LightWeightSource> m_sources;
        private LightWeightSource m_selectedSource;
        private List<LightWeightSession> m_sessions;
        private LightWeightSession m_selectedSession;
        private HashSet<ConflictType> m_conflictTypes;
        private ConflictType m_selectedConflictType;

        private class ConflictTypeComparer : IEqualityComparer<ConflictType>
        {
            #region IEqualityComparer<ConflictType> Members

            public bool Equals(ConflictType x, ConflictType y)
            {
                return x.ReferenceName.Equals(y.ReferenceName);
            }

            public int GetHashCode(ConflictType obj)
            {
                return obj.ReferenceName.GetHashCode();
            }

            #endregion
        }
    }

    public class LightWeightSource
    {
        public LightWeightSource()
        {
            FriendlyName = "All";
            UniqueId = Guid.Empty;
        }

        public LightWeightSource(Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource source)
        {
            FriendlyName = source.FriendlyName;
            UniqueId = new Guid(source.InternalUniqueId);
        }

        public LightWeightSource(Microsoft.TeamFoundation.Migration.EntityModel.MigrationSource source)
        {
            FriendlyName = source.FriendlyName;
            UniqueId = source.UniqueId;
        }

        public LightWeightSource(RTMigrationSource source)
        {
            FriendlyName = source.FriendlyName;
            UniqueId = source.UniqueId;
        }

        public string FriendlyName { get; private set; }
        public Guid UniqueId { get; private set; }
    }
    
    public class LightWeightSession
    {
        private LightWeightSession(string friendlyName, Guid uniqueId)
        {
            FriendlyName = friendlyName;
            UniqueId = uniqueId;
        }
        public LightWeightSession(Session session)
        {
            List<LightWeightSource> sources = new List<LightWeightSource>();
            foreach (Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource source in session.MigrationSources.Select(x => x.Value))
            {
                sources.Add(new LightWeightSource(source));
            }
            Sources = sources;
            FriendlyName = session.FriendlyName;
            UniqueId = new Guid(session.SessionUniqueId);
        }

        public IEnumerable<LightWeightSource> Sources { get; private set; }
        public string FriendlyName { get; private set; }
        public Guid UniqueId { get; private set; }
    }
}
