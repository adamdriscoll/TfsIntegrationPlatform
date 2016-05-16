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
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.WIT
{
    /// <summary>
    /// Interaction logic for UnmappedWorkItemTypeCustomControl.xaml
    /// </summary>
    public partial class UnmappedWorkItemTypeCustomControl : UserControl, IConflictTypeUserControl
    {
        private ConflictRuleViewModel m_conflictRuleViewModel;

        public UnmappedWorkItemTypeCustomControl()
        {
            InitializeComponent();
        }

        private SerializableCustomSettings m_customSettings;
        private string m_oldCustomSettings;
        private WITUnmappedWITConflictType m_conflictType = new WITUnmappedWITConflictType();

        #region IConflictTypeUserControl Members

        private Dictionary<string, FrameworkElement> m_details;
        public Dictionary<string, FrameworkElement> Details
        {
            get
            {
                if (m_details == null)
                {
                    m_details = new Dictionary<string, FrameworkElement>();

                    m_details["Details"] = this.Resources["Details"] as TextBlock;
                    m_details["Details"].DataContext = m_conflictRuleViewModel;
                }
                return m_details;
            }
        }

        public void Save()
        {
            Configuration config = m_conflictRuleViewModel.AppViewModel.Config;

            if ((bool)updateConfigRadioButton.IsChecked)
            {
                // save config
                config.UniqueId = Guid.NewGuid().ToString();
                SessionGroupConfigurationManager saver = new SessionGroupConfigurationManager(config);
                int configId = saver.TrySave();

                // set rule
                m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new WITUnmappedWITConflictUpdateWITMappingAction().ReferenceName));
                ObservableDataField updatedConfigIdDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, Constants.DATAKEY_UPDATED_CONFIGURATION_ID));
                updatedConfigIdDataField.FieldValue = configId.ToString();
                ObservableDataField mapToDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, WITUnmappedWITConflictUpdateWITMappingAction.DATAKEY_MAP_TO));
                mapToDataField.FieldValue = newWorkItemTypeTextBox.Text;
            }
            else if ((bool)newValueRadioButton.IsChecked)
            {
                // undo config changes
                m_customSettings.SerializedContent = m_oldCustomSettings;
                m_customSettings.Save();

                // select resolution action
                m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new WITUnmappedWITConflictExcludeWITInSessionFilter().ReferenceName));

                // update config
                m_filterItem.FilterString = newValueTextBox.Text;

                // save config
                config.UniqueId = Guid.NewGuid().ToString();
                SessionGroupConfigurationManager saver = new SessionGroupConfigurationManager(config);
                int configId = saver.TrySave();

                // set data fields
                ObservableDataField updatedConfigIdDataField = m_conflictRuleViewModel.ObservableDataFields.First(x => string.Equals(x.FieldName, Constants.DATAKEY_UPDATED_CONFIGURATION_ID));
                updatedConfigIdDataField.FieldValue = configId.ToString();
            }
            else if ((bool)retryRadioButton.IsChecked)
            {
                // undo config changes
                m_customSettings.SerializedContent = m_oldCustomSettings;
                m_customSettings.Save();

                // select resolution action
                m_conflictRuleViewModel.SelectedResolutionAction = m_conflictRuleViewModel.ResolutionActions.First(x => x.ReferenceName.Equals(new ManualConflictResolutionAction().ReferenceName));
            }

            m_conflictRuleViewModel.AppViewModel.ShellViewModel.RefreshConfigViewModel();
        }

        public UserControl UserControl
        {
            get
            {
                return this;
            }
        }

        private FilterItem m_filterItem;
        public void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_conflictRuleViewModel = viewModel;

            Configuration config = m_conflictRuleViewModel.AppViewModel.Config;
            Session session = config.SessionGroup.Sessions.Session.First(x => m_conflictRuleViewModel.RTConflict.ScopeId.Equals(new Guid(x.SessionUniqueId)));
            m_customSettings = new SerializableCustomSettings(session);
            customSettingsView.DataContext = m_customSettings;
            m_oldCustomSettings = m_customSettings.Serialize();

            Debug.Assert(session.Filters.FilterPair.Count() == 1, string.Format("More than one filter pair exists for session {0}", session.FriendlyName));
            FilterPair filterPair = session.Filters.FilterPair.First();
            m_filterItem = filterPair.FilterItem.FirstOrDefault(x => m_conflictRuleViewModel.SourceId.Equals(new Guid(x.MigrationSourceUniqueId)));
            Debug.Assert(m_filterItem != null, string.Format("FilterItem could not be found for migration source {0}", m_conflictRuleViewModel.SourceId));
            newValueTextBox.Text = m_filterItem.FilterString;
        }

        #endregion
    }
}
