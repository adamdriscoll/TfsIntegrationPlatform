// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for ConflictListView.xaml
    /// </summary>
    public partial class ConflictListView : UserControl
    {
        public ConflictListView()
        {
            InitializeComponent();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIHelper.AdjustLastColumnWidth(sender as ListView);
        }

        /// <summary>
        /// An event handler for hyperlink navigate request.
        /// </summary>
        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ApplicationCommands.Help.Execute(null, this);
        }

        private void btnResolve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
                Debug.Assert(viewModel != null);

                ConflictRuleViewModel conflict = viewModel.CurrentRule;

                if (conflict != null)
                {
                    var results = viewModel.ResolveConflict(conflict);

                    if (results.Where(x => !x.Resolved).Count() > 0)
                    {
                        ResultsDialog resultsDialog = new ResultsDialog(results);
                        resultsDialog.Owner = Window.GetWindow(this);
                        resultsDialog.ShowDialog();
                    }

                    viewModel.ResetResolvableConflicts();
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private string FindBestScope(IEnumerable<ConflictRuleViewModel> conflicts)
        {
            if (conflicts.Count() == 1)
            {
                return conflicts.First().Scope;
            }
            else if (conflicts.Count(x => x.IsResolved != ResolvedStatus.Resolved) == 0)
            {
                return null;
            }
            else
            {
                conflicts = conflicts.Where(x => x.IsResolved != ResolvedStatus.Resolved);
                string bestScope = conflicts.OrderBy(x => x.Scope.Length).First().Scope;
                while (true) // brute force
                {
                    bool scopeIsValid = true;
                    foreach (ConflictRuleViewModel conflict in conflicts)
                    {
                        scopeIsValid = conflict.ConflictType.ScopeInterpreter.IsInScope(conflict.Scope, bestScope);
                        if (!scopeIsValid)
                        {
                            break;
                        }
                    }
                    if (scopeIsValid)
                    {
                        return bestScope;
                    }
                    else
                    {
                        bestScope = bestScope.Substring(0, bestScope.Length - 1);
                    }
                }
            }
        }

        private void conflictsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;
            ApplicationViewModel viewModel = DataContext as ApplicationViewModel;

            if (viewModel != null)
            {
                if (list.SelectedItems.Count > 0)
                {
                    var items = list.SelectedItems.Cast<ConflictRuleViewModel>();
                    var v = items.GroupBy(x => x.ConflictType);
                    var v2 = items.GroupBy(x => x.MigrationSource);
                    if (v.Count() == 1 && v2.Count() == 1)
                    {
                        ConflictHelpProvider.SetHelpPath(this, Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), 
                                                         @"Documentation\TfsIntegration.chm"));
                        ConflictHelpProvider.SetHelpKeyword(this, items.First().ConflictType);
                        string bestScope = FindBestScope(items);
                        if (bestScope != null)
                        {
                            viewModel.CurrentRule = new ConflictRuleViewModel(items.First());
                            viewModel.CurrentRule.Scope = bestScope;
                            if (items.FirstOrDefault(x => x.IsResolved != ResolvedStatus.Resolved) == null)
                            {
                                viewModel.CurrentRule.IsResolved = ResolvedStatus.Resolved;
                                viewModel.ResetResolvableConflicts();
                            }
                            else
                            {
                                viewModel.CurrentRule.SetResolvableConflicts();
                            }
                        }
                        else
                        {
                            viewModel.CurrentRule = null;
                            viewModel.ResetResolvableConflicts();
                        }
                    }
                    else
                    {
                        viewModel.CurrentRule = null;
                        viewModel.ResetResolvableConflicts();
                    }
                }
                else
                {
                    viewModel.CurrentRule = null;
                    viewModel.ResetResolvableConflicts();
                }
                /*
                Selector listView = sender as Selector;
                if (listView != null)
                {
                    if (listView.SelectedItem != null)
                    {
                        ConflictRuleViewModel conflict = listView.SelectedItem as ConflictRuleViewModel;
                        Debug.Assert(conflict != null);
                        conflict.SetResolvableConflicts();
                    }
                    else
                    {
                        ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
                        Debug.Assert(viewModel != null);
                        viewModel.ResetResolvableConflicts();
                    }
                }
                 * */
            }
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
                    btnResolve_Click(sender, e);
                }
            }
            catch (Exception exception)
            {
                Utilities.HandleException(exception);
            }
        }

        private void conflictsListView_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.GridViewColumnHeader_Click(sender, e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.DisplayExtendedInformation(sender as FrameworkElement);
        }

        private void conflictsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UIHelper.DisplayExtendedInformation(sender as FrameworkElement);
        }

        private void conflictsListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                UIHelper.DisplayExtendedInformation(sender as FrameworkElement);
            }
        }

        private void rulesButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
            if (viewModel != null)
            {
                viewModel.ExistingRuleListView = new ExistingRuleListView(viewModel);
                viewModel.ShellViewModel.PushViewModel(viewModel.ExistingRuleListView);
            }
        }

        private RuntimeConflictsView m_runtimeConflictsView;
        private void runtimeConflictsButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
            if (viewModel != null)
            {
                m_runtimeConflictsView = new RuntimeConflictsView(viewModel);
                viewModel.ShellViewModel.PushViewModel(m_runtimeConflictsView);
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationViewModel viewModel = DataContext as ApplicationViewModel;
            viewModel.ClearResolvedConflicts();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ApplicationViewModel)
            {
                ApplicationViewModel viewModel = e.NewValue as ApplicationViewModel;
                viewModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(viewModel_PropertyChanged);
            }
        }

        void viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, "TotalConflicts"))
            {
                if ((sender as ApplicationViewModel).TotalConflicts > 0)
                {
                    // flash window
                    Flash();
                }
            }
        }

        public static void Flash()
        {
            FLASHWINFO fw = new FLASHWINFO();

            fw.cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO)));
            fw.hwnd = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            fw.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
            fw.uCount = UInt32.MaxValue;

            FlashWindowEx(ref fw);
        }

        //Stop flashing. The system restores the window to its original state.
        public const UInt32 FLASHW_STOP = 0;
        //Flash the window caption.
        public const UInt32 FLASHW_CAPTION = 1;
        //Flash the taskbar button.
        public const UInt32 FLASHW_TRAY = 2;
        //Flash both the window caption and taskbar button.
        //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        public const UInt32 FLASHW_ALL = 3;
        //Flash continuously, until the FLASHW_STOP flag is set.
        public const UInt32 FLASHW_TIMER = 4;
        //Flash continuously until the window comes to the foreground.
        public const UInt32 FLASHW_TIMERNOFG = 12;

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        [DllImport("user32.dll")]
        static extern Int32 FlashWindowEx(ref FLASHWINFO pwfi);
    }
}
