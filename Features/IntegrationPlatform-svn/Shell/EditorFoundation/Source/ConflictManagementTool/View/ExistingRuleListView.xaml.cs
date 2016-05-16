// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for ExistingRuleListView.xaml
    /// </summary>
    public partial class ExistingRuleListView : UserControl
    {
        public ExistingRuleListView(ApplicationViewModel appViewModel)
        {
            InitializeComponent();
            DataContext = appViewModel;
        }

        private void rulesListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            if (listView != null)
            {
                UIHelper.AdjustLastColumnWidth(listView);
            }
        }

        private void resolveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                Debug.Assert(button != null);

                ExistingRuleViewModel rule = button.DataContext as ExistingRuleViewModel;
                if (rule != null)
                {
                    IEnumerable<ConflictResolutionResult> results = rule.SaveToDB();

                    if (results.Where(x => !x.Resolved).Count() > 0)
                    {
                        ResultsDialog resultsDialog = new ResultsDialog(results);
                        resultsDialog.Owner = Window.GetWindow(this);
                        resultsDialog.ShowDialog();
                    }
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void btnAddRule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
                if (viewModel != null)
                {
                    viewModel.ShellViewModel.PushViewModel(new NewRuleView(viewModel));
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void btnRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
                Debug.Assert(viewModel != null);
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the selected rule?", "Confirm rule deletion", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Yes)
                {
                    viewModel.RemoveSelectedRule(rulesListView.SelectedItem as ExistingRuleViewModel);
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void rulesListView_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.GridViewColumnHeader_Click(sender, e);
        }

        private void previewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FrameworkElement button = sender as FrameworkElement;
                Debug.Assert(button != null);
                RuleViewModelBase conflict = button.DataContext as RuleViewModelBase;
                Debug.Assert(conflict != null);

                PreviewDialog previewDialog = new PreviewDialog(conflict);
                previewDialog.Owner = Window.GetWindow(this);

                if ((bool)previewDialog.ShowDialog())
                {
                    resolveButton_Click(sender, e);
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayRule(sender as FrameworkElement);
        }

        private void rulesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DisplayRule(sender as FrameworkElement);
        }

        private void rulesListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                DisplayRule(sender as FrameworkElement);
            }
        }

        private void DisplayRule(FrameworkElement element)
        {
            try
            {
                if (element != null)
                {
                    ExistingRuleViewModel rule = element.DataContext as ExistingRuleViewModel;
                    Debug.Assert(rule != null);

                    ViewRuleDialog dialog = new ViewRuleDialog(rule);
                    dialog.Owner = Window.GetWindow(this);
                    dialog.ShowDialog();
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void copyRuleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
                if (viewModel != null)
                {
                    viewModel.ShellViewModel.PushViewModel(new NewRuleView(viewModel, rulesListView.SelectedItem as ExistingRuleViewModel));
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationViewModel appVM = this.DataContext as ApplicationViewModel;
            if (appVM != null)
            {
                appVM.ShellViewModel.PopViewModel(this);
            }
        }
    }
}
