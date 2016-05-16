// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class VCPathNotMappedConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public VCPathNotMappedConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.VCPathNotMappedConflictTypeDescription;

            ResolutionActionViewModel changeToAddAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.ChangeToAddAction,
                ResolutionActionReferenceName = new VCChangeToAddOnBranchSourceNotMappedAction().ReferenceName,
                IsSelected = true
            };

            ResolutionActionViewModel addPathToMappingAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.AddPathToMappingAction,
                ResolutionActionReferenceName = new VCAddPathToMappingAction().ReferenceName
            };

            RegisterResolutionAction(changeToAddAction);
            RegisterResolutionAction(addPathToMappingAction);
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
                        string.Format("{0} {1}", Properties.Resources.VCPathNotMappedConflictDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}