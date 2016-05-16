// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter.Properties;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class Tfs2010WITShellAdapter : TfsCommonWITShellAdapter
    {
        private static Guid s_providerId = new Guid(m_adapterGuid);
        private static List<IConflictTypeView> s_conflictTypes;

        static Tfs2010WITShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ExcessivePathConflictType().ReferenceName,
                FriendlyName = Resources.ExcessivePathConflictTypeFriendlyName,
                Description = Resources.ExcessivePathConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new FileAttachmentOversizedConflictType().ReferenceName,
                FriendlyName = Resources.FileAttachmentOversizedConflictTypeFriendlyName,
                Description = Resources.FileAttachmentOversizedConflictTypeDescription,
                Type = typeof(OversizedAttachmentConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new InsufficientPermissionConflictType().ReferenceName,
                FriendlyName = Resources.InsufficientPermissionConflictTypeFriendlyName,
                Description = Resources.InsufficientPermissionConflictTypeDescription,
                Type = typeof(TFSInsufficientPermissionsConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new InvalidFieldConflictType().ReferenceName,
                FriendlyName = Resources.InvalidFieldConflictTypeFriendlyName,
                Description = Resources.InvalidFieldConflictTypeDescription,
                Type = typeof(InvalidFieldCustomControl)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new InvalidFieldValueConflictType().ReferenceName,
                FriendlyName = Resources.InvalidFieldValueConflictTypeFriendlyName,
                Description = Resources.InvalidFieldValueConflictTypeDescription,
                Type = typeof(TFSInvalidFieldValueCustomControl)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new InvalidSubmissionConflictType().ReferenceName,
                FriendlyName = Resources.InvalidSubmissionConflictTypeFriendlyName,
                Description = Resources.InvalidSubmissionConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new WitGeneralConflictType().ReferenceName,
                FriendlyName = Resources.WitGeneralConflictTypeFriendlyName,
                Description = Resources.WitGeneralConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new WorkItemHistoryNotFoundConflictType().ReferenceName,
                FriendlyName = Resources.WorkItemHistoryNotFoundConflictTypeFriendlyName,
                Description = Resources.WorkItemHistoryNotFoundConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new WorkItemTypeNotExistConflictType().ReferenceName,
                FriendlyName = Resources.WorkItemTypeNotExistConflictTypeFriendlyName,
                Description = Resources.WorkItemTypeNotExistConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSCyclicLinkConflictType().ReferenceName,
                FriendlyName = Resources.TFSCyclicLinkConflictTypeFriendlyName,
                Description = Resources.TFSCyclicLinkConflictTypeDescription,
                Type = typeof(CyclicLinkConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSMulitpleParentLinkConflictType().ReferenceName,
                FriendlyName = Resources.TFSMultipleParentLinkConflictTypeFriendlyName,
                Description = Resources.TFSMultipleParentLinkConflictTypeDescription,
                Type = typeof(MultipleParentLinkConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSLinkAccessViolationConflictType().ReferenceName,
                FriendlyName = Resources.TFSLinkAccessViolationConflictTypeFriendlyName,
                Description = Resources.TFSLinkAccessViolationConflictTypeDescription,
                Type = typeof(LinkAccessViolationConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSModifyLockedWorkItemLinkConflictType().ReferenceName,
                FriendlyName = Resources.TFSModifyLockedWorkItemLinkConflictTypeFriendlyName,
                Description = Resources.TFSModifyLockedWorkItemLinkConflictTypeDescription,
                Type = typeof(ModifyLockedWorkItemLinkConflictTypeViewModel)
            });
        }

        #region Fields
        private const string m_adapterGuid = "B84B30DD-1496-462A-BD9D-5A078A617779";
        private const string m_adapterName = "TFS 11 WIT Migration Shell Adapter";
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
                migrationSource.FriendlyName += " (WIT)";
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
                return new ExecuteFilterStringExtension(PopulateFilterItem, c_emptyTfsWitQuery, null);
            }
        }

        private void PopulateFilterItem(FilterItem filterItem, MigrationSource migrationSource)
        {
            WITQueryPickerDialog dialog = new WITQueryPickerDialog(filterItem, migrationSource);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        #endregion
    }
}
