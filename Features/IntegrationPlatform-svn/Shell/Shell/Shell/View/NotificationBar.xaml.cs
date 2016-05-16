// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for NotificationBar.xaml
    /// </summary>
    public partial class NotificationBar : UserControl
    {
        public NotificationBar()
        {
            InitializeComponent();
        }

        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            NotificationBarViewModel viewModel = (NotificationBarViewModel)this.DataContext;
            if (viewModel.ActionCommand != null)
            {
                viewModel.ActionCommand.Execute(viewModel.ShellViewModel, sender as IInputElement);
            }
        }
    }
}
