// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    /// <summary>
    /// Interaction logic for ResultsDialog.xaml
    /// </summary>
    public partial class ResultsDialog : Window
    {
        public ResultsDialog(IEnumerable<ConflictResolutionResult> results)
        {
            InitializeComponent();
            DataContext = results;
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
