// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Shell.Properties;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for ProviderEditView.xaml
    /// </summary>
    public partial class ProviderEditView : UserControl
    {
        public ProviderEditView()
        {
            InitializeComponent();
        }

        private void providerButton_Click(object sender, RoutedEventArgs e)
        {
            MigrationProviderView providerView = providerListBox.SelectedItem as MigrationProviderView;
            Microsoft.TeamFoundation.Migration.Shell.Tfs.Shell shell = this.DataContext as Microsoft.TeamFoundation.Migration.Shell.Tfs.Shell;

            if (providerView != null && shell != null)
            {
                // TODO: This hack disables the button unless a doc is open
                if (shell.ViewModel.DataModel != null)
                {
                    ICollection<ProviderElement> providers = shell.ViewModel.DataModel.Configuration.Providers.Provider;
                    ProviderElement provider = providers.FirstOrDefault(x => string.Equals(x.ReferenceName, providerView.ProviderId, StringComparison.OrdinalIgnoreCase));

                    if (provider == null)
                    {
                        provider = new ProviderElement();
                        provider.FriendlyName = providerView.Name;
                        provider.ReferenceName = providerView.ProviderId;
                        providers.Add(provider);

                        shell.ViewModel.DataModel.Configuration.SessionGroup.Linking.CreationTime = DateTime.Now;
                    }
                    
                    // TODO: Hack, look it up by provider id
                    //MigrationSourceCommand command = shell.ViewModel.ExtensibileViewModel.MigrationServerViews[0].Command.Target as MigrationSourceCommand;
                    //bool b = shell.ViewModel.ExtensibileViewModel.MigrationServerViews[0].Command.Target is MigrationSourceCommand;
                    //MigrationSourceView serverView = shell.ViewModel.ExtensibileViewModel.MigrationServerViews.FirstOrDefault(x => string.Equals((x.Command.Target as MigrationSourceCommand).ProviderReferenceName, provider.ReferenceName));
                    MigrationSourceView serverView = shell.ViewModel.ExtensibileViewModel.MigrationServerViews[0];
                    MigrationSource migrationSource = new MigrationSource();
                    
                    serverView.Command(migrationSource);

                    if (migrationSource.InternalUniqueId != null)
                    {
                        shell.ViewModel.DataModel.Configuration.SessionGroup.MigrationSources.MigrationSource.Add(migrationSource);
                    }
                }
            }
        }

        private void WITSessionButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewSession(SessionTypeEnum.WorkItemTracking);
        }
        private void VCSessionButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewSession(SessionTypeEnum.VersionControl);
        }
        private void CreateNewSession(SessionTypeEnum sessionType)
        {
            if (leftSource != null && rightSource != null)
            {
                Session session = new Session();
                session.SessionUniqueId = Guid.NewGuid().ToString();
                session.FriendlyName = string.Format(ModelResources.SessionFriendlyNameString, sessionType);
                session.LeftMigrationSourceUniqueId = leftSource.InternalUniqueId;
                session.RightMigrationSourceUniqueId = rightSource.InternalUniqueId;
                session.SessionType = sessionType;

                Microsoft.TeamFoundation.Migration.Shell.Tfs.Shell shell = DataContext as Microsoft.TeamFoundation.Migration.Shell.Tfs.Shell;
                shell.ViewModel.DataModel.Configuration.SessionGroup.Sessions.Session.Add(session);
            }
        }

        private MigrationSource leftSource;
        private MigrationSource rightSource;
        private void LeftRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            leftSource = (sender as RadioButton).DataContext as MigrationSource;
        }
        private void RightRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            rightSource = (sender as RadioButton).DataContext as MigrationSource;
        }

        private void addFilterButton_Click(object sender, RoutedEventArgs e)
        {
            Session session = (sender as Button).DataContext as Session;
            FilterPair lastFilterPair = session.Filters.FilterPair.LastOrDefault();
            if (lastFilterPair == null || (!string.IsNullOrEmpty(lastFilterPair.FilterItem[0].FilterString) && !string.IsNullOrEmpty(lastFilterPair.FilterItem[1].FilterString)))
            {
                FilterPair filterpair = new FilterPair();
                FilterItem leftItem = new FilterItem();
                leftItem.MigrationSourceUniqueId = session.LeftMigrationSourceUniqueId;
                FilterItem rightItem = new FilterItem();
                rightItem.MigrationSourceUniqueId = session.RightMigrationSourceUniqueId;
                filterpair.FilterItem.Add(leftItem);
                filterpair.FilterItem.Add(rightItem);
                session.Filters.FilterPair.Add(filterpair);
            }
        }
    }
}
