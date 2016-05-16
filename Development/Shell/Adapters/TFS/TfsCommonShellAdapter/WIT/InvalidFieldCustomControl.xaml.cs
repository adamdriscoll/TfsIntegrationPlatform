// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    /// <summary>
    /// Interaction logic for InvalidFieldCustomControl.xaml
    /// </summary>
    public partial class InvalidFieldCustomControl : UserControl, IConflictTypeUserControl
    {
        public InvalidFieldCustomControl()
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

                    m_details["Details"] = ConflictTypeViewModelBase.CreateTextElement(m_invalidFieldConflict.Description);
                }
                return m_details;
            }
        }

        public void Save()
        {
            if ((bool)updateConfigRadioButton.IsChecked)
            {
                m_invalidFieldConflict.UpdateConfiguration();
            }
            else if ((bool)dropFieldRadioButton.IsChecked)
            {
                m_invalidFieldConflict.DropField();
            }
            else if ((bool)newValueRadioButton.IsChecked)
            {
                m_invalidFieldConflict.SetNewValue();
            }
            else if ((bool)retryRadioButton.IsChecked)
            {
                m_invalidFieldConflict.Retry();
            }
        }

        public UserControl UserControl
        {
            get
            {
                return this;
            }
        }

        private InvalidFieldConflictViewModel m_invalidFieldConflict;

        public void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_invalidFieldConflict = new InvalidFieldConflictViewModel(viewModel);
            DataContext = m_invalidFieldConflict;
        }

        #endregion

        private void newValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            newValueRadioButton.IsChecked = true;
        }
    }

    public class UpdateConfigConflictViewModelBase
    {
        protected ConflictRuleViewModel m_conflictRuleViewModel;
        protected Configuration m_configuration;
        protected Session m_session;
        protected ConflictDetailsProperties m_properties;
        
        private SerializableCustomSettings m_customSettings;
        private string m_cachedCustomSettings;

        protected FieldMap m_fieldMap;
        protected MappedField m_mappedField;
        protected FieldsAggregationGroup m_fieldsAggregationGroup;

        public UpdateConfigConflictViewModelBase(ConflictRuleViewModel viewModel)
        {
            m_conflictRuleViewModel = viewModel;

            m_configuration = m_conflictRuleViewModel.AppViewModel.Config;
            m_session = m_configuration.SessionGroup.Sessions.Session.First(x => m_conflictRuleViewModel.RTConflict.ScopeId.Equals(new Guid(x.SessionUniqueId)));
            m_customSettings = new SerializableCustomSettings(m_session);
            m_cachedCustomSettings = m_customSettings.Serialize();
            
            m_properties = m_conflictRuleViewModel.MigrationConflict.ConflictDetailsProperties;

            string str;
            HasProperties = !m_properties.TryGetValue(ConflictDetailsProperties.DefaultConflictDetailsKey, out str);
        }

        protected int SaveConfiguration()
        {
            m_configuration.UniqueId = Guid.NewGuid().ToString();
            SessionGroupConfigurationManager saver = new SessionGroupConfigurationManager(m_configuration);
            m_conflictRuleViewModel.AppViewModel.ShellViewModel.RefreshConfigViewModel();
            return saver.TrySave();
        }

        public bool HasProperties { get; private set; }

        public bool IsLeftSidedConflict { get; private set; }

        public string SourceField { get; private set; }

        public SerializableCustomSettings CustomSettings
        {
            get
            {
                return m_customSettings;
            }
        }

        public bool HasExplicitFieldMaps
        {
            get
            {
                return m_fieldMap != null;
            }
        }

        public string Description
        {
            get
            {
                return m_conflictRuleViewModel.ConflictDetails;
            }
        }

        protected void Initialize(string targetField, string targetWorkItemType)
        {
            // find workItemType
            if (m_conflictRuleViewModel.RTConflict.SourceSideMigrationSource == null)
            {
                m_conflictRuleViewModel.RTConflict.SourceSideMigrationSourceReference.Load();
            }
            Guid migrationSourceUniqueId = m_conflictRuleViewModel.RTConflict.SourceSideMigrationSource.UniqueId;
            var leftSources = m_configuration.SessionGroup.Sessions.Session.Select(x => x.LeftMigrationSourceUniqueId);
            if (leftSources.Contains(migrationSourceUniqueId.ToString())) // conflict happens on left side
            {
                IsLeftSidedConflict = true;
                WorkItemTypeMappingElement workItemTypeMapping = m_session.WITCustomSetting.WorkItemTypes.WorkItemType.FirstOrDefault(x => string.Equals(x.LeftWorkItemTypeName, targetWorkItemType));
                if (workItemTypeMapping != null && !string.IsNullOrEmpty(workItemTypeMapping.fieldMap))
                {
                    // try to find mappedField
                    m_fieldMap = m_session.WITCustomSetting.FieldMaps.FieldMap.FirstOrDefault(x => string.Equals(x.name, workItemTypeMapping.fieldMap));
                    if (m_fieldMap != null)
                    {
                        m_fieldsAggregationGroup = m_fieldMap.AggregatedFields.FieldsAggregationGroup.FirstOrDefault(x => string.Equals(x.TargetFieldName, targetField) && x.MapFromSide == SourceSideTypeEnum.Right);
                        if (m_fieldsAggregationGroup != null)
                        {
                            SourceField = targetField;
                        }
                        else
                        {
                            m_mappedField = m_fieldMap.MappedFields.MappedField.FirstOrDefault(x => string.Equals(x.LeftName, targetField));
                            if (m_mappedField != null)
                            {
                                SourceField = m_mappedField.RightName;
                            }
                            else
                            {
                                SourceField = targetField;
                            }
                        }
                    }
                }
            }
            else // conflict happens on right side
            {
                IsLeftSidedConflict = false;
                Debug.Assert(m_configuration.SessionGroup.Sessions.Session.Select(x => x.RightMigrationSourceUniqueId).Contains(migrationSourceUniqueId.ToString()));

                // try to find workItemTypeMapping
                WorkItemTypeMappingElement workItemTypeMapping = m_session.WITCustomSetting.WorkItemTypes.WorkItemType.FirstOrDefault(x => string.Equals(x.RightWorkItemTypeName, targetWorkItemType));
                if (workItemTypeMapping != null && !string.IsNullOrEmpty(workItemTypeMapping.fieldMap))
                {
                    // try to find mappedField
                    m_fieldMap = m_session.WITCustomSetting.FieldMaps.FieldMap.FirstOrDefault(x => string.Equals(x.name, workItemTypeMapping.fieldMap));
                    if (m_fieldMap != null)
                    {
                        m_fieldsAggregationGroup = m_fieldMap.AggregatedFields.FieldsAggregationGroup.FirstOrDefault(x => string.Equals(x.TargetFieldName, targetField) && x.MapFromSide == SourceSideTypeEnum.Left);
                        if (m_fieldsAggregationGroup != null)
                        {
                            SourceField = targetField;
                        }
                        else
                        {
                            m_mappedField = m_fieldMap.MappedFields.MappedField.FirstOrDefault(x => string.Equals(x.RightName, targetField));
                            if (m_mappedField != null)
                            {
                                SourceField = m_mappedField.LeftName;
                            }
                            else
                            {
                                SourceField = targetField;
                            }
                        }
                    }
                }
            }
        }

        protected void UndoXmlChanges()
        {
            // undo config changes
            m_customSettings.SerializedContent = m_cachedCustomSettings;
            m_customSettings.Save();
        }
    }

    public class InvalidFieldConflictViewModel : UpdateConfigConflictViewModelBase
    {
        public InvalidFieldConflictViewModel(ConflictRuleViewModel viewModel)
            : base(viewModel)
        {
            Initialize(TargetField, TargetWorkItemType);
        }

        #region Properties
        public string SourceFieldRefName
        {
            get
            {
                return m_properties[InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceFieldRefName];
            }
        }

        public string TargetTeamFoundationServerUrl
        {
            get
            {
                return m_properties[InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetTeamFoundationServerUrl];
            }
        }

        public string TargetTeamProject
        {
            get
            {
                return m_properties[InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetTeamProject];
            }
        }

        public string TargetField
        {
            get
            {
                return m_properties[InvalidFieldConflictTypeConstants.ConflictDetailsKey_SourceFieldRefName];
            }
        }

        public string TargetWorkItemType
        {
            get
            {
                return m_properties[InvalidFieldConflictTypeConstants.ConflictDetailsKey_TargetWorkItemType];
            }
        }

        public string NewTargetField { get; set; } 
        #endregion

        #region Public Methods
        internal void DropField()
        {
            UndoXmlChanges();

            WITSessionCustomSetting customSetting = m_session.WITCustomSetting;

            if (m_fieldsAggregationGroup != null)
            {
                m_fieldMap.AggregatedFields.FieldsAggregationGroup.Remove(m_fieldsAggregationGroup);
            }
            else if (m_mappedField == null)
            {
                m_mappedField = new MappedField();
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
                m_fieldMap.MappedFields.MappedField.Add(m_mappedField);
            }
            else
            {
                m_fieldMap.MappedFields.MappedField.Remove(m_mappedField);
                /*
                if (m_fieldMap.MappedFields.MappedField.Remove(m_mappedField))
                {
                    ValueMap valueMap = customSetting.ValueMaps.ValueMap.FirstOrDefault(x => string.Equals(x.name, m_mappedField.valueMap));
                    if (valueMap != null)
                    {
                        customSetting.ValueMaps.ValueMap.Remove(valueMap);
                    }
                }
                */
            }

            // save config
            m_session.UpdateCustomSetting(customSetting);
            
            int configId = SaveConfiguration();

            // set conflictRuleViewModel options
            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new InvalidFieldConflictDropFieldAction().ReferenceName));
            ObservableDataField invalidFieldDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, InvalidFieldConflictDropFieldAction.DATAKEY_INVALID_FIELD));
            invalidFieldDataField.FieldValue = TargetField;
            ObservableDataField updatedConfigIdDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, Constants.DATAKEY_UPDATED_CONFIGURATION_ID));
            updatedConfigIdDataField.FieldValue = configId.ToString();
        }

        internal void SetNewValue()
        {
            UndoXmlChanges();

            WITSessionCustomSetting customSetting = m_session.WITCustomSetting;

            if (m_fieldsAggregationGroup != null)
            {
                m_fieldsAggregationGroup.TargetFieldName = NewTargetField;
            }
            else if (IsLeftSidedConflict)
            {
                if (m_mappedField == null)
                {
                    m_mappedField = m_fieldMap.MappedFields.MappedField.FirstOrDefault(x => string.Equals(x.RightName, SourceField));
                    if (m_mappedField == null)
                    {
                        m_mappedField = new MappedField();
                        m_mappedField.MapFromSide = SourceSideTypeEnum.Right;
                        m_fieldMap.MappedFields.MappedField.Add(m_mappedField);
                    }
                }

                m_mappedField.RightName = SourceField;
                m_mappedField.LeftName = NewTargetField;
            }
            else
            {
                if (m_mappedField == null)
                {
                    m_mappedField = m_fieldMap.MappedFields.MappedField.FirstOrDefault(x => string.Equals(x.LeftName, SourceField));
                    if (m_mappedField == null)
                    {
                        m_mappedField = new MappedField();
                        m_mappedField.MapFromSide = SourceSideTypeEnum.Left;
                        m_fieldMap.MappedFields.MappedField.Add(m_mappedField);
                    }
                }

                m_mappedField.LeftName = SourceField;
                m_mappedField.RightName = NewTargetField;
            }

            // save config
            m_session.UpdateCustomSetting(customSetting);

            int configId = SaveConfiguration();

            // set conflictRuleViewModel options
            m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new InvalidFieldConflictUseFieldMapAction().ReferenceName));
            ObservableDataField mapFromDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, InvalidFieldConflictUseFieldMapAction.DATAKEY_MAP_FROM));
            mapFromDataField.FieldValue = TargetField;
            ObservableDataField mapToDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, InvalidFieldConflictUseFieldMapAction.DATAKEY_MAP_TO));
            mapToDataField.FieldValue = NewTargetField;
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
    }
}
