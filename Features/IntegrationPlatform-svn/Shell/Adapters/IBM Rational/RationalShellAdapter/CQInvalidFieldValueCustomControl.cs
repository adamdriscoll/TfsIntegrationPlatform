// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT;

namespace Microsoft.TeamFoundation.Migration.Shell.RationalShellAdapter
{
    public class CQInvalidFieldValueCustomControl : IConflictTypeUserControl
    {
        private InvalidFieldValueCustomControl m_userControl;
        private CQInvalidFieldValueConflictViewModel m_invalidFieldValueConflict;

        public CQInvalidFieldValueCustomControl()
        {
            m_userControl = new InvalidFieldValueCustomControl();
        }

        #region IConflictTypeUserControl Members

        public void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_invalidFieldValueConflict = new CQInvalidFieldValueConflictViewModel(viewModel);
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

    public class CQInvalidFieldValueConflictViewModel : InvalidFieldValueConflictViewModel
    {
        public CQInvalidFieldValueConflictViewModel(ConflictRuleViewModel viewModel)
            : base(viewModel)
        {
            if (string.Equals(m_properties["Reason"], "MissingValueInMandatoryField"))
            {
                m_fieldMap = null;
            }
        }

        public override string TargetFieldDispName
        {
            get
            {
                return m_properties[ClearQuestInvalidFieldValueConflictType.ConflictDetailsKey_FieldName];
            }
        }

        public override string TargetFieldCurrentValue
        {
            get
            {
                return m_properties[ClearQuestInvalidFieldValueConflictType.ConflictDetailsKey_FieldValue];
            }
        }

        public override string TargetFieldOriginalValue
        {
            get
            {
                return m_properties[ClearQuestInvalidFieldValueConflictType.ConflictDetailsKey_FieldValue];
            }
        }

        public override string TargetField
        {
            get
            {
                return m_properties[ClearQuestInvalidFieldValueConflictType.ConflictDetailsKey_FieldName];
            }
        }

        public override string TargetWorkItemType
        {
            get
            {
                return m_properties[ClearQuestInvalidFieldValueConflictType.ConflictDetailsKey_RecordType];
            }
        }

        public override void DropField()
        {
            base.DropField();

            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new DropFieldConflictResolutionAction().ReferenceName));
            ObservableDataField invalidFieldDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, DropFieldConflictResolutionAction.ActionDataKey_FieldName));
            invalidFieldDataField.FieldValue = TargetFieldCurrentValue;
        }

        public override void SetNewValue()
        {
            base.SetNewValue();

            // set conflictRuleViewModel options
            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new UseValueMapConflictResolutionAction().ReferenceName));
            ObservableDataField mapFromDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, UseValueMapConflictResolutionAction.ActionDataKey_MapFromValue));
            mapFromDataField.FieldValue = TargetFieldCurrentValue;
            ObservableDataField mapToDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, UseValueMapConflictResolutionAction.ActionDataKey_MapToValue));
            mapToDataField.FieldValue = NewValue;
            ObservableDataField targetFieldNameDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, UseValueMapConflictResolutionAction.ActionDataKey_TargetFieldName));
            targetFieldNameDataField.FieldValue = TargetField;
        }
    }
}
