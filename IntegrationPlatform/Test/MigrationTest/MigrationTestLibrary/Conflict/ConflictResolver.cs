// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement.Test;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace MigrationTestLibrary.Conflict
{
    public class ConflictResolver
    {
        private ConflictManagementTest m_conflictResolver;

        public ConflictResolver(Configuration config)
        {
            m_conflictResolver = new ConflictManagementTest(config.SessionGroupUniqueId);
        }

        #region public methods

        public List<RTConflict> GetConflicts()
        {
            List<RTConflict> conflictList = m_conflictResolver.GetConflicts();
            TraceManager.TraceInformation("ConflictResolver: {0} conflicts detected", conflictList.Count);

            foreach(RTConflict c in conflictList)
            {
                c.ConflictTypeReference.Load();
            }

            return conflictList;
        }

        public bool TryResolveConflict(RTConflict conflict, ResolutionAction resolutionAction, string applicableScope)
        {
            TraceManager.TraceInformation("ConflictResolver: TryResolveConflict {0} with action: {1} and scope: {2} ",
                    conflict.ConflictType.FriendlyName, resolutionAction.FriendlyName, applicableScope);

            IEnumerable<ConflictResolutionResult> resolutionResults = new List<ConflictResolutionResult>();
            return m_conflictResolver.TryResolveConflict(conflict, resolutionAction, applicableScope, out resolutionResults);
        }

        public bool TryResolveConflict(RTConflict conflict, ResolutionAction resolutionAction, string applicableScope, Dictionary<string, string> dataFields)
        {
            TraceManager.TraceInformation("ConflictResolver: TryResolveConflict {0} with action: {1} and scope: {2} ",
                conflict.ConflictType.FriendlyName, resolutionAction.FriendlyName, applicableScope);

            IEnumerable<ConflictResolutionResult> resolutionResults = new List<ConflictResolutionResult>();
            return m_conflictResolver.TryResolveConflict(conflict, resolutionAction, applicableScope, dataFields, out resolutionResults);
        }

        public bool TryResolveConflict(RTConflict conflict, Guid resolutionActionGuid, string applicableScope)
        {
            TraceManager.TraceInformation("ConflictResolver: TryResolveConflict {0} with action: {1} and scope: {2} ",
                conflict.ConflictType.FriendlyName, resolutionActionGuid, applicableScope);

            IEnumerable<ConflictResolutionResult> resolutionResults = new List<ConflictResolutionResult>();
            return m_conflictResolver.TryResolveConflict(conflict, resolutionActionGuid, applicableScope, out resolutionResults);
        }

        public bool TryResolveConflict(RTConflict conflict, Guid resolutionActionGuid, string applicableScope, Dictionary<string, string> dataFields)
        {
            TraceManager.TraceInformation("ConflictResolver: TryResolveConflict {0} with action: {1} and scope: {2} ",
                conflict.ConflictType.FriendlyName, resolutionActionGuid, applicableScope);

            IEnumerable<ConflictResolutionResult> resolutionResults = new List<ConflictResolutionResult>();
            return m_conflictResolver.TryResolveConflict(conflict, resolutionActionGuid, applicableScope, dataFields, out resolutionResults);
        }

        #endregion
    }
}
