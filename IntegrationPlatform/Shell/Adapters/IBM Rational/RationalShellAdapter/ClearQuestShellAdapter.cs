// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter.Properties;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class ClearQuestShellAdapter : TfsCommonWITShellAdapter
    {
        private static Guid s_providerId = new Guid(m_adapterGuid);
        private static List<IConflictTypeView> s_conflictTypes;

        static ClearQuestShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ClearQuestGenericConflictType().ReferenceName,
                FriendlyName = Resources.ClearQuestGenericConflictTypeFriendlyName,
                Description = Resources.ClearQuestGenericConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ClearQuestInsufficentPrivilegeConflictType().ReferenceName,
                FriendlyName = Resources.ClearQuestInsufficentPrivilegeConflictTypeFriendlyName,
                Description = Resources.ClearQuestInsufficentPrivilegeConflictTypeDescription,
                Type = typeof(CQInsufficientPermissionsConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ClearQuestMissingCQDllConflictType().ReferenceName,
                FriendlyName = Resources.ClearQuestMissingCQDllConflictTypeFriendlyName,
                Description = Resources.ClearQuestMissingCQDllConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ClearQuestSetFieldValueConflictType().ReferenceName,
                FriendlyName = Resources.ClearQuestSetFieldValueConflictTypeFriendlyName,
                Description = Resources.ClearQuestSetFieldValueConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ClearQuestInvalidFieldValueConflictType().ReferenceName,
                FriendlyName = Resources.ClearQuestInvalidFieldValueConflictTypeFriendlyName,
                Description = Resources.ClearQuestInvalidFieldValueConflictTypeDescription,
                Type = typeof(CQInvalidFieldValueCustomControl)
            });
        }

        #region Fields
        private const string m_adapterGuid = "d9637401-7385-4643-9c64-31585d77ed16";
        private const string m_adapterName = "ClearQuest Migration Provider";
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

        public override IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return base.GetConflictTypeViews().Concat(s_conflictTypes);
        }

        public override ExecuteFilterStringExtension FilterStringExtension
        {
            get
            {
                return new ExecuteFilterStringExtension(PopulateFilterString, string.Empty, null);
            }
        }

        private void PopulateFilterString(FilterItem filterItem, MigrationSource migrationSourceConfig)
        {
            StoredQueryDialog dialog = new StoredQueryDialog(filterItem, migrationSourceConfig);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        private void PopulateMigrationSource(MigrationSource migrationSource)
        {
            ClearQuestConnectDialog dialog = new ClearQuestConnectDialog(migrationSource);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() != true)
            {
                migrationSource.ProviderReferenceName = null;
            }
        }

        protected override Dictionary<string, string> GetMigrationSourceProperties(MigrationSource migrationSource)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties["Schema Repository"] = migrationSource.ServerUrl;
            properties["Database"] = migrationSource.SourceIdentifier;
            string userName = GetCustomSetting(migrationSource, "UserName");
            if (!string.IsNullOrEmpty(userName))
            {
                properties["User ID"] = userName;
            }

            return properties;
        }

        private string GetCustomSetting(MigrationSource migrationSource, string key)
        {
            return migrationSource.CustomSettings.CustomSetting.Where(x => x.SettingKey.Equals(key)).Select(x => x.SettingValue).FirstOrDefault();
        }

        #endregion
    }
}
