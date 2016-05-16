// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Shell.View;
namespace Microsoft.TeamFoundation.Migration.Shell.ViewModel
{
    public class HeadlineControlViewModel : ViewModelBase
    {
        public HeadlineControlViewModel(string headline, ShellViewModel shellVM)
        {
            Text = headline;
            ShellViewModel = shellVM;
        }

        public ShellViewModel ShellViewModel { get; set; }

        private string m_text = string.Empty;
        public string Text
        {
            get
            {
                return m_text;
            }
            private set
            {
                m_text = value;
                OnPropertyChanged("Text");
            }
        }

        private bool m_showRefreshTime = false;
        public bool ShowRefreshTime
        {
            get
            {
                return m_showRefreshTime;
            }
            set
            {
                m_showRefreshTime = value;
                OnPropertyChanged("ShowRefreshTime");
            }
        }
    }
}
