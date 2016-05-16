// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for ViewRuleDialog.xaml
    /// </summary>
    public partial class ViewRuleDialog : Window
    {
        private ExistingRuleViewModel m_rule;

        internal ViewRuleDialog(ExistingRuleViewModel rule)
        {
            m_rule = rule;

            InitializeComponent();
            DataContext = m_rule;
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIHelper.AdjustLastColumnWidth(sender as ListView);
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ListView_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.GridViewColumnHeader_Click(sender, e);
        }
    }
}
