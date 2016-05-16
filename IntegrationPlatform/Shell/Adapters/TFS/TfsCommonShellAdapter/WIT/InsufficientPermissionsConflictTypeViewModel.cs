// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    public abstract class InsufficientPermissionsConflictTypeViewModel : ConflictTypeViewModelBase
    {
        private RetryMultipleTimesViewModel retryViewModel;

        public InsufficientPermissionsConflictTypeViewModel()
        {
            retryViewModel = new RetryMultipleTimesViewModel();
            RetryMultipleTimesControl retryControl = new RetryMultipleTimesControl();
            retryControl.DataContext = retryViewModel;

            ResolutionActionViewModel retryAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.RetryAction,
                ResolutionActionReferenceName = new MultipleRetryResolutionAction().ReferenceName,
                UserControl = retryControl,
                ExecuteCommand = SetNumRetries,
                IsSelected = true
            };

            RegisterResolutionAction(retryAction);
        }

        public void SetNumRetries()
        {
            ObservableDataField multipleRetryDataField = m_viewModel.ObservableDataFields.FirstOrDefault(x => string.Equals(x.FieldName, MultipleRetryResolutionAction.DATAKEY_NUMBER_OF_RETRIES));
            Debug.Assert(multipleRetryDataField != null, string.Format("No DataField with key {0}", MultipleRetryResolutionAction.DATAKEY_NUMBER_OF_RETRIES));
            multipleRetryDataField.FieldValue = retryViewModel.SelectedOption.ToString();
        }
    }

    public class CQInsufficientPermissionsConflictTypeViewModel : InsufficientPermissionsConflictTypeViewModel
    {
        public CQInsufficientPermissionsConflictTypeViewModel()
        {
            ConflictTypeDescription = string.Format(Properties.Resources.InsufficientPermissionsConflictTypeDescription, "ClearQuest");
        }
    }

    public class TFSInsufficientPermissionsConflictTypeViewModel : InsufficientPermissionsConflictTypeViewModel
    {
        public TFSInsufficientPermissionsConflictTypeViewModel()
        {
            ConflictTypeDescription = string.Format(Properties.Resources.InsufficientPermissionsConflictTypeDescription, "TFS");
        }

        private Dictionary<string, FrameworkElement> m_details;
        public override Dictionary<string, FrameworkElement> Details
        {
            get
            {
                if (m_details == null)
                {
                    m_details = new Dictionary<string, FrameworkElement>();

                    ConflictDetailsProperties properties = m_viewModel.MigrationConflict.ConflictDetailsProperties;
                    string fullUsername;
                    if (string.IsNullOrEmpty(properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_UserDomain]))
                    {
                        fullUsername = properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_UserAlias];
                    }
                    else
                    {
                        fullUsername = properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_UserDomain] + "\\" +
                                       properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_UserAlias];
                    }
                    string details = string.Format(@"User '{0}' needs to be in TFS permission group '{1}'.",
                        fullUsername,
                        properties[InsufficientPermissionConflictTypeConstants.ConflictDetailsKey_PermissionGroupName]);
                    m_details["Details"] = CreateTextElement(details);
                }
                return m_details;
            }
        }
    }
}
