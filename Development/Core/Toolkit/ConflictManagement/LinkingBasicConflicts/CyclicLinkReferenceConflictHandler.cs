// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement
{
    public class CyclicLinkReferenceConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(
            MigrationConflict conflict, 
            ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(
            IServiceContainer serviceContainer, 
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            Debug.Assert(!string.IsNullOrEmpty(conflict.ConflictDetails));
            Debug.Assert(null != conflict.ConflictedLinkChangeAction);

            actions = null;

            long linkChangeActionId = GetLinkActionInternalId(conflict);
            if (rule.ActionRefNameGuid.Equals(new CyclicLinkReferenceConflictDropLinkFromSource().ReferenceName))
            {                
                conflict.ConflictedLinkChangeAction.Status = LinkChangeAction.LinkChangeActionStatus.Skipped;
                return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedLinkChangeAction);
            }

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Other);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }           

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        private long GetLinkActionInternalId(MigrationConflict conflict)
        {
            long linkChangeActionID;
            if (!long.TryParse(conflict.ConflictDetails, out linkChangeActionID))
            {
                throw new ArgumentException(MigrationToolkitResources.InvalidConflictDescription);
            }

            Debug.Assert(linkChangeActionID != LinkChangeAction.INVALID_INTERNAL_ID);
            return linkChangeActionID;
        }

        #endregion
    }
}
