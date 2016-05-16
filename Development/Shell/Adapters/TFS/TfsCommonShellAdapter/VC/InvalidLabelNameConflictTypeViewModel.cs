// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class InvalidLabelNameConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private TextBoxControlViewModel controlViewModel;

        public InvalidLabelNameConflictTypeViewModel()
        {
            controlViewModel = new TextBoxControlViewModel();
            TextBoxControl textControl = new TextBoxControl();
            textControl.DataContext = controlViewModel;

            ConflictTypeDescription = Properties.Resources.InvalidLabelNameConflictTypeDescription;

            ResolutionActionViewModel renameLabelAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.RenameLabelAction,
                ResolutionActionReferenceName = new VCLabelConflictManualRenameAction().ReferenceName,
                UserControl = textControl,
                ExecuteCommand = SetNewLabelName,
                IsSelected = true
            };
            RegisterResolutionAction(renameLabelAction);

            /* Action code is commented out until the conflict handler can handle this for any adaptter via an interface and not just for TFS
             * ResolutionActionViewModel replaceInvalidCharacterAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.ReplaceInvalidCharacterAction,
                ResolutionActionReferenceName = new VCInvalidLabelNameAutomaticRenameAction().ReferenceName,
            };
            RegisterResolutionAction(replaceInvalidCharacterAction); */
        }

        public void SetNewLabelName()
        {
            ObservableDataField dataField = m_viewModel.ObservableDataFields.FirstOrDefault(x => string.Equals(
                x.FieldName, VCLabelConflictManualRenameAction.DATAKEY_RENAME_LABEL));
            Debug.Assert(dataField != null, string.Format("No DataField with key {0}", VCLabelConflictManualRenameAction.DATAKEY_RENAME_LABEL));
            dataField.FieldValue = controlViewModel.Text;
        }
    }
}