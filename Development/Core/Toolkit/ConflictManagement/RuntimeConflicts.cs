// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement
{
    internal static class RuntimeConflicts
    {
        // runtime conflicts
        // todo: remove witGeneralConflictTypeRefName after it is deprecated and all conflicts 
        // in dogfood db is resolved and rules deleted
        private static Guid s_witGeneralConflictTypeRefName = new Guid("470F9617-FC96-4166-96EB-44CC2CF73A97");
        private static Guid s_generalConflictTypeRefName = new GenericConflictType().ReferenceName;

        internal static IQueryable<RTConflict> GetActiveAcknowledgeableConflicts(
            RuntimeEntityModel context,
            Guid sessionGroupUniqueId)
        {
            var conflictQuery =
                from c in context.RTConflictSet
                where (c.InCollection.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                    || c.InCollection.SessionRun.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId))
                && c.Status == 0 // only search for active conflicts 
                && (c.ConflictType.ReferenceName.Equals(s_witGeneralConflictTypeRefName)
                    || c.ConflictType.ReferenceName.Equals(s_generalConflictTypeRefName))
                && c.ConflictedChangeAction == null // if an action is associated, do not batch-acknowledge
                && c.ConflictedLinkChangeAction == null // if an link action is associated, do not batch-acknowledge
                select c;

            return conflictQuery;
        }

        internal static IQueryable<RTConflict> GetActiveConflicts(
            RuntimeEntityModel context,
            Guid sessionGroupUniqueId)
        {
            var conflictQuery =
                from c in context.RTConflictSet
                where (c.InCollection.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                    || c.InCollection.SessionRun.SessionGroupRun.Config.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId))
                && c.Status == 0 // only search for active conflicts 
                && (c.ConflictType.ReferenceName.Equals(s_witGeneralConflictTypeRefName)
                    || c.ConflictType.ReferenceName.Equals(s_generalConflictTypeRefName))
                select c;

            return conflictQuery;
        }

        /// <summary>
        /// Acknowledge all acknowledgeable runtime conflicts
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sessionGroupUniqueId"></param>
        /// <returns>List resolved conflicts, by Id</returns>
        internal static IEnumerable<int> AcknowledgeAllActiveConflicts(
            RuntimeEntityModel context,
            Guid sessionGroupUniqueId)
        {
            List<int> resolvedConflictIds = new List<int>();

            IQueryable<RTConflict> activeConflicts = GetActiveAcknowledgeableConflicts(context, sessionGroupUniqueId);
            foreach (RTConflict c in activeConflicts)
            {
                c.Status = 1; // 1: resolved
                resolvedConflictIds.Add(c.Id);
            }

            context.TrySaveChanges();

            return resolvedConflictIds;
        }
    }
}
