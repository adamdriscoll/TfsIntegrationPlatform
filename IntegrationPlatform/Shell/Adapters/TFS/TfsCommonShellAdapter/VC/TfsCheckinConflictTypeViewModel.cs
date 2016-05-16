// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class TfsCheckinConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public TfsCheckinConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.TfsCheckinConflictTypeDescription;

            ResolutionActionViewModel autoResolveAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.AutoResolveConflict,
                ResolutionActionReferenceName = new TfsCheckinAutoResolveAction().ReferenceName,
                IsSelected = true
            };

            RegisterResolutionAction(autoResolveAction);
            ResolutionActionViewModel skipErrorsAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.SkipErrors,
                ResolutionActionReferenceName = new TfsCheckinSkipAction().ReferenceName
            };

            RegisterResolutionAction(skipErrorsAction);
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
                        string.Format("{0} {1}", Properties.Resources.TfsCheckinConflictTypeDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}