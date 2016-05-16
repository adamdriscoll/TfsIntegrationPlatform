// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ChangeActionViewModel : ModelObject
    {
        RTChangeAction m_changeAction;

        public ChangeActionViewModel(RTChangeAction changeAction)
        {
            m_changeAction = changeAction;
        }

        #region Properties
        // ChangeAction

        public string ActionComment { get { return m_changeAction.ActionComment; } }
        public string ActionData { get { return m_changeAction.ActionData; }  }
        public Guid ActionId { get { return m_changeAction.ActionId; }   }
        public int AnalysisPhase { get{ return m_changeAction.AnalysisPhase; }  }
        public bool Backlogged { get{ return m_changeAction.Backlogged; }  }
        public long ChangeActionId { get{ return m_changeAction.ChangeActionId; }  }
        public int? ExecutionOrder { get { return m_changeAction.ExecutionOrder; } }
        public DateTime? FinishTime { get { return m_changeAction.FinishTime; } }
        public string FromPath { get { return m_changeAction.FromPath; } }
        public bool IsSubstituted { get { return m_changeAction.IsSubstituted; } }
        public string ItemTypeReferenceName { get { return m_changeAction.ItemTypeReferenceName; } }
        public string Label { get { return m_changeAction.Label; } }
        public string MergetVersionTo { get { return m_changeAction.MergeVersionTo; } }
        public bool Recursivity { get { return m_changeAction.Recursivity; } }
        public string SourceItem { get { return m_changeAction.SourceItem; } }
        public DateTime? StartTime { get { return m_changeAction.StartTime; } }
        public string ToPath { get { return m_changeAction.ToPath; } }
        public string Version { get { return m_changeAction.Version; } }

        private ObservableCollection<ConflictViewModel> m_conflicts;

        public ObservableCollection<ConflictViewModel> Conflicts 
        {
            get
            {
                m_changeAction.Conflicts.Load();

                foreach (RTConflict conflict in m_changeAction.Conflicts)
                {
                    m_conflicts.Add(new ConflictViewModel(conflict));
                }
                return m_conflicts;
            }
        }

        // View related
        #endregion

    }
}
