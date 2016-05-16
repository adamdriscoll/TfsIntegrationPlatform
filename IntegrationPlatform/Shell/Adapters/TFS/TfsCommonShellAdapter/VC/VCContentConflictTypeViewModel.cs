// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class VCContentConflictTypeViewModel : ConflictTypeViewModelBase
    {
        public static readonly string PathForwardDelimiter = "/";
        public static readonly string PathSemicolonDelimiter = ";";

        private ListPathsControlViewModel m_localPathsControlVM, m_otherPathsControlVM;
        private ChangesetPairControlViewModel m_changesetPairControlVM;
        public VCContentConflictTypeViewModel()
        {
            m_changesetPairControlVM = new ChangesetPairControlViewModel();
            ChangesetPairControl changesetPairControl = new ChangesetPairControl();
            changesetPairControl.DataContext = m_changesetPairControlVM;

            m_localPathsControlVM = new ListPathsControlViewModel();
            ListPathsControl localPathsControl = new ListPathsControl();
            localPathsControl.DataContext = m_localPathsControlVM;

            m_otherPathsControlVM = new ListPathsControlViewModel();
            ListPathsControl otherPathsControl = new ListPathsControl();
            otherPathsControl.DataContext = m_otherPathsControlVM;

            ConflictTypeDescription = Properties.Resources.VCContentConflictTypeDescription;

            ResolutionActionViewModel takeLocalChangesAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.TakeLocalChangesAction,
                ResolutionActionReferenceName = new VCContentConflictTakeLocalChangeAction().ReferenceName,
                UserControl = localPathsControl,
                ExecuteCommand = SetSelectedLocalPath,
                IsSelected = true
            };

            ResolutionActionViewModel takeOtherChangesAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.TakeOtherChangesAction,
                ResolutionActionReferenceName = new VCContentConflictTakeOtherChangesAction().ReferenceName,
                UserControl = otherPathsControl,
                ExecuteCommand = SetSelectedOtherPath
            };

            ResolutionActionViewModel userMergeChangesAction = new ResolutionActionViewModel()
            {
                ResolutionActionDescription = Properties.Resources.UserMergeChangesAction,
                ResolutionActionReferenceName = new VCContentConflictUserMergeChangeAction().ReferenceName,
                UserControl = changesetPairControl,
                ExecuteCommand = SetChangeSetIDs
            };
            
            RegisterResolutionAction(takeLocalChangesAction);
            RegisterResolutionAction(takeOtherChangesAction);
            RegisterResolutionAction(userMergeChangesAction);
        }

        public void SetSelectedLocalPath()
        {
            m_viewModel.Scope = m_localPathsControlVM.SelectedPath;
        }

        public void SetSelectedOtherPath()
        {
            m_viewModel.Scope = m_otherPathsControlVM.SelectedPath;
        }

        public void SetChangeSetIDs()
        {
            ObservableDataField sourceIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(
                x => string.Equals(x.FieldName, VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId));
            Debug.Assert(sourceIDDataField != null, string.Format("No DataField with key {0}", 
                VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId));
            sourceIDDataField.FieldValue = m_changesetPairControlVM.SourceID;

            ObservableDataField targetIDDataField = m_viewModel.ObservableDataFields.FirstOrDefault(
                x => string.Equals(x.FieldName, VCContentConflictUserMergeChangeAction.DeltaTableChangeId));
            Debug.Assert(targetIDDataField != null, string.Format("No DataField with key {0}",
                VCContentConflictUserMergeChangeAction.DeltaTableChangeId));
            targetIDDataField.FieldValue = m_changesetPairControlVM.TargetID;

        }

        public override void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_viewModel = viewModel;
            UserControl.DataContext = this;

            m_localPathsControlVM.Conflict = m_viewModel.MigrationConflict;
            m_otherPathsControlVM.Conflict = m_viewModel.MigrationConflict;

            UpdateResolutionActionsText();
        }

        private void UpdateResolutionActionsText()
        {
            foreach (ResolutionActionViewModel actionVM in ResolutionActions)
            {
                if (actionVM.ResolutionActionReferenceName.Equals(new VCContentConflictTakeLocalChangeAction().ReferenceName))
                {
                    actionVM.ResolutionActionDescription = GetResolutionActionDescription(m_viewModel.MigrationSource);
                }
                else if (actionVM.ResolutionActionReferenceName.Equals(new VCContentConflictTakeOtherChangesAction().ReferenceName))
                {
                    actionVM.ResolutionActionDescription = GetResolutionActionDescription(m_viewModel.MigrationOther);
                }
            }
        }

        private string GetResolutionActionDescription(string source)
        {
            string resolutionActionDescription = string.Empty;
            if (m_viewModel.MigrationConflict != null)
            {
                string scopeHint = m_viewModel.MigrationConflict.ScopeHint;

                string[] scopeSplit = scopeHint.Split(PathSemicolonDelimiter.ToCharArray());
                if (scopeSplit.Length > 1 && scopeSplit[1].Contains(PathForwardDelimiter))
                {
                    //Clearcase format
                    resolutionActionDescription = string.Format(Properties.Resources.TakeSourceChangesAction,
                        source);
                }
                else
                {
                    //Fullpath;changesetID format
                    resolutionActionDescription = string.Format(Properties.Resources.TakeSourceChangesActionWithChangeset,
                        source,
                        scopeHint);
                }
            }
            return resolutionActionDescription;
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
                        string.Format("{0} {1}", Properties.Resources.VCContentConflictDetails,
                        m_viewModel.MigrationConflict.ScopeHint));
                }
                return m_details;
            }
        }
    }
}