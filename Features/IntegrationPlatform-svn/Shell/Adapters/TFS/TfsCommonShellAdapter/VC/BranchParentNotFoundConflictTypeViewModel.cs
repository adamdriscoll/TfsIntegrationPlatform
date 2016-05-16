// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Windows;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class BranchParentNotFoundConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private ListPathsControlViewModel m_listPathsControlVM;
        public BranchParentNotFoundConflictTypeViewModel()
        {
            m_listPathsControlVM = new ListPathsControlViewModel();
            ListPathsControl control = new ListPathsControl();
            control.DataContext = m_listPathsControlVM;

            ConflictTypeDescription = Properties.Resources.BranchParentNotFoundConflictTypeDescription;

            ResolutionActionViewModel resolveAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.ResolveBranchParentAction,
                ResolutionActionReferenceName = new VCChangeToAddOnBranchParentNotFoundAction().ReferenceName,
                IsSelected = true,
                UserControl = control,
                ExecuteCommand = SetSelectedPath
            };

            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.RetryAction,
                ResolutionActionReferenceName = new VCRetryOnBranchParentNotFoundAction().ReferenceName
            };

            RegisterResolutionAction(resolveAction);
            RegisterResolutionAction(retryAction);
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
                        string.Format("{0} {1}", Properties.Resources.BranchParentNotFoundConflictDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }

        public override void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_viewModel = viewModel;
            UserControl.DataContext = this;

            m_listPathsControlVM.Conflict = m_viewModel.MigrationConflict;
        }

        public void SetSelectedPath()
        {
            m_viewModel.Scope = m_listPathsControlVM.SelectedPath;
        }
    }
}