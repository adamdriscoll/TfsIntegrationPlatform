// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    public class OversizedAttachmentConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public OversizedAttachmentConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.OversizedAttachmentConflictTypeDescription;

            ResolutionActionViewModel dropAttachmentAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.DropAttachmentAction,
                ResolutionActionReferenceName = new FileAttachmentOversizedConflictDropAttachmentAction().ReferenceName,
                ExecuteCommand = SetMinFileSize,
                IsSelected = true
            };
            ResolutionActionViewModel alwaysDropAttachmentAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.AlwaysDropAttachmentAction,
                ResolutionActionReferenceName = new FileAttachmentOversizedConflictDropAttachmentAction().ReferenceName,
                ExecuteCommand = SetAlwaysScope
            };
            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = "Retry the operation (need to increase maximum attachment size)",
                ResolutionActionReferenceName = new ManualConflictResolutionAction().ReferenceName,
                UserControl = new SetMaxAttachmentHyperlink()
            };

            RegisterResolutionAction(dropAttachmentAction);
            RegisterResolutionAction(alwaysDropAttachmentAction);
            RegisterResolutionAction(retryAction);
        }

        private void SetMinFileSize()
        {
            ConflictDetailsProperties properties = m_viewModel.MigrationConflict.ConflictDetailsProperties;
            string fileSize = properties[FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_FileSize];

            ObservableDataField fileSizeDataField = m_viewModel.ObservableDataFields.FirstOrDefault(x => string.Equals(x.FieldName, FileAttachmentOversizedConflictDropAttachmentAction.DATAKEY_MIN_FILE_SIZE_TO_DROP));
            Debug.Assert(fileSizeDataField != null, string.Format("No DataField with key {0}", FileAttachmentOversizedConflictDropAttachmentAction.DATAKEY_MIN_FILE_SIZE_TO_DROP));
            fileSizeDataField.FieldValue = fileSize;
        }

        private void SetAlwaysScope()
        {
            SetMinFileSize();
            m_viewModel.Scope = "/";
        }
    }
}
