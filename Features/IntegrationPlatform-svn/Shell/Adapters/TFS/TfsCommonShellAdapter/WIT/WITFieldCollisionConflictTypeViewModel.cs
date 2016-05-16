// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    public class WITFieldCollisionConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public WITFieldCollisionConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.WITFieldCollisionConflictTypeDescription;

            ResolutionActionViewModel takeSourceAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.WITEditEditTakeSourceAction,
                ResolutionActionReferenceName = new WITEditEditConflictTakeSourceChangesAction().ReferenceName,
                IsSelected = true
            };
            ResolutionActionViewModel alwaysTakeSourceAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.WITEditEditAlwaysTakeSourceAction,
                ResolutionActionReferenceName = new WITEditEditConflictTakeSourceChangesAction().ReferenceName,
                ExecuteCommand = SetAlwaysScope
            };
            ResolutionActionViewModel takeTargetAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.WITEditEditTakeTargetAction,
                ResolutionActionReferenceName = new WITEditEditConflictTakeTargetChangesAction().ReferenceName
            };
            ResolutionActionViewModel alwaysTakeTargetAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.WITEditEditAlwaysTakeTargetAction,
                ResolutionActionReferenceName = new WITEditEditConflictTakeTargetChangesAction().ReferenceName,
                ExecuteCommand = SetAlwaysScope
            };

            RegisterResolutionAction(takeSourceAction);
            RegisterResolutionAction(alwaysTakeSourceAction);
            RegisterResolutionAction(takeTargetAction);
            RegisterResolutionAction(alwaysTakeTargetAction);
        }

        private void SetAlwaysScope()
        {
            m_viewModel.Scope = "/";
        }

        public override void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_viewModel = viewModel;
            UserControl.DataContext = this;

            UpdateResolutionActionsText();
        }

        private void UpdateResolutionActionsText()
        {
            foreach (ResolutionActionViewModel actionVM in ResolutionActions)
            {
                if (actionVM.ResolutionActionDescription.Equals(Properties.Resources.WITEditEditTakeSourceAction))
                {
                    actionVM.ResolutionActionDescription = string.Format(Properties.Resources.WitTakeChangesAction,
                        m_viewModel.MigrationOther);
                }
                else if (actionVM.ResolutionActionDescription.Equals(Properties.Resources.WITEditEditAlwaysTakeSourceAction))
                {
                    actionVM.ResolutionActionDescription = string.Format(Properties.Resources.WitAlwaysTakeChangesAction,
                        m_viewModel.MigrationOther);
                }
                else if (actionVM.ResolutionActionDescription.Equals(Properties.Resources.WITEditEditTakeTargetAction))
                {
                    actionVM.ResolutionActionDescription = string.Format(Properties.Resources.WitKeepChangesAction,
                        m_viewModel.MigrationSource);
                }
                else if (actionVM.ResolutionActionDescription.Equals(Properties.Resources.WITEditEditAlwaysTakeTargetAction))
                {
                    actionVM.ResolutionActionDescription = string.Format(Properties.Resources.WitAlwaysKeepChangesAction,
                        m_viewModel.MigrationSource);
                }
            }
        }
    }
}
