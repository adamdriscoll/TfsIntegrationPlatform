// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    /// <summary>
    /// Interaction logic for InvalidFieldValueCustomControl.xaml
    /// </summary>
    public partial class InvalidFieldValueCustomControl : UserControl
    {
        public InvalidFieldValueCustomControl()
        {
            InitializeComponent();
        }

        #region IConflictTypeUserControl Members

        private Dictionary<string, FrameworkElement> m_details;
        public Dictionary<string, FrameworkElement> Details
        {
            get
            {
                if (m_details == null)
                {
                    m_details = new Dictionary<string, FrameworkElement>();

                    m_details["Details"] = ConflictTypeViewModelBase.CreateTextElement(m_invalidFieldValueConflict.Description);
                }
                return m_details;
            }
        }

        public void Save()
        {
            if ((bool)updateConfigRadioButton.IsChecked)
            {
                m_invalidFieldValueConflict.UpdateConfiguration();
            }
            else if ((bool)newValueRadioButton.IsChecked)
            {
                m_invalidFieldValueConflict.SetNewValue();
            }
            else if ((bool)retryRadioButton.IsChecked)
            {
                m_invalidFieldValueConflict.Retry();
            }
            else if ((bool)dropFieldRadioButton.IsChecked)
            {
                m_invalidFieldValueConflict.DropField();
            }
        }

        protected InvalidFieldValueConflictViewModel m_invalidFieldValueConflict;
        
        #endregion

        private void newValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            newValueRadioButton.IsChecked = true;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            m_invalidFieldValueConflict = e.NewValue as InvalidFieldValueConflictViewModel;
        }
    }

    public abstract class InvalidFieldValueConflictViewModel : UpdateConfigConflictViewModelBase
    {
        public InvalidFieldValueConflictViewModel(ConflictRuleViewModel viewModel)
            : base(viewModel)
        {
            Initialize(TargetField, TargetWorkItemType);
        }

        #region Properties
        public string NewValue { get; set; }

        public bool AlwaysMapToValue { get; set; }

        public abstract string TargetFieldDispName { get; }

        public abstract string TargetFieldCurrentValue { get; }

        public abstract string TargetFieldOriginalValue { get; }

        public abstract string TargetField { get; }

        public abstract string TargetWorkItemType { get; }

        #endregion

        #region Public Methods
        public virtual void SetNewValue()
        {
            UndoXmlChanges();

            WITSessionCustomSetting customSettings = m_session.WITCustomSetting;

            if (m_mappedField == null) // no field mapping exists
            {
                // add new field mapping
                m_mappedField = new MappedField();
                if (IsLeftSidedConflict)
                {
                    m_mappedField.MapFromSide = SourceSideTypeEnum.Right;
                    m_mappedField.RightName = SourceField;
                    m_mappedField.LeftName = TargetField;
                }
                else
                {
                    m_mappedField.MapFromSide = SourceSideTypeEnum.Left;
                    m_mappedField.LeftName = SourceField;
                    m_mappedField.RightName = TargetField;
                }

                m_fieldMap.MappedFields.MappedField.Add(m_mappedField);
            }

            // add/get valueMap
            ValueMap valueMap;
            if (!string.IsNullOrEmpty(m_mappedField.valueMap))
            {
                valueMap = customSettings.ValueMaps.ValueMap.FirstOrDefault(x => string.Equals(x.name, m_mappedField.valueMap));
                Debug.Assert(valueMap != null, string.Format("valueMap {0} not found", m_mappedField.valueMap));
            }
            else
            {
                valueMap = new ValueMap();
                valueMap.name = GenerateValueMapName(customSettings.ValueMaps.ValueMap, TargetFieldDispName);
                m_mappedField.valueMap = valueMap.name;
                customSettings.ValueMaps.ValueMap.Add(valueMap);
            }

            // add/set value
            string sourceValue = AlwaysMapToValue ? WitMappingConfigVocab.Any : TargetFieldCurrentValue;
            if (IsLeftSidedConflict)
            {
                UpdateValueMap(TargetFieldCurrentValue, sourceValue, NewValue, sourceValue, valueMap.Value);
            }
            else
            {
                UpdateValueMap(sourceValue, TargetFieldCurrentValue, sourceValue, NewValue, valueMap.Value);
            }

            // save config
            m_session.UpdateCustomSetting(customSettings);
            int configId = SaveConfiguration();

            // set conflictRuleViewModel options
            ObservableDataField updatedConfigIdDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, Constants.DATAKEY_UPDATED_CONFIGURATION_ID));
            updatedConfigIdDataField.FieldValue = configId.ToString();
        }

        public virtual void DropField()
        {
            UndoXmlChanges();

            WITSessionCustomSetting customSettings = m_session.WITCustomSetting;

            if (m_mappedField == null)
            {
                m_mappedField = new MappedField();
                m_fieldMap.MappedFields.MappedField.Add(m_mappedField);
            }

            m_mappedField.valueMap = string.Empty;
            if (IsLeftSidedConflict)
            {
                m_mappedField.RightName = SourceField;
                m_mappedField.LeftName = string.Empty;
                m_mappedField.MapFromSide = SourceSideTypeEnum.Right;
            }
            else
            {
                m_mappedField.LeftName = SourceField;
                m_mappedField.RightName = string.Empty;
                m_mappedField.MapFromSide = SourceSideTypeEnum.Left;
            }

            // save config
            m_session.UpdateCustomSetting(customSettings);
            int configId = SaveConfiguration();

            // set conflictRuleViewModel options
            ObservableDataField updatedConfigIdDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, Constants.DATAKEY_UPDATED_CONFIGURATION_ID));
            updatedConfigIdDataField.FieldValue = configId.ToString();
        }

        internal void UpdateConfiguration()
        {
            // select resolution action
            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new UpdatedConfigurationResolutionAction().ReferenceName));

            // save config
            int configId = SaveConfiguration();

            // set data fields
            ObservableDataField updatedConfigIdDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, Constants.DATAKEY_UPDATED_CONFIGURATION_ID));
            updatedConfigIdDataField.FieldValue = configId.ToString();
        }

        internal void Retry()
        {
            UndoXmlChanges();

            // select resolution action
            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new ManualConflictResolutionAction().ReferenceName));
        } 
        #endregion

        #region Private Methods
        private void UpdateValueMap(string oldLeftValue, string oldRightValue, string newLeftValue, string newRightValue, ICollection<Value> values)
        {
            try
            {
                Value value = values.SingleOrDefault(x => string.Equals(x.LeftValue, oldLeftValue) && string.Equals(x.RightValue, oldRightValue));
                if (value == null)
                {
                    value = new Value();
                    values.Add(value);
                }

                value.LeftValue = newLeftValue;
                value.RightValue = newRightValue;
            }
            catch (InvalidOperationException)
            {
                throw new Exception(string.Format("Multiple values (LeftValue={0}, RightValue={1}) were found.  Resolve by editing the configuration", oldLeftValue, oldRightValue));
            }
        }

        private string GenerateValueMapName(IEnumerable<ValueMap> valueMaps, string newName)
        {
            string proposedName = newName + "ValueMap";

            int i = 0;
            while (valueMaps.Select(x => x.name).Contains(proposedName))
            {
                i++;
                proposedName = newName + "ValueMap" + i;
            }

            return proposedName;
        } 
        #endregion
    }
}
