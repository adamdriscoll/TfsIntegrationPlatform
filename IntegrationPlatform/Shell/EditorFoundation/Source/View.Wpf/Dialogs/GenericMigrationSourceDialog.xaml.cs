// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for GenericMigrationSourceDialog.xaml
    /// </summary>
    public partial class GenericMigrationSourceDialog : Window
    {
        private MigrationSource m_source;

        public GenericMigrationSourceDialog(MigrationSource source)
        {
            InitializeComponent();
            m_source = source;

            friendlyNameTextBox.Text = m_source.FriendlyName;
            serverUrlTextBox.Text = m_source.ServerUrl;
            sourceIdentifierTextBox.Text = m_source.SourceIdentifier;
            /*
            migrationSource.ServerIdentifier = "<hostname>";
            migrationSource.ServerUrl = "http://<hostname>";
            migrationSource.FriendlyName = "<Project> (<hostname>)";
            migrationSource.SourceIdentifier = "<Project>";
            
             */
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            m_source.FriendlyName = friendlyNameTextBox.Text;
            m_source.ServerUrl = serverUrlTextBox.Text;
            m_source.SourceIdentifier = sourceIdentifierTextBox.Text;
            m_source.ServerIdentifier = string.Format("{0}@{1}", m_source.SourceIdentifier, m_source.ServerUrl);
            DialogResult = true;
            Close();
        }
    }
}
