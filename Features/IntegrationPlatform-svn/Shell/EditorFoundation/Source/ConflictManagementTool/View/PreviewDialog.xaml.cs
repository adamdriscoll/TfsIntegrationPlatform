// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for PreviewDialog.xaml
    /// </summary>
    public partial class PreviewDialog : Window
    {
        private RuleViewModelBase m_rule;

        internal PreviewDialog(RuleViewModelBase rule)
        {
            m_rule = rule;

            InitializeComponent();
            DataContext = m_rule;
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIHelper.AdjustLastColumnWidth(sender as ListView);
        }

        private void resolveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ListView_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.GridViewColumnHeader_Click(sender, e);
        }
    }
}
