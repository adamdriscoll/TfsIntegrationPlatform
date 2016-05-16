// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class InvalidShortFilenameFormatConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public InvalidShortFilenameFormatConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.InvalidShortFilenameFormatConflictTypeDescription;

            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.RetryAction,
                ResolutionActionReferenceName = new TFSDosShortNameRetryAction().ReferenceName,
                IsSelected = true
            };

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
                        string.Format("{0} {1}", Properties.Resources.InvalidShortFilenameFormatConflictTypeDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}
