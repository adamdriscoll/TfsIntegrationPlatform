// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class VCNamespaceConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private ChangesetPairControlViewModel m_changesetPairControlVM;
        public VCNamespaceConflictTypeViewModel()
        {
            m_changesetPairControlVM = new ChangesetPairControlViewModel();
            ChangesetPairControl changesetPairControl = new ChangesetPairControl();
            changesetPairControl.DataContext = m_changesetPairControlVM;

            ConflictTypeDescription = Properties.Resources.VCNameSpaceConflictTypeDescription;

            ResolutionActionViewModel userMergeChangesAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.UserMergeChangesAction,
                ResolutionActionReferenceName = new VCContentConflictUserMergeChangeAction().ReferenceName,
                UserControl = changesetPairControl,
                ExecuteCommand = SetChangeSetIDs,
                IsSelected = true
            };
            
            RegisterResolutionAction(userMergeChangesAction);
        }

        public void SetChangeSetIDs()
        {
            ObservableDataField sourceIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(
                x => string.Equals(x.FieldName, VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId));
            Debug.Assert(sourceIDDataField != null, string.Format("No DataField with key {0}", 
                VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId));
            sourceIDDataField.FieldValue = m_changesetPairControlVM.SourceID;

            ObservableDataField targetIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(
                x => string.Equals(x.FieldName, VCContentConflictUserMergeChangeAction.DeltaTableChangeId));
            Debug.Assert(targetIDDataField != null, string.Format("No DataField with key {0}",
                VCContentConflictUserMergeChangeAction.DeltaTableChangeId));
            targetIDDataField.FieldValue = m_changesetPairControlVM.TargetID;
        }

        public override void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_viewModel = viewModel;
            UserControl.DataContext = this;
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
                        string.Format("{0} {1}", Properties.Resources.VCNameSpaceConflictDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}