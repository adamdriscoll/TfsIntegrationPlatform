// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class TfsHistoryNotFoundConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private TextBoxControlViewModel controlViewModel;

        public TfsHistoryNotFoundConflictTypeViewModel()
        {
            controlViewModel = new TextBoxControlViewModel();
            TextBoxControl textControl = new TextBoxControl();
            textControl.DataContext = controlViewModel;

            ConflictTypeDescription = Properties.Resources.TfsHistoryNotFoundConflictTypeDescription;

            ResolutionActionViewModel suppressHistoryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.SuppressHistoryAction,
                ResolutionActionReferenceName = new TFSHistoryNotFoundSuppressAction().ReferenceName,
                UserControl = textControl,
                ExecuteCommand = SetChangesetID,
                IsSelected = true
            };
            RegisterResolutionAction(suppressHistoryAction);

            ResolutionActionViewModel skipHistoryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.SkipHistoryAction,
                ResolutionActionReferenceName = new TFSHistoryNotFoundSkipAction().ReferenceName,
            };
            RegisterResolutionAction(skipHistoryAction);
        }

        public void SetChangesetID()
        {
            ObservableDataField changesetIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(x => string.Equals(
                x.FieldName, TFSHistoryNotFoundSuppressAction.SupressChangeSetId));
            Debug.Assert(changesetIDDataField != null, string.Format("No DataField with key {0}", TFSHistoryNotFoundSuppressAction.SupressChangeSetId));
            changesetIDDataField.FieldValue = controlViewModel.Text;
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
                        string.Format("{0} {1}", Properties.Resources.TfsHistoryNotFoundConflictTypeDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}