// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class VCContentConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (rule.ActionRefNameGuid.Equals(new VCContentConflictUserMergeChangeAction().ReferenceName))
            {
                if (!conflict.ScopeHint.Contains(';'))
                {
                    TraceManager.TraceWarning("Manual resolution rule of content conflict must be change group specific");
                    return false;
                }
            }
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            long otherSideConflictActionId;

            if (rule.ActionRefNameGuid.Equals(new VCContentConflictTakeLocalChangeAction().ReferenceName))
            {
                // left-> right migration. User select take local (right). So we skip left side conflict action(the original conflict action).
                if (!markChangeActionAsSkipped(conflict.ConflictedChangeAction.ActionId))
                {
                    TraceManager.TraceError("Cannot mark conflict action {0} as skipped", conflict.ConflictedChangeAction.ActionId);
                    return new ConflictResolutionResult(false, ConflictResolutionType.SkipConflictedChangeAction);
                }
                return new ConflictResolutionResult(true, ConflictResolutionType.SkipConflictedChangeAction);
            }
            else if (rule.ActionRefNameGuid.Equals(new VCContentConflictTakeOtherChangesAction().ReferenceName))
            {
                // left-> right migration. User select take other (left). So we skip right side conflict action(the other side's conflict action).
                if (!long.TryParse(conflict.ConflictDetails, out otherSideConflictActionId))
                {
                    TraceManager.TraceError("Cannot find other side's conflict action {0}", conflict.ConflictDetails);
                    return new ConflictResolutionResult(false, ConflictResolutionType.SuppressOtherSideChangeAction);
                }
                if (!markChangeActionAsSkipped(otherSideConflictActionId))
                {
                    TraceManager.TraceError("Cannot mark conflict action {0} as skipped", conflict.ConflictDetails);
                    return new ConflictResolutionResult(false, ConflictResolutionType.SuppressOtherSideChangeAction);
                }
                return new ConflictResolutionResult(true, ConflictResolutionType.SuppressOtherSideChangeAction);
            }
            else if (rule.ActionRefNameGuid.Equals(new VCContentConflictUserMergeChangeAction().ReferenceName))
            {
                if (updateConversionHistory(conflict, rule))
                {
                    return new ConflictResolutionResult(true, ConflictResolutionType.SuppressedConflictedChangeGroup);
                }
                else
                {
                    return new ConflictResolutionResult(false, ConflictResolutionType.Other);
                }
            }
            else
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.UnknownResolutionAction);
            }
        }

        /// <summary>
        /// Mark the action state IsSubstituted (skipped) as true.
        /// </summary>
        /// <param name="actionId"></param>
        /// <returns></returns>
        private bool markChangeActionAsSkipped(long actionId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var actionTableEntries =
                    from a in context.RTChangeActionSet
                    where a.ChangeActionId == actionId
                    select a;
                if (actionTableEntries.Count() > 0)
                {
                    actionTableEntries.First().IsSubstituted = true;
                    context.TrySaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool updateConversionHistory(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (!rule.DataFieldDictionary.ContainsKey(VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId) ||
                !rule.DataFieldDictionary.ContainsKey(VCContentConflictUserMergeChangeAction.DeltaTableChangeId))
            {
                return false;
            }
            string migrationInstructionName = rule.DataFieldDictionary[VCContentConflictUserMergeChangeAction.MigrationInstructionChangeId];
            string deltaTableName = rule.DataFieldDictionary[VCContentConflictUserMergeChangeAction.DeltaTableChangeId];

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                ChangeGroup conflictChangeGroup = conflict.ConflictedChangeAction.ChangeGroup;
                Guid deltaSideSourceId;
                
                // Mark all delta table entry as DeltaComplete
                var deltaTableEntries =
                    from d in context.RTChangeGroupSet
                    where d.SessionUniqueId == conflictChangeGroup.SessionId
                    && d.Status == (int)ChangeStatus.DeltaPending
                    select d;
                foreach (RTChangeGroup deltaTableEntry in deltaTableEntries)
                {
                    deltaTableEntry.Status = (int)ChangeStatus.DeltaComplete;
                    deltaTableEntry.ContainsBackloggedAction = false;
                }

                // Mark all migration instruction entry as Complete
                var migrationInstructionEntries =
                    from d in context.RTChangeGroupSet
                    where d.SessionUniqueId == conflictChangeGroup.SessionId
                    && (d.Status == (int)ChangeStatus.Pending || d.Status == (int)ChangeStatus.InProgress || d.Status == (int)ChangeStatus.PendingConflictDetection)
                    select d;
                foreach (RTChangeGroup migrationInstructionEntry in migrationInstructionEntries)
                {
                    migrationInstructionEntry.Status = (int)ChangeStatus.Complete;
                    migrationInstructionEntry.ContainsBackloggedAction = false;
                }

                // Mark the source side highwatermark
                var sourceSideHighWaterMark =
                    (from hwm in context.RTHighWaterMarkSet
                     where hwm.SessionUniqueId == conflictChangeGroup.SessionId
                     && hwm.SourceUniqueId != conflictChangeGroup.SourceId
                     && hwm.Name == Constants.HwmDelta
                     select hwm).First();
                Debug.Assert(sourceSideHighWaterMark != null, "Can't find the source side HWM");

                sourceSideHighWaterMark.Value = deltaTableName;
                deltaSideSourceId = sourceSideHighWaterMark.SourceUniqueId;                    

                // Mark the target side highwatermark
                var targetHighWaterMark =
                    (from hwm in context.RTHighWaterMarkSet
                     where hwm.SessionUniqueId == conflictChangeGroup.SessionId
                     && hwm.SourceUniqueId == conflictChangeGroup.SourceId
                     && hwm.Name == Constants.HwmDelta
                     select hwm).First();
                Debug.Assert(targetHighWaterMark != null, "Can't find the target side HWM");

                targetHighWaterMark.Value = migrationInstructionName;

                // Create the conversion history entry
                RTConversionHistory conversionHistory = RTConversionHistory.CreateRTConversionHistory(
                        DateTime.UtcNow,
                        -1,
                        true);
                conversionHistory.Comment = rule.RuleDescription;

                var session =
                    (from s in context.RTSessionConfigSet
                    where s.SessionUniqueId == conflictChangeGroup.SessionId
                    select s).First();

                Debug.Assert(session != null, "Cannot find session in DB");

                RTSessionRun sessionRun =
                    (from sr in context.RTSessionRunSet
                     where sr.Id == session.Id
                     select sr).First();
                Debug.Assert(sessionRun != null, "Cannot find session run in DB");

                conversionHistory.SessionRun = sessionRun;

                RTMigrationSource migrationSource =
                    (from ms in context.RTMigrationSourceSet
                     where ms.UniqueId.Equals(conflictChangeGroup.SourceId)
                     select ms).First();
                Debug.Assert(migrationSource != null, "Cannot find the migration source to persist conversion history");

                RTMigrationSource deltaSideMigrationSource =
                    (from ms in context.RTMigrationSourceSet
                     where ms.UniqueId.Equals(deltaSideSourceId)
                     select ms).First();
                Debug.Assert(deltaSideMigrationSource != null, "Cannot find the migration source to persist conversion history");

                conversionHistory.SourceMigrationSource = migrationSource;

                context.AddToRTConversionHistorySet(conversionHistory);

                RTMigrationItem sourceItem = RTMigrationItem.CreateRTMigrationItem(0, deltaTableName, Constants.ChangeGroupGenericVersionNumber);
                sourceItem.MigrationSource = migrationSource;
                RTMigrationItem targetItem = RTMigrationItem.CreateRTMigrationItem(0, migrationInstructionName, Constants.ChangeGroupGenericVersionNumber);
                targetItem.MigrationSource = deltaSideMigrationSource;


                RTItemRevisionPair pair = RTItemRevisionPair.CreateRTItemRevisionPair(
                    sourceItem.Id, targetItem.Id);
                pair.LeftMigrationItem = sourceItem;
                pair.RightMigrationItem = targetItem;
                pair.ConversionHistory = conversionHistory;



                // Create a new HistoryNotFoundConflict Resolution Rule
                context.TrySaveChanges();
            }
            return true;
        }

        private bool readyToManualResolve(MigrationConflict conflict)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var changeGroups =
                    from c in context.RTChangeGroupSet
                    where c.ExecutionOrder < conflict.ConflictedChangeAction.ChangeGroup.ExecutionOrder
                        && (c.Status == (int)ChangeStatus.Pending || c.Status == (int)ChangeStatus.InProgress)
                    select c;

                if (changeGroups.Count() > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }            
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
    }
}
