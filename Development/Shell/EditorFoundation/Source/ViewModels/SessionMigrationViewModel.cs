// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class SessionMigrationViewModel : RuntimeSessionViewModel
    {
        public override void Detach()
        {
            base.Detach();
            foreach (var oneWaySession in OneWaySessions)
            {
                oneWaySession.Detach();
            }
        }

        public override SessionViewModel Session
        {
            get
            {
                return m_session;
            }
            set
            {
                m_session = value;
                OneWaySessions.Clear();

                foreach (OneWaySessionViewModel oneWaySession in m_session.OneWaySessions.Values)
                {
                    OneWaySessions.Add(new OneWaySessionMigrationViewModel(oneWaySession));
                }

                Refresh();
            }
        }

        public override void Refresh()
        {
            ApplicationViewModel conflictManager = RuntimeManager.GetInstance().ConflictManager;
            conflictManager.Refresh();
        }
        
        public ObservableCollection<OneWaySessionMigrationViewModel> OneWaySessions
        {
            get
            {
                return m_oneWaySessions;
            }
        }

        private ObservableCollection<OneWaySessionMigrationViewModel> m_oneWaySessions = new ObservableCollection<OneWaySessionMigrationViewModel>();
        private SessionViewModel m_session;
    }
}
