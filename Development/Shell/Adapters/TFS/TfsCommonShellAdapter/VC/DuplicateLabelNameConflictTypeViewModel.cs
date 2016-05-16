// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class DuplicateLabelNameConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private TextBoxControlViewModel controlViewModel;

        public DuplicateLabelNameConflictTypeViewModel()
        {
            controlViewModel = new TextBoxControlViewModel();
            TextBoxControl textControl = new TextBoxControl();
            textControl.DataContext = controlViewModel;

            ConflictTypeDescription = Properties.Resources.DuplicateLabelNameConflictTypeDescription;

            ResolutionActionViewModel renameLabelAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.RenameLabelAction,
                ResolutionActionReferenceName = new VCLabelConflictManualRenameAction().ReferenceName,
                UserControl = textControl,
                ExecuteCommand = SetNewLabelName,
                IsSelected = true
            };
            RegisterResolutionAction(renameLabelAction);

            /* No Automatic Rename label action??? */
        }

        public void SetNewLabelName()
        {
            ObservableDataField dataField = m_viewModel.ObservableDataFields.FirstOrDefault(x => string.Equals(
                x.FieldName, VCLabelConflictManualRenameAction.DATAKEY_RENAME_LABEL));
            Debug.Assert(dataField != null, string.Format("No DataField with key {0}", VCLabelConflictManualRenameAction.DATAKEY_RENAME_LABEL));
            dataField.FieldValue = controlViewModel.Text;
        }

        private Dictionary<string, FrameworkElement> m_details;
        public override Dictionary<string, FrameworkElement> Details
        {
            get
            {
                if (m_details == null)
                {
                    m_details = new Dictionary<string, FrameworkElement>();
                    m_details["Description"] = CreateTextElement(
                        string.Format("{0}", Properties.Resources.DuplicateLabelNameConflictTypeDetails));
                }
                return m_details;
            }
        }
    }
}