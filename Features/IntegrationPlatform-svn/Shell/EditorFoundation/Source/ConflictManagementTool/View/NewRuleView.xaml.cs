// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for NewRuleView.xaml
    /// </summary>
    public partial class NewRuleView : UserControl
    {
        private NewRuleViewModel m_newRule;
        private ApplicationViewModel m_applicationViewModel;
        internal NewRuleView(ApplicationViewModel appViewModel, ExistingRuleViewModel basedOnRule)
        {
            InitializeComponent();
            m_newRule = new NewRuleViewModel(appViewModel, basedOnRule);
            DataContext = m_newRule;
            m_applicationViewModel = appViewModel;
        }

        internal NewRuleView(ApplicationViewModel appViewModel)
            : this(appViewModel, null)
        { }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_newRule.Save();
                CloseView();
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void CloseView()
        {
            m_applicationViewModel.ShellViewModel.PopViewModel(this);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseView();
        }

        private void previewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PreviewDialog previewDialog = new PreviewDialog(m_newRule);
                previewDialog.Owner = Window.GetWindow(this);

                if ((bool)previewDialog.ShowDialog())
                {
                    okButton_Click(sender, e);
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }
    }
}
