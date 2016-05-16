// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for ChangeGroupView.xaml
    /// </summary>
    public partial class MigrationView : UserControl
    {
        public MigrationView()
        {
            InitializeComponent();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            OneWaySessionMigrationViewModel oneWaySession = (sender as Hyperlink).DataContext as OneWaySessionMigrationViewModel;
            var v = oneWaySession.Source;

            TabControl tabControl = FindVisualParent<TabControl>(this);
            tabControl.SelectedIndex = 1;

            RuntimeManager runtimeManager = RuntimeManager.GetInstance();
            runtimeManager.ConflictManager.SelectedMigrationSource = runtimeManager.ConflictManager.MigrationSources.Single(x => x.UniqueId.Equals(v.UniqueId));
        }

        private parentItemType FindVisualParent<parentItemType>(DependencyObject obj)
            where parentItemType : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            if (parent != null)
            {
                if (parent is parentItemType)
                {
                    return (parentItemType)parent;
                }
                else
                {
                    return FindVisualParent<parentItemType>(parent);
                }
            }
            else
            {
                return null;
            }
        }
    }
}
