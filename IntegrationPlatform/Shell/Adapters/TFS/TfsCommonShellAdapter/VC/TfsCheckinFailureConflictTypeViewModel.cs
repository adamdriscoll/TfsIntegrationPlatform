// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class TfsCheckinFailureConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private ChangesetPairControlViewModel m_changesetPairControlVM;
        private TextBoxControlViewModel m_textControlViewModel;

        public TfsCheckinFailureConflictTypeViewModel()
        {
            m_changesetPairControlVM = new ChangesetPairControlViewModel();
            ChangesetPairControl changesetPairControl = new ChangesetPairControl();
            changesetPairControl.DataContext = m_changesetPairControlVM;

            m_textControlViewModel = new TextBoxControlViewModel();
            TextBoxControl textControl = new TextBoxControl();
            textControl.DataContext = m_textControlViewModel;

            ConflictTypeDescription = Properties.Resources.TfsCheckinFailureConflictTypeDescription;

            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.TfsCheckinFailureConflictRetryAction,
                ResolutionActionReferenceName = new TfsCheckinFailureRetryAction().ReferenceName,
                IsSelected = true
            };
            RegisterResolutionAction(retryAction);

            ResolutionActionViewModel manualResolveAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.UserResolveChangesAction,
                ResolutionActionReferenceName = new TfsCheckinFailureManualResolveAction().ReferenceName,
                UserControl = changesetPairControl,
                ExecuteCommand = SetChangeSetIDs
            };
            RegisterResolutionAction(manualResolveAction);            
        }

        public void SetChangeSetIDs()
        {
            ObservableDataField sourceIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(
                x => string.Equals(x.FieldName, TfsCheckinFailureManualResolveAction.DeltaTableChangeId));
            Debug.Assert(sourceIDDataField != null, string.Format("No DataField with key {0}",
                TfsCheckinFailureManualResolveAction.DeltaTableChangeId));
            sourceIDDataField.FieldValue = m_changesetPairControlVM.SourceID;

            ObservableDataField targetIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(
                x => string.Equals(x.FieldName, TfsCheckinFailureManualResolveAction.MigrationInstructionChangeId));
            Debug.Assert(targetIDDataField != null, string.Format("No DataField with key {0}",
                TfsCheckinFailureManualResolveAction.MigrationInstructionChangeId));
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
                        string.Format("{0} {1}", Properties.Resources.TfsCheckinFailureConflictTypeDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}