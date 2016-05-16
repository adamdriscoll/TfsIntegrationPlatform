// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.SubversionShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class SubversionShellAdapter : TfsCommonVCShellAdapter
    {
        private static Guid s_providerId = new Guid(m_adapterGuid);

        #region Fields
        private const string m_adapterGuid = "BCC31CA2-534D-4054-9013-C1FEF67D5273";
        private const string m_adapterName = "SVN Migration Provider";
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
            SubversionConnectDialog dialog = new SubversionConnectDialog(migrationSource);
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

        private void PopulateFilterString(FilterItem filterItem, MigrationSource migrationSource)
        {
            VCServerPathDialog dialog = new VCServerPathDialog(filterItem, migrationSource);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        #endregion
    }
}