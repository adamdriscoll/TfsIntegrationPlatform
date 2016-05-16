// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public class ConflictRuleViewModel : RuleViewModelBase
    {
        private static readonly Dictionary<int, string> TypeIconLookup = new Dictionary<int, string>
        {
            {-1, "../Resources/Images/global.png"},
            {0, "../Resources/Images/vc.png"},
            {1, "../Resources/Images/wit.png"}
        };

        private static readonly Dictionary<int, string> TypeNameLookup = new Dictionary<int, string>
        {
            {-1, "Global"},
            {0, "Version Control"},
            {1, "Work Item Tracking"}
        };

        private static Dictionary<int, RTMigrationSource> s_migrationSourceTable = new Dictionary<int, RTMigrationSource>();
        private static Dictionary<int, RTConflictType> s_conflictTypeTable = new Dictionary<int, RTConflictType>();

        private RTMigrationSource m_rtMigrationSource;

        public ConflictRuleViewModel(ConflictRuleViewModel copyFrom)
            : this(copyFrom.RTConflict, copyFrom.AppViewModel)
        {
        }

        public ApplicationViewModel AppViewModel
        {
            get
            {
                return m_appViewModel;
            }
        }

        public ConflictRuleViewModel(RTConflict conflict, ApplicationViewModel appViewModel)
            : base(appViewModel)
        {
            m_conflict = conflict;
            base.Scope = m_conflict.ScopeHint;

            int migrationSourceId = (int)m_conflict.SourceSideMigrationSourceReference.EntityKey.EntityKeyValues.First().Value;
            if (!s_migrationSourceTable.ContainsKey(migrationSourceId))
            {
                lock (s_migrationSourceTable)
                {
                    if (!s_migrationSourceTable.ContainsKey(migrationSourceId))
                    {
                        m_conflict.SourceSideMigrationSourceReference.Load();
                        s_migrationSourceTable.Add(migrationSourceId, m_conflict.SourceSideMigrationSource);
                    }
                }
            }
            m_rtMigrationSource = s_migrationSourceTable[migrationSourceId];

            int conflictTypeId = (int)m_conflict.ConflictTypeReference.EntityKey.EntityKeyValues.First().Value;
            if (!s_conflictTypeTable.ContainsKey(conflictTypeId))
            {
                lock (s_conflictTypeTable)
                {
                    if (!s_conflictTypeTable.ContainsKey(conflictTypeId))
                    {
                        m_conflict.ConflictTypeReference.Load();
                        s_conflictTypeTable.Add(conflictTypeId, m_conflict.ConflictType);
                    }
                }
            }

            SetConflictManager(m_conflict.ScopeId, m_rtMigrationSource.UniqueId, s_conflictTypeTable[conflictTypeId].ReferenceName);
        }

        private IConflictTypeUserControl m_customControl;
        public IConflictTypeUserControl CustomControl
        {
            get
            {
                if (m_customControl == null && m_appViewModel.ExtensibilityViewModel != null)
                {
                    m_customControl = m_appViewModel.ExtensibilityViewModel.GetConflictTypeUserControl(this, SourceId);
                }
                return m_customControl;
            }
        }

        public DateTime? CreationTime
        {
            get
            {
                return m_conflict.CreationTime;
            }
        }

        public override Guid SourceId
        {
            get
            {
                return m_rtMigrationSource.UniqueId;
            }
        }

        public int ConflictCount
        {
            get
            {
                return m_conflict.ConflictCount ?? 0;
            }
        }

        public int ConflictInternalId
        {
            get
            {
                return m_conflict.Id;
            }
        }

        public string ConflictTypeFriendlyName
        {
            get
            {
                if (m_appViewModel.ExtensibilityViewModel != null)
                {
                    return m_appViewModel.ExtensibilityViewModel.GetConflictTypeFriendlyName(this, SourceId);
                }
                return string.Empty;
            }
        }

        public Dictionary<string, string> ExtendedInformation
        {
            get
            {
                Dictionary<string, string> extendedInformation = new Dictionary<string, string>();
                extendedInformation["Type"] = TypeName;
                extendedInformation["Conflict Id"] = ConflictInternalId.ToString();
                if (CreationTime != null)
                {
                    extendedInformation["Creation Time"] = CreationTime.ToString();
                }
                extendedInformation["Conflict Type"] = ConflictTypeFriendlyName;
                extendedInformation["Migration Source"] = MigrationSource;
                extendedInformation["Resolved Status"] = IsResolved.ToString() + (IsResolved == ResolvedStatus.Resolved ? " by Rule " + ResolvedByRuleId : string.Empty);

                try
                {
                    m_conflict.ConflictedChangeActionReference.Load();
                    if (m_conflict.ConflictedChangeAction != null)
                    {
                        m_conflict.ConflictedChangeAction.ChangeGroupReference.Load();
                        extendedInformation["Change Action"] = m_conflict.ConflictedChangeAction.ChangeGroup.Name;
                    }

                    m_conflict.ConflictedLinkChangeActionReference.Load();
                    if (m_conflict.ConflictedLinkChangeAction != null)
                    {
                        m_conflict.ConflictedLinkChangeAction.LinkChangeGroupReference.Load();
                        extendedInformation["Link Change Action"] = m_conflict.ConflictedLinkChangeAction.LinkChangeGroup.GroupName;
                    }

                    m_conflict.ConflictedLinkChangeGroupReference.Load();
                    if (m_conflict.ConflictedLinkChangeGroup != null)
                    {
                        extendedInformation["Link Change Group"] = m_conflict.ConflictedLinkChangeGroup.GroupName;
                    }
                }
                catch
                { }
                return extendedInformation;
            }
        }

        private int? m_type;
        private static Dictionary<Guid, int> s_sessionGuid2Type;

        public int Type
        {
            get
            {
                if (m_type == null)
                {
                    if (s_sessionGuid2Type == null)
                    {
                        s_sessionGuid2Type = new Dictionary<Guid, int>();
                        RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                        IEnumerable<RTSessionConfig> sessionConfigs = context.RTSessionConfigSet;
                        foreach (RTSessionConfig sessionConfig in sessionConfigs)
                        {
                            s_sessionGuid2Type[sessionConfig.SessionUniqueId] = sessionConfig.Type;
                        }
                    }

                    if (s_sessionGuid2Type.ContainsKey(m_conflict.ScopeId))
                    {
                        m_type = s_sessionGuid2Type[m_conflict.ScopeId];
                    }
                    else
                    {
                        m_type = -1;
                    }
                }
                return (int)m_type;
            }
        }

        public string TypeName
        {
            get
            {
                return TypeNameLookup[Type];
            }
        }

        public string TypeIcon
        {
            get
            {
                return TypeIconLookup[Type];
            }
        }

        public string MigrationSource
        {
            get
            {
                return m_rtMigrationSource.FriendlyName;
            }
        }

        public static Dictionary<Guid, string> s_otherMigrationSourceLookup;

        private string m_migrationOther;
        public string MigrationOther
        {
            get
            {
                if (m_migrationOther == null)
                {
                    if (s_otherMigrationSourceLookup == null)
                    {
                        s_otherMigrationSourceLookup = new Dictionary<Guid, string>();
                        RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                        foreach (RTSession session in context.RTSessionSet.Where(x=>x.SessionGroup.GroupUniqueId.Equals(m_appViewModel.SessionGroupUniqueId)))
                        {
                            session.LeftMigrationSourceReference.Load();
                            session.RightMigrationSourceReference.Load();
                            s_otherMigrationSourceLookup[session.LeftMigrationSource.UniqueId] = session.RightMigrationSource.FriendlyName;
                            s_otherMigrationSourceLookup[session.RightMigrationSource.UniqueId] = session.LeftMigrationSource.FriendlyName;
                        }
                    }
                    if (s_otherMigrationSourceLookup.ContainsKey(m_rtMigrationSource.UniqueId))
                    {
                        m_migrationOther = s_otherMigrationSourceLookup[m_rtMigrationSource.UniqueId];
                    }
                    else
                    {
                        m_migrationOther = string.Empty;
                    }
                }
                return m_migrationOther;
            }
        }

        public MigrationConflict MigrationConflict
        {
            get
            {
                if (m_migrationConflict == null)
                {
                    m_migrationConflict = new MigrationConflict(ConflictType, MigrationConflict.Status.Unresolved, m_conflict.ConflictDetails, m_conflict.ScopeHint);
                }
                return m_migrationConflict;
            }
        }

        public bool IsResolvable
        {
            get
            {
                return m_isResolvable && IsResolved != ResolvedStatus.Resolved;
            }
            set
            {
                if (m_isResolvable != value)
                {
                    m_isResolvable = value;
                    OnPropertyChanged("IsResolvable");
                }
            }
        }

        public string ConflictDetails
        {
            get
            {
                return ConflictType.TranslateConflictDetailsToReadableDescription(m_conflict.ConflictDetails);
            }
        }

        private ResolvedStatus m_isResolved;
        public ResolvedStatus IsResolved
        {
            get
            {
                return m_isResolved;
            }
            set
            {
                m_isResolved = value;
                OnPropertyChanged("CanSave");
                OnPropertyChanged("IsResolved");
                OnPropertyChanged("IsResolvable");
            }
        }

        public override string Scope
        {
            get
            {
                return base.Scope;
            }
            set
            {
                if (base.Scope != null && !base.Scope.Equals(value))
                {
                    base.Scope = value;
                    SetResolvableConflicts();
                }
            }
        }

        public override bool CanSave
        {
            get
            {
                return IsResolved != ResolvedStatus.Resolved && base.CanSave;
            }
        }

        public void SetResolvableConflicts()
        {
            IEnumerable<MigrationConflict> resolvableConflicts = ResolvableConflicts;
            MigrationConflictComparer comparer = new MigrationConflictComparer();
            foreach (ConflictRuleViewModel conflictRule in m_appViewModel.AllConflicts)
            {
                if (resolvableConflicts.Contains(conflictRule.MigrationConflict, new MigrationConflictComparer()))
                {
                    conflictRule.IsResolvable = true;
                }
                else
                {
                    conflictRule.IsResolvable = false;
                }
            }
        }

        public int ResolvedByRuleId { get; set; }
        private bool m_showAdvancedOptions = false;
        public bool ShowAdvancedOptions
        {
            get
            {
                return m_showAdvancedOptions;
            }
            set
            {
                m_showAdvancedOptions = value;
                OnPropertyChanged("ShowAdvancedOptions");
            }
        }
        private ConflictResolutionRule m_newRule;
        public IEnumerable<ConflictResolutionResult> Save()
        {
            if (CustomControl != null && !ShowAdvancedOptions)
            {
                CustomControl.Save();
            }

            if (CanSave)
            {
                m_newRule = SelectedResolutionAction.NewRule(Scope, Description, DataFields.ToDictionary(x => x.FieldName, x => x.FieldValue));
                
                List<ConflictResolutionResult> results = new List<ConflictResolutionResult>();
                ConflictResolutionResult firstConflictResolutionResult = m_conflictManager.ResolveExistingConflictWithNewRule(m_conflict.Id, m_newRule);
                firstConflictResolutionResult.ConflictInternalId = ConflictInternalId;
                results.Add(firstConflictResolutionResult);

                if (firstConflictResolutionResult.Resolved)
                {
                    try
                    {
                        IEnumerable<ConflictResolutionResult> otherConflictsResolutionResult = m_conflictManager.ResolveExistingConflictWithExistingRule(m_newRule);
                        results.AddRange(otherConflictsResolutionResult);
                    }
                    catch (MigrationException)
                    { }
                    catch (EntityException)
                    { }
                }

                foreach (ConflictResolutionResult result in results)
                {
                    if (this.ConflictInternalId == result.ConflictInternalId)
                    {
                        if (result.Resolved)
                        {
                            IsResolved = ResolvedStatus.Resolved;
                            ResolvedByRuleId = m_newRule.InternalId;
                        }
                        else
                        {
                            IsResolved = ResolvedStatus.Failed;
                        }
                    }
                }

                m_appViewModel.SetResolvedConflicts(results, m_newRule.InternalId);

                // add new rule to rules list
                RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
                var v = from r in context.RTResolutionRuleSet
                        where r.Id == ResolvedByRuleId
                        select r;
                if (v.Count() > 0)
                {
                    m_appViewModel.Rules.Insert(0, new ExistingRuleViewModel(v.First(), m_appViewModel));
                }
            
                return results;
            }
            else
            {
                throw new Exception("Invalid scope");
            }
        }
        public RTConflict RTConflict
        {
            get
            {
                return m_conflict;
            }
        }
        private RTConflict m_conflict;
        private MigrationConflict m_migrationConflict;
        private bool m_isResolvable;
        
        class MigrationConflictComparer : IEqualityComparer<MigrationConflict>
        {
            #region IEqualityComparer<MigrationConflict> Members

            public bool Equals(MigrationConflict x, MigrationConflict y)
            {
                if (x == null || y == null)
                {
                    return false;
                }
                else
                {
                    return x.ConflictType.ReferenceName.Equals(y.ConflictType.ReferenceName)
                        && x.ConflictDetails.Equals(y.ConflictDetails)
                        && x.ScopeHint.Equals(y.ScopeHint);
                }
            }

            public int GetHashCode(MigrationConflict obj)
            {
                return obj.ConflictType.ReferenceName.GetHashCode()
                    | obj.ConflictDetails.GetHashCode()
                    | obj.ScopeHint.GetHashCode();
            }

            #endregion
        }
    }
    public enum ResolvedStatus
    {
        Unresolved,
        Resolved,
        Failed
    }
}
