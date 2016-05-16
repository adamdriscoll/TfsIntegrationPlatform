// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for RuleEditView.xaml
    /// </summary>
    public partial class RuleEditView : UserControl, IConflictTypeUserControl
    {
        public RuleEditView()
        {
            InitializeComponent();
        }

        #region IConflictTypeUserControl Members

        public UserControl UserControl
        {
            get
            {
                return this;
            }
        }

        private Dictionary<string, FrameworkElement> m_details;
        public Dictionary<string, FrameworkElement> Details
        {
            get
            {
                if (m_details == null)
                {
                    m_details = new Dictionary<string, FrameworkElement>();

                    m_details["Description"] = this.Resources["Description"] as TextBlock;
                    m_details["Description"].DataContext = m_conflictRuleViewModel;

                    m_details["Details"] = this.Resources["Details"] as TextBox;
                    m_details["Details"].DataContext = m_conflictRuleViewModel;
                }
                return m_details;
            }
        }

        public void Save()
        {
            // do nothing
        }

        private ConflictRuleViewModel m_conflictRuleViewModel;

        public void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_conflictRuleViewModel = viewModel;
        }

        #endregion
    }
}
