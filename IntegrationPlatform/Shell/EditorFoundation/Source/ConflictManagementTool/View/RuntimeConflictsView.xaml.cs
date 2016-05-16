// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for RuntimeConflictsView.xaml
    /// </summary>
    public partial class RuntimeConflictsView : UserControl
    {
        private ApplicationViewModel m_viewModel;

        public RuntimeConflictsView(ApplicationViewModel viewModel)
        {
            m_viewModel = viewModel;
            DataContext = m_viewModel;
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.DisplayExtendedInformation(sender as FrameworkElement);
        }

        private void conflictsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UIHelper.DisplayExtendedInformation(sender as FrameworkElement);
        }

        private void acknowledgeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var v in conflictsListView.SelectedItems.Cast<ConflictRuleViewModel>().Where(x => x.CanSave))
                {
                    v.Save();
                }
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.ShellViewModel.PopViewModel(this);
                m_viewModel.Refresh();
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }

        private void nextPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.GetMoreRuntimeConflicts();
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }

        private void acknowledgeAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_viewModel.AcknowledgeAllRuntimeConflicts();
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex);
            }
        }
    }
}
