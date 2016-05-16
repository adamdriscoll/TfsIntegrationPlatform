// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    public class TFSInvalidFieldValueCustomControl : IConflictTypeUserControl
    {
        private InvalidFieldValueCustomControl m_userControl;
        private TFSInvalidFieldValueConflictViewModel m_invalidFieldValueConflict;

        public TFSInvalidFieldValueCustomControl()
        {
            m_userControl = new InvalidFieldValueCustomControl();
        }

        #region IConflictTypeUserControl Members

        public void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_invalidFieldValueConflict = new TFSInvalidFieldValueConflictViewModel(viewModel);
            m_userControl.DataContext = m_invalidFieldValueConflict;
        }

        public UserControl UserControl
        {
            get
            {
                return m_userControl;
            }
        }

        public Dictionary<string, FrameworkElement> Details
        {
            get
            {
                return m_userControl.Details;
            }
        }

        public void Save()
        {
            m_userControl.Save();
        }

        #endregion
    }

    public class TFSInvalidFieldValueConflictViewModel : InvalidFieldValueConflictViewModel
    {
        public TFSInvalidFieldValueConflictViewModel(ConflictRuleViewModel viewModel)
            : base(viewModel)
        {
        }

        public override string TargetFieldDispName
        {
            get
            {
                return m_properties[InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldDispName];
            }
        }

        public override string TargetFieldCurrentValue
        {
            get
            {
                return m_properties[InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldCurrentValue];
            }
        }

        public override string TargetFieldOriginalValue
        {
            get
            {
                return m_properties[InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldOriginalValue];
            }
        }

        public override string TargetField
        {
            get
            {
                return m_properties[InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetFieldRefName];
            }
        }

        public override string TargetWorkItemType
        {
            get
            {
                return m_properties[InvalidFieldValueConflictTypeConstants.ConflictDetailsKey_TargetWorkItemType];
            }
        }

        public override void DropField()
        {
            base.DropField();

            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new InvalidFieldConflictDropFieldAction().ReferenceName));
            ObservableDataField invalidFieldDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, InvalidFieldConflictDropFieldAction.DATAKEY_INVALID_FIELD));
            invalidFieldDataField.FieldValue = TargetField;
        }

        public override void SetNewValue()
        {
            base.SetNewValue();

            // set conflictRuleViewModel options
            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new InvalidFieldValueConflictUseValueMapAction().ReferenceName));
            ObservableDataField mapFromDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, InvalidFieldValueConflictUseValueMapAction.DATAKEY_MAP_FROM));
            mapFromDataField.FieldValue = TargetFieldCurrentValue;
            ObservableDataField mapToDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, InvalidFieldValueConflictUseValueMapAction.DATAKEY_MAP_TO));
            mapToDataField.FieldValue = NewValue;
        }
    }
}
