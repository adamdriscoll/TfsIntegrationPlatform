// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement.Test
{
    public class ConflictManagementTest
    {
        private ApplicationViewModel m_model;
        private Guid m_sessionGroupUniqueId;
        public ConflictManagementTest(Guid sessionGroupUniqueId)
        {
            m_sessionGroupUniqueId = sessionGroupUniqueId;
            m_model = new ApplicationViewModel();
            m_model.SetSessionGroupUniqueId(m_sessionGroupUniqueId, true);
            while (m_model.IsBusy)
            {
                Thread.Sleep(1000);
            }
        }

        // Returns a list of conflicts associated with a current configuration
        public List<RTConflict> GetConflicts()
        {
            return new List<RTConflict>(m_model.GetActiveConflicts(m_sessionGroupUniqueId));
        }

        // Resolves the given conflict with the provided resolution action and scope
        public bool TryResolveConflict(RTConflict conflict, ResolutionAction resolutionAction, string applicableScope, out IEnumerable<ConflictResolutionResult> resolutionResults)
        {
            ConflictRuleViewModel conflictModel = new ConflictRuleViewModel(conflict, m_model);
            conflictModel.SelectedResolutionAction = resolutionAction;
            conflictModel.Scope = applicableScope;
            resolutionResults = conflictModel.Save();

            return resolutionResults.Where(x => !x.Resolved).Count() == 0;
        }

        // Resolves the given conflict with the provided resolution action ,scope and data fields
        public bool TryResolveConflict(RTConflict conflict, ResolutionAction resolutionAction, string applicableScope, Dictionary<string, string> dataFields, out IEnumerable<ConflictResolutionResult> resolutionResults)
        {
            ConflictRuleViewModel conflictModel = new ConflictRuleViewModel(conflict, m_model);
            conflictModel.SelectedResolutionAction = resolutionAction;
            conflictModel.Scope = applicableScope;
            foreach (ObservableDataField dataField in conflictModel.ObservableDataFields)
            {
                dataField.FieldValue = dataFields[dataField.FieldName];
            }
            resolutionResults = conflictModel.Save();

            return resolutionResults.Where(x => !x.Resolved).Count() == 0;
        }

        // Resolves the given conflict with the provided resolution action GUID and scope
        public bool TryResolveConflict(RTConflict conflict, Guid resolutionActionGuid, string applicableScope, out IEnumerable<ConflictResolutionResult> resolutionResults)
        {
            ConflictRuleViewModel conflictModel = new ConflictRuleViewModel(conflict, m_model);
            conflictModel.SelectedResolutionAction = conflictModel.ResolutionActions.Where(x=>x.ReferenceName.Equals(resolutionActionGuid)).Single();
            conflictModel.Scope = applicableScope;
            resolutionResults = conflictModel.Save();

            return resolutionResults.Where(x => !x.Resolved).Count() == 0;
        }

        // Resolves the given conflict with the provided resolution action GUID ,scope and data fields
        public bool TryResolveConflict(RTConflict conflict, Guid resolutionActionGuid, string applicableScope, Dictionary<string, string> dataFields, out IEnumerable<ConflictResolutionResult> resolutionResults)
        {
            ConflictRuleViewModel conflictModel = new ConflictRuleViewModel(conflict, m_model);
            conflictModel.SelectedResolutionAction = conflictModel.ResolutionActions.Where(x => x.ReferenceName.Equals(resolutionActionGuid)).Single();
            conflictModel.Scope = applicableScope;
            foreach (ObservableDataField dataField in conflictModel.ObservableDataFields)
            {
                dataField.FieldValue = dataFields[dataField.FieldName];
            }
            resolutionResults = conflictModel.Save();

            return resolutionResults.Where(x => !x.Resolved).Count() == 0;
        }
    }
}
