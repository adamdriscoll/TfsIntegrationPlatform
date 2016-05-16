// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class SessionGroupViewModel : ModelObject
    {
        public SessionGroupViewModel(Guid sessionGroupUniqueId)
        {
            m_host = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)m_host.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;

            SessionGroupUniqueId = sessionGroupUniqueId;
        }

        public Guid SessionGroupUniqueId
        {
            get
            {
                return m_sessionGroupUniqueId;
            }
            set
            {
                m_sessionGroupUniqueId = value;
                Refresh(true);
                RaisePropertyChangedEvent("SessionGroupUniqueId", null, null);
            }
        }

        public ObservableCollection<SessionViewModel> SessionViewModels
        {
            get
            {
                if (m_sessionViewModels == null)
                {
                    m_sessionViewModels = new ObservableCollection<SessionViewModel>();
                }
                return m_sessionViewModels;
            }
        }

        public void AutoRefresh(object sender, EventArgs e)
        {
             Refresh(false);
        }

        private void Refresh(bool forceUpdate)
        {
            RefreshSessions(forceUpdate);
        }

        private void RefreshSessions(bool forceUpdate)
        {
            var all = from sc in m_host.Context.RTSessionConfigSet
                      where sc.SessionGroupConfig.SessionGroup.GroupUniqueId.Equals(m_sessionGroupUniqueId)
                      group sc by sc.SessionUniqueId into g
                      select g;

            if (forceUpdate || all.Count() != SessionViewModels.Count)
            {
                SessionViewModels.Clear();

                foreach (var sessionConfig in all)
                {
                    SessionViewModel sessionViewModel = new SessionViewModel(sessionConfig.Key, sessionConfig.Last());
                    SessionViewModels.Add(sessionViewModel);
                }
            }
        }

        private Guid m_sessionGroupUniqueId;
        private RuntimeManager m_host;
        private ObservableCollection<SessionViewModel> m_sessionViewModels;
    }
}
