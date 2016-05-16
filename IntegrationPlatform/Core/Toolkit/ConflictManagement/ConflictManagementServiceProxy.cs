// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static class ConflictManagementServiceProxy
    {
        public static class ResolutionRule
        {
            // TODO: 
            //   1. move to ConflictManagementServiceProxy
            public static void ObsoleteResolutionRule(ConflictResolutionRule ruleToObsolete)
            {
                if (ruleToObsolete == null)
                {
                    throw new ArgumentNullException("ruleToObsolete");
                }

                if (ruleToObsolete.RuleRefNameGuid.Equals(Guid.Empty))
                {
                    throw new ArgumentException("ruleToObsolete.RuleRefNameGuid is Empty");
                }

                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    Guid ruleRefName = ruleToObsolete.RuleRefNameGuid;
                    var ruleQuery = context.RTResolutionRuleSet.Where
                        (r => r.ReferenceName.Equals(ruleRefName));

                    if (ruleQuery.Count<RTResolutionRule>() == 0)
                    {
                        throw new ConflictManagementGeneralException(
                            MigrationToolkitResources.ErrorResolutionRuleNotFound,
                            ruleToObsolete.RuleRefNameGuid.ToString());
                    }

                    if (ruleQuery.Count() > 1)
                    {
                        throw new ConflictManagementGeneralException(
                            MigrationToolkitResources.ErrorMultiResolutionRuleOfSameRefName,
                            ruleToObsolete.RuleRefNameGuid.ToString());
                    }

                    ruleQuery.First().Status = ConflictResolutionRuleState.Deprecated.StorageValue;
                    context.TrySaveChanges();
                }
            }
        }

        public static class RuntimeErrors
        {
            /// <summary>
            /// Acknowledge all the active runtime conflicts of a particular session group.
            /// </summary>
            /// <param name="sessionGroupUniqueId">The unique Id of the session group, to which the conflicts belong</param>
            public static IEnumerable<int> AcknowledgeAllActiveRuntimeConflicts(
                Guid sessionGroupUniqueId)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    return RuntimeConflicts.AcknowledgeAllActiveConflicts(context, sessionGroupUniqueId);
                }
            }

            /// <summary>
            /// Get all the active acknowledgeable runtime conflicts of a particular session group
            /// </summary>
            /// <param name="context">The RuntimeEntityModel instance to be used to query the conflicts</param>
            /// <param name="sessionGroupUniqueId">The unique Id of the session group, for which the runtime conflicts are created</param>
            /// <returns>A queryable collection of the active acknowledgeable runtime conflicts</returns>
            public static IQueryable<RTConflict> GetAcknowledgeableActiveRuntimeConflicts(
                RuntimeEntityModel context,
                Guid sessionGroupUniqueId)
            {
                return RuntimeConflicts.GetActiveAcknowledgeableConflicts(context, sessionGroupUniqueId);
            }

            /// <summary>
            /// Get the total number of the active acknowledgeable runtime conflicts of a particular session group
            /// </summary>
            /// <param name="sessionGroupUniqueId">The unique Id of the session group, for which the runtime conflicts are created</param>
            /// <returns>The total number of all the active acknowledgeable runtime conflicts</returns>
            public static int GetAcknowledgeableActiveRuntimeConflictsCount(
                Guid sessionGroupUniqueId)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    return RuntimeConflicts.GetActiveAcknowledgeableConflicts(context, sessionGroupUniqueId).Count();
                }
            }

            /// <summary>
            /// Get all the active runtime conflicts of a particular session group
            /// </summary>
            /// <param name="sessionGroupUniqueId">The unique Id of the session group, for which the runtime conflicts are created</param>
            /// <returns>A queryable collection of all the active runtime conflicts</returns>
            public static IQueryable<RTConflict> GetAllActiveRuntimeConflicts(
                RuntimeEntityModel context,
                Guid sessionGroupUniqueId)
            {
                return RuntimeConflicts.GetActiveConflicts(context, sessionGroupUniqueId);
            }

            /// <summary>
            /// Get the total number of the active runtime conflicts of a particular session group
            /// </summary>
            /// <param name="sessionGroupUniqueId">The unique Id of the session group, for which the runtime conflicts are created</param>
            /// <returns>The total number of the active runtime conflicts</returns>
            public static int GetAllActiveRuntimeConflictsCount(
                Guid sessionGroupUniqueId)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    return RuntimeConflicts.GetActiveConflicts(context, sessionGroupUniqueId).Count();
                }
            }
        }

        public static class Conflicts
        {
            /// <summary>
            /// Select the active conflicts that can be resolved by a rule with certain scope.
            /// </summary>
            /// <param name="conflicts"></param>
            /// <param name="conflictType"></param>
            /// <param name="ruleScope"></param>
            /// <returns></returns>
            public static MigrationConflict[] GetResolvableConflictListByScope(
                MigrationConflict[] conflicts,
                ConflictType conflictType,
                string ruleScope)
            {
                List<MigrationConflict> resolvableConflicts = new List<MigrationConflict>(conflicts.Length);

                string hint;
                if (IsResolutionRuleScopeValid(conflictType, ruleScope, out hint))
                {
                    foreach (MigrationConflict conflict in conflicts)
                    {
                        // filter out incompatible conflicts (those of other types)
                        if (!conflict.ConflictType.ReferenceName.Equals(conflictType.ReferenceName))
                        {
                            continue;
                        }

                        // select those whose ScopeHint is in 'ruleScope'
                        if (conflictType.ScopeInterpreter.IsInScope(conflict.ScopeHint, ruleScope))
                        {
                            resolvableConflicts.Add(conflict);
                        }
                    }
                }

                return resolvableConflicts.ToArray();
            }

            public static bool IsResolutionRuleScopeValid(
                ConflictType conflictType,
                string ruleScopeToValidate,
                out string hint)
            {
                return conflictType.ScopeInterpreter.IsResolutionRuleScopeValid(ruleScopeToValidate, out hint);
            }
        }
    }
}
