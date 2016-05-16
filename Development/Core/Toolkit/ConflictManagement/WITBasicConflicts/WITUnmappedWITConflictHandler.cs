// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement.WITBasicConflicts
{
    internal class WITUnmappedWITConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Other);
            }
            else if (rule.ActionRefNameGuid.Equals(new WITUnmappedWITConflictUpdateWITMappingAction().ReferenceName))
            {
                return ResolveByUsingWITMapping(conflict, rule);
            }
            else if (rule.ActionRefNameGuid.Equals(new WITUnmappedWITConflictExcludeWITInSessionFilter().ReferenceName) ||
                     rule.ActionRefNameGuid.Equals(new SkipConflictedActionResolutionAction().ReferenceName))
            {
                conflict.ConflictedChangeAction.ChangeGroup.Status = ChangeStatus.Skipped;
                conflict.ConflictedChangeAction.State = ActionState.Skipped;
                return new ConflictResolutionResult(true, ConflictResolutionType.SkipConflictedChangeAction);
            }
            else
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
        }

        private ConflictResolutionResult ResolveByUsingWITMapping(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            string mapToWIT;
            if (rule.DataFieldDictionary.TryGetValue(WITUnmappedWITConflictUpdateWITMappingAction.DATAKEY_MAP_TO, out mapToWIT))
            {
                try
                {
                    Debug.Assert(conflict.ConflictedChangeAction != null, "conflict.ConflictedChangeAction is NULL");
                    XmlDocument updateDoc = conflict.ConflictedChangeAction.MigrationActionDescription;
                    updateDoc.DocumentElement.Attributes["WorkItemType"].Value = mapToWIT;

                    return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
                }
                catch (Exception e)
                {
                    TraceManager.TraceException(e);
                }
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
    }
}
