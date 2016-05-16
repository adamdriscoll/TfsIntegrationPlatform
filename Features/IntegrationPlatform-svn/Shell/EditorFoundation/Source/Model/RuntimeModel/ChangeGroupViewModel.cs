// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ChangeGroupViewModel : ModelObject
    {
        private RTChangeGroup m_changeGroup;
        private RuntimeManager m_host;
        private int m_changeActionCount;

        public ChangeGroupViewModel(RTChangeGroup changeGroup)
        {
            m_changeGroup = changeGroup;

            m_host = RuntimeManager.GetInstance();
        }

        #region Properties
        public RTChangeGroup ChangeGroup
        {
            get { return m_changeGroup; }
        }

        // ChangeGroup
        public bool ContainsBackloggedAction { get { return m_changeGroup.ContainsBackloggedAction; } }
        public string Comment { get { return m_changeGroup.Comment; } }
        public long Id { get { return m_changeGroup.Id; } }
        public string Name { get { return m_changeGroup.Name; } }
        public string Owner { get { return m_changeGroup.Owner; } }
        public int Status { get { return m_changeGroup.Status; } }
        public DateTime? StartTime { get { return m_changeGroup.StartTime; } }
        public DateTime? FinishTime { get { return m_changeGroup.FinishTime; } }

        // View specific or synthesized properties

        public int ChangeActionCount
        {
            // We support a setter to allow our parent in the view model to
            // efficiently compute the count of actions without forcing us down the
            // expensive EF route of loading the whole collection just to count the items.
            get { return m_changeActionCount; }
            set { m_changeActionCount = value; }
        }

        ObservableCollection<ChangeActionViewModel> m_changeActions;

        public ObservableCollection<ChangeActionViewModel> ChangeActions
        {
            get { return m_changeActions; }
        }

        #endregion

        #region Methods
        // Expose refresh methods directly rather than wiring up auto refresh for these
        // expensive drill-through collections.
        public void RefreshChangeActionCount()
        {
            ObjectQuery<RTChangeAction> changeActionObjectQuery = m_changeGroup.ChangeActions.CreateSourceQuery();

            var query = (from changeActions in changeActionObjectQuery
                         select changeActions).Count();

            m_changeActionCount = query;
        }

        public void RefreshChangeActions()
        {
            if (m_changeActions == null)
            {
                m_changeActions = new ObservableCollection<ChangeActionViewModel>();
            }

            ObjectQuery<RTChangeAction> changeActionObjectQuery = m_changeGroup.ChangeActions.CreateSourceQuery();

            var query = (from ca in changeActionObjectQuery
                         orderby ca.ChangeActionId descending
                         select ca).Take(m_host.MaxQueryResults);
        }

        #endregion
    }
}
