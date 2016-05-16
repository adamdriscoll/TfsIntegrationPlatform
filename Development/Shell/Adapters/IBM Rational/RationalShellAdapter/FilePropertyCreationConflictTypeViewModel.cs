// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter;
using Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    public class FilePropertyCreationConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public FilePropertyCreationConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.FilePropertyCreationConflictTypeDescription;

            ResolutionActionViewModel skipAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.SkipAction,
                ResolutionActionReferenceName = new VCFilePropertyCreationConflictSkipAction().ReferenceName,
                IsSelected = true
            };

            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.RetryAction,
                ResolutionActionReferenceName = new VCFilePropertyCreationConflictRetryAction().ReferenceName
            };

            RegisterResolutionAction(skipAction);
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
                        string.Format("{0} {1} ", Properties.Resources.FilePropertyCreationConflictTypeDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}