// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class HistoryViewModel : RuntimeSessionGroupViewModel<SessionHistoryViewModel>
    {
        public HistoryViewModel(SessionGroupViewModel sessionGroup)
            : base(sessionGroup)
        {
            sessionGroup.PropertyChanged += new UndoablePropertyChangedEventHandler(sessionGroup_PropertyChanged);
            m_oneWaySessions = new ObservableCollection<OneWaySessionHistoryViewModel>();
        }

        private void sessionGroup_PropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName.Equals("SessionGroupUniqueId"))
            {
                m_oneWaySessions.Clear();
                RaisePropertyChangedEvent("OneWaySessions", null, null);
                RaisePropertyChangedEvent("SelectedOneWaySession", null, null);
            }
        }

        public int Width { get; set; }

        public OneWaySessionHistoryViewModel SelectedOneWaySession
        {
            get
            {
                if (m_selectedOneWaySession == null)
                {
                    SelectedOneWaySession = OneWaySessions.FirstOrDefault();
                }
                return m_selectedOneWaySession;
            }
            set
            {
                m_selectedOneWaySession = value;
                if (m_selectedOneWaySession != null)
                {
                    m_selectedOneWaySession.Width = Width;
                    m_selectedOneWaySession.Refresh();
                }
            }
        }

        public ObservableCollection<OneWaySessionHistoryViewModel> OneWaySessions
        {
            get
            {
                if (m_oneWaySessions.Count() == 0)
                {
                    m_selectedOneWaySession = null;
                    foreach (OneWaySessionHistoryViewModel oneWaySession in Sessions.SelectMany(x => x.OneWaySessions))
                    {
                        m_oneWaySessions.Add(oneWaySession);
                    }
                }
                return m_oneWaySessions;
            }
        }

        private OneWaySessionHistoryViewModel m_selectedOneWaySession;
        private ObservableCollection<OneWaySessionHistoryViewModel> m_oneWaySessions;
    }

    public class RuntimeSessionGroupViewModel<T> : ModelObject where T : RuntimeSessionViewModel, new()
    {
        public RuntimeSessionGroupViewModel(SessionGroupViewModel sessionGroup)
        {
            m_sessionGroup = sessionGroup;
            m_sessionGroup.PropertyChanged += new UndoablePropertyChangedEventHandler(sessionGroup_PropertyChanged);

            Sessions = new ObservableCollection<T>();

            RefreshSessions();
        }

        private void RefreshSessions()
        {
            foreach (var session in Sessions)
            {
                session.Detach();
            }
            Sessions.Clear();

            foreach (SessionViewModel session in m_sessionGroup.SessionViewModels)
            {
                T runtimeSession = new T();
                runtimeSession.Session = session;
                Sessions.Add(runtimeSession);
            }
        }

        private void sessionGroup_PropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName.Equals("SessionGroupUniqueId"))
            {
                RefreshSessions();
            }
        }

        public ObservableCollection<T> Sessions { get; private set; }

        private SessionGroupViewModel m_sessionGroup;
    }
}
