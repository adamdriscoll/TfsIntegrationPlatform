// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs2008ShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class Tfs2008VCShellAdapter : TfsCommonVCShellAdapter
    {
        private static Guid s_providerId = new Guid(m_adapterGuid);
        private static List<IConflictTypeView> s_conflictTypes;

        static Tfs2008VCShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
        }

        #region Fields
        private const string m_adapterGuid = "2f82c6c4-bbee-42fb-b3d0-4799cabcf00e";
        private const string m_adapterName = "TFS 2008 VC Migration Shell Adapter";
        #endregion

        #region IPlugin Members

        private static IMigrationSourceView s_migrationSourceView;
        private MigrationSourceCommand m_command = new MigrationSourceCommand();
        public override IMigrationSourceView GetMigrationSourceView()
        {
            if (s_migrationSourceView == null)
            {
                s_migrationSourceView = new MigrationSourceView(m_command.CommandName, s_providerId, m_command.ButtonImage, PopulateMigrationSource, GetMigrationSourceProperties);
            }
            return s_migrationSourceView;
        }

        private void PopulateMigrationSource(MigrationSource migrationSource)
        {
            m_command.Execute(migrationSource);
            if (migrationSource.ProviderReferenceName != null)
            {
                migrationSource.FriendlyName += " (VC)";
            }
        }

        public override IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return base.GetConflictTypeViews().Concat(s_conflictTypes);
        }

        public override ExecuteFilterStringExtension FilterStringExtension
        {
            get
            {
                return new ExecuteFilterStringExtension(PopulateFilterItem, null, c_vcFilterStringPrefix);
            }
        }

        private void PopulateFilterItem(FilterItem filterItem, MigrationSource migrationSource)
        {
            VCServerPathDialog dialog = new VCServerPathDialog(filterItem, migrationSource);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        #endregion
    }
}
