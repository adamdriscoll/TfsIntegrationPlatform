// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.FileSystemShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class FileSystemShellAdapter : TfsCommonVCShellAdapter
    {
        private static Guid s_providerId = new Guid(m_adapterGuid);

        #region Fields
        private const string m_adapterGuid = "43B0D301-9B38-4caa-A754-61E854A71C78";
        private const string m_adapterName = "File System Migration Provider";
        #endregion

        #region IPlugin Members
        private MigrationSourceView m_migrationSourceView;
        public override IMigrationSourceView GetMigrationSourceView()
        {
            if (m_migrationSourceView == null)
            {
                m_migrationSourceView = new MigrationSourceView("Connect", s_providerId, null, PopulateMigrationSource, GetMigrationSourceProperties);
            }
            return m_migrationSourceView;
        }

        protected override Dictionary<string, string> GetMigrationSourceProperties(MigrationSource migrationSource)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            return properties;
        }

        protected void PopulateMigrationSource(MigrationSource migrationSource)
        {
            FileSystemConnectDialog dialog = new FileSystemConnectDialog(migrationSource);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            if (dialog.ShowDialog() != true)
            {
                migrationSource.ProviderReferenceName = null;
            }
        }

        public override ExecuteFilterStringExtension FilterStringExtension
        {
            get
            {
                return new ExecuteFilterStringExtension(PopulateFilterString, null, string.Empty);
            }
        }

        private void PopulateFilterString(FilterItem filterItem, MigrationSource migrationSourceConfig)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = false;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filterItem.FilterString = dlg.SelectedPath;
            }
        }

        #endregion
    }
}
