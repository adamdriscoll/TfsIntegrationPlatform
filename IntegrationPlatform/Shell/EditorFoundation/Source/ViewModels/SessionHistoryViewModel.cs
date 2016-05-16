// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class SessionHistoryViewModel : RuntimeSessionViewModel
    {
        private SessionViewModel m_session;
        public ObservableCollection<OneWaySessionHistoryViewModel> OneWaySessions { get; private set; }

        public override SessionViewModel Session
        {
            get
            {
                return m_session;
            }
            set
            {
                m_session = value;
                OneWaySessions = new ObservableCollection<OneWaySessionHistoryViewModel>();

                foreach (OneWaySessionViewModel oneWaySession in m_session.OneWaySessions.Values)
                {
                    OneWaySessions.Add(new OneWaySessionHistoryViewModel(oneWaySession));
                }
            }
        }
    }
}
