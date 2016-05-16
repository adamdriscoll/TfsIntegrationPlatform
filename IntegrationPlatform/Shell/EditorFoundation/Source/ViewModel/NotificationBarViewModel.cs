// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using System;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class NotificationBarViewModel : ViewModelBase
    {
        public NotificationBarViewModel(ShellViewModel shellVM)
        {
            ShellViewModel = shellVM;
        }

        public void RefreshDefaultNotification()
        {
            if (ShellViewModel.IsCompleted)
            {
                ShellViewModel.SystemState = SystemState.MigrationCompleted;
                if (ShellViewModel.DataModel != null)
                {
                    SetState(NotificationState.Completed,
                        ShellViewModel.DataModel.Configuration.SessionGroup.FriendlyName,
                        Properties.Resources.ConfigurationSuccessString);
                }
            }
            else
            {
                //TODO: Error icon issue
                if (ShellViewModel.ConflictManager != null && ShellViewModel.ConflictManager.TotalConflicts > 0)
                {
                    if (ShellViewModel.DataModel != null)
                    {
                        string conflictString;
                        if (ShellViewModel.ConflictManager.TotalConflicts == 1)
                        {
                            conflictString = Properties.Resources.OneConflictString;
                        }
                        else
                        {
                            conflictString = Properties.Resources.ConflictsString;
                        }
                        SetState(NotificationState.Warning, ShellViewModel.DataModel.Configuration.SessionGroup.FriendlyName,
                            ShellViewModel.CurrentPipelineState.ToString(),
                            String.Format("{0} {1}", ShellViewModel.ConflictManager.TotalConflicts, conflictString),
                            Properties.Resources.ResolveConflictString, 
                            ShellCommands.ViewConflicts);
                    }
                }
                else
                {
                    if (ShellViewModel.DataModel != null)
                    {
                        SetState(NotificationState.Info, ShellViewModel.DataModel.Configuration.SessionGroup.FriendlyName,
                            ShellViewModel.CurrentPipelineState.ToString());
                    }
                }
            }
        }

        public ShellViewModel ShellViewModel { get; set; }
        private bool m_showNotifications = true;
        public bool ShowNotifications
        {
            get
            {
                return m_showNotifications;
            }
            set
            {
                if (m_showNotifications != value)
                {
                    m_showNotifications = value;
                    OnPropertyChanged("ShowNotifications");
                }
            }
        }

        private string m_notification1 = string.Empty;
        public string Notification1
        {
            get
            {
                return m_notification1;
            }
            private set
            {
                m_notification1 = value;
                OnPropertyChanged("Notification1");
            }
        }
        private string m_notification2 = string.Empty;
        public string Notification2
        {
            get
            {
                return m_notification2;
            }
            private set
            {
                m_notification2 = value;
                OnPropertyChanged("Notification2");
            }
        }
        private string m_notification3 = string.Empty;
        public string Notification3
        {
            get
            {
                return m_notification3;
            }
            private set
            {
                m_notification3 = value;
                OnPropertyChanged("Notification3");
            }
        }
        private string m_link = string.Empty;
        public string Link
        {
            get
            {
                return m_link;
            }
            private set
            {
                m_link = value;
                OnPropertyChanged("Link");
            }
        }
        private SolidColorBrush m_background;
        public SolidColorBrush Background
        {
            get
            {
                return m_background;
            }
            private set
            {
                m_background = value;
                OnPropertyChanged("Background");
            }
        }
        private string m_icon = string.Empty;
        public string Icon
        {
            get
            {
                return m_icon;
            }
            private set
            {
                m_icon = value;
                OnPropertyChanged("Icon");
            }
        }
        private RoutedUICommand m_actionCommand = null;
        public RoutedUICommand ActionCommand
        {
            get
            {
                return m_actionCommand;
            }
            private set
            {
                m_actionCommand = value;
                OnPropertyChanged("ActionCommand");
            }
        }

        public void SetState(NotificationState newState, string message1)
        {
            SetState(newState, message1, string.Empty, string.Empty, string.Empty, null);
        }

        public void SetState(NotificationState newState, string message1, string message2)
        {
            SetState(newState, message1, message2, string.Empty, string.Empty, null);
        }

        public void SetState(NotificationState newState, string message1, string message2, string message3)
        {
            SetState(newState, message1, message2, message3, string.Empty, null);
        }

        public void SetState(NotificationState newState, string message1, string message2, string message3, string link, RoutedUICommand linkHandler)
        {
            SetMessages(message1, message2, message3, link, linkHandler);
            switch (newState)
            {
                case NotificationState.Completed:
                    {
                        Icon = "Images/confirmation32.png";
                        Background = Brushes.Green;
                        break;
                    }
                case NotificationState.Error:
                    {
                        Icon = "Images/error32.png";
                        Background = Brushes.Red;
                        break;
                    }
                case NotificationState.Info:
                    {
                        Icon = "Images/information32.png";
                        Background = Brushes.DarkBlue;
                        break;
                    }
                case NotificationState.Warning:
                    {
                        Icon = "Images/warning32.png";
                        Background = Brushes.Gold;
                        break;
                    }
                case NotificationState.InProgress:
                    {
                        Icon = "Images/inProgress.png";
                        Background = Brushes.LightGreen;
                        break;
                    }
                case NotificationState.Paused:
                    {
                        Icon = "Images/paused.png";
                        Background = Brushes.LightGreen;
                        break;
                    }
            }
        }

        public bool AddNotification(string message)
        {
            ShowNotifications = true;
            if (String.IsNullOrEmpty(Notification1))
            {
                Notification1 = message;
                return true;
            }
            else if (String.IsNullOrEmpty(Notification2))
            {
                Notification2 = message;
                return true;
            }
            else if (String.IsNullOrEmpty(Notification3))
            {
                Notification3 = message;
                return true;
            }
            return false;
        }

        public void ClearNotifications()
        {
            Notification1 = Notification2 = Notification3 = string.Empty;
        }

        private void SetMessages(string message1, string message2, string message3, string link, RoutedUICommand command)
        {
            Notification1 = message1;
            Notification2 = message2;
            Notification3 = message3;
            Link = link;
            ActionCommand = command;
        }
    }

    public enum NotificationState
    {
        Completed,
        Warning,
        Error,
        InProgress,
        Info,
        Paused
    }

}
