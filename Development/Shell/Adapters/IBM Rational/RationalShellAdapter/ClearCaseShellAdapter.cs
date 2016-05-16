// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter.Properties;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class ClearCaseShellAdapter : TfsCommonVCShellAdapter
    {
        private static Guid s_providerId = new Guid(m_adapterGuid);
        private static List<IConflictTypeView> s_conflictTypes;

        static ClearCaseShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new CCAttrTypeNotFoundConflictType().ReferenceName,
                FriendlyName = Resources.CCAttrTypeNotFoundConflictTypeFriendlyName,
                Description = Resources.CCAttrTypeNotFoundConflictTypeDescription,
                Type = typeof(CCAttrTypeNotFoundConflictType)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new CCCheckinConflictType().ReferenceName,
                FriendlyName = Resources.CCCheckinConflictTypeFriendlyName,
                Description = Resources.CCCheckinConflictTypeDescription,
                Type = typeof(CCCheckinConflictTypeViewModel)
            });
        }

        #region Fields
        private const string m_adapterGuid = "f2a6ba65-8acb-4cd0-be8f-b25887f94392";
        private const string m_adapterName = "ClearCase Migration Provider";
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

        public override ExecuteFilterStringExtension FilterStringExtension
        {
            get
            {
                return new ExecuteFilterStringExtension(null, null, string.Empty);
            }
        }

        public override IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return base.GetConflictTypeViews().Concat(s_conflictTypes);
        }

        protected void PopulateMigrationSource(MigrationSource migrationSource)
        {
            ClearCaseConnectDialog dialog = new ClearCaseConnectDialog(migrationSource);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() != true)
            {
                migrationSource.ProviderReferenceName = null;
            }
        }

        protected override Dictionary<string, string> GetMigrationSourceProperties(MigrationSource migrationSource)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            
            properties["Primary VOB"] = migrationSource.ServerUrl;

            string precreatedViewName = GetCustomSetting(migrationSource, CCResources.PrecreatedViewSettingName);
            if (!string.IsNullOrEmpty(precreatedViewName))
            {
                properties["View"] = precreatedViewName;

                string dynamicViewRoot = GetCustomSetting(migrationSource, CCResources.DynamicViewRootSettingName);
                string storageRoot = GetCustomSetting(migrationSource, CCResources.StorageLocationSettingName);
                if (!string.IsNullOrEmpty(dynamicViewRoot))
                {
                    properties["Type"] = "Dynamic";
                    properties["Root"] = dynamicViewRoot;
                }
                else if (!string.IsNullOrEmpty(storageRoot))
                {
                    properties["Type"] = "Snapshot";
                    properties["Root"] = storageRoot;
                }
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
