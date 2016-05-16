// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using System.Collections.Generic;
using System.Windows;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class VCInvalidPathConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public VCInvalidPathConflictTypeViewModel()
        {
            ConflictTypeDescription = Properties.Resources.VCInvalidPathConflictTypeDescription;

            ResolutionActionViewModel skipAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.SkipAction,
                ResolutionActionReferenceName = new VCInvalidPathSkipAction().ReferenceName,
                IsSelected = true
            };

            RegisterResolutionAction(skipAction);
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
                        string.Format("{0} {1}", Properties.Resources.VCInvalidPathConflictTypeDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }

    }
}

