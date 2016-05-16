// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class SqlChangeGroupManager : ChangeGroupManager
    {
        Dictionary<string, string> m_conversionHistory = new Dictionary<string, string>();
        Dictionary<string, bool> m_conversionHistoryContentChanged = new Dictionary<string, bool>();
        int m_cacheSize = 10000;

        public SqlChangeGroupManager(
            Session session,
            Guid sourceId)
            : base(session, sourceId)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }
        }

        internal override void Initialize(int sessionRunStorageId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RuntimeSessionRun =
                    (from sr in context.RTSessionRunSet
                     where sr.Id == sessionRunStorageId
                     select sr).First();
                Debug.Assert(RuntimeSessionRun != null, "Cannot find session run in DB");
                context.Detach(RuntimeSessionRun);

                RunTimeMigrationSource = context.LoadMigrationSources(SourceId).First();
                Debug.Assert(RuntimeSessionRun != null, "Cannot find migration source in DB");
                context.Detach(RunTimeMigrationSource);
            }
        }

        //todo: the following code is not thread safe
        internal RTMigrationSource RunTimeMigrationSource
        {
            get;
            private set;
        }

        internal RTSessionRun RuntimeSessionRun
        {
            get;
            private set;
        }

        public override ChangeGroup Next()
        {
            return SqlChangeGroup.Next(this);
        }

        public override int DemoteInProgressActionsToPending()
        {
            RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
            context.BatchUpdateChangeGroupsStatus(
                new Guid(Session.SessionUniqueId), 
                SourceId, 
                (int)ChangeStatus.InProgress, 
                (int)ChangeStatus.Pending);

            /*context.PromoteChangeGroups(
                new Guid(Session.SessionUniqueId), 
                SourceId, 
                (int)ChangeStatus.InProgress, 
                (int)ChangeStatus.Pending);*/
            // ToDo
            return 0;
        }

        public override int PromoteAnalysisToPending()
        {
            return SqlChangeGroup.PromoteAnalysisToPending(Session.SessionUniqueId, SourceId);
        }

        public override void PromoteDeltaToPending()
        {
            RuntimeEntityModel context = RuntimeEntityModel.CreateInstance();
            context.PromoteChangeGroups(
                new Guid(Session.SessionUniqueId), 
                SourceId, 
                (int)ChangeStatus.Delta, 
                (int)ChangeStatus.DeltaPending);
        }

        public override void RemoveIncompleteChangeGroups()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                context.DeleteIncompeleteChangeGroups(new Guid(Session.SessionUniqueId), SourceId);
            }
        }

        public override int NumOfDeltaTableEntries()
        {
            Guid sessionUniqueId = new Guid(Session.SessionUniqueId);
            
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var statusVal = (int)ChangeStatus.DeltaPending;
                return 
                    (from g in context.RTChangeGroupSet
                     where g.SessionUniqueId.Equals(sessionUniqueId)
                     && g.SourceUniqueId.Equals(SourceId)
                     && g.Status == statusVal
                     && g.ContainsBackloggedAction == false
                     select g.Id).Count();
            }
        }

        public override IEnumerable<ChangeGroup> NextDeltaTablePage(int pageNumber, int pageSize, bool includeConflicts)
        {
            Guid sessionUniqueId = new Guid(Session.SessionUniqueId);
            return GetChangeGroupTablePageByStatus(pageNumber, pageSize, sessionUniqueId, ChangeStatus.DeltaPending, false, includeConflicts);            
        }

        public override IMigrationAction LoadSingleAction(long actionInternalId)
        {
            IMigrationAction retAction = null;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Guid sessionUniqueId = new Guid(Session.SessionUniqueId);
                var actions =
                    (from ca in context.RTChangeActionSet
                     where ca.ChangeActionId == actionInternalId
                     select ca);

                if (actions.Count() > 0)
                {
                    RTChangeAction rtChangeAction = actions.First();
                    rtChangeAction.ChangeGroupReference.Load();
                    RTChangeGroup parentRTGroup = rtChangeAction.ChangeGroup;

                    SqlChangeGroup parentChangeGroup = new SqlChangeGroup(this);
                    retAction = parentChangeGroup.RealizeFromEDMWithSingleAction(parentRTGroup, rtChangeAction);
                }
            }

            return retAction;
        }

        internal List<IMigrationAction> LoadPagedActions(IEnumerable<RTChangeAction> rtChangeActions)
        {
            List<IMigrationAction> realizedMigrationActions = new List<IMigrationAction>();

            SqlChangeGroup parentChangeGroup = null;
            RTChangeGroup parentRTGroup = null;

            foreach (RTChangeAction rtChangeAction in rtChangeActions)
            {
                if (parentRTGroup == null)
                {
                    parentChangeGroup = new SqlChangeGroup(this);
                    rtChangeAction.ChangeGroupReference.Load();
                    parentRTGroup = rtChangeAction.ChangeGroup;
                }

                IMigrationAction migrationAction = parentChangeGroup.RealizeFromEDMWithSingleAction(parentRTGroup, rtChangeAction);
                
                if (null != migrationAction)
                {
                    realizedMigrationActions.Add(migrationAction);
                }
            }

            return realizedMigrationActions;
        }

        /// <summary>
        /// Get the counts of in-progress migration instructions that are translated from the other side.
        /// </summary>
        /// <returns></returns>
        public override int GetInProgressMigrationInstructionCount()
        {
            Guid sessionUniqueId = new Guid(Session.SessionUniqueId);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                IQueryable<RTChangeGroup> query;
                query = (from cg in context.RTChangeGroupSet
                         where cg.SessionUniqueId.Equals(sessionUniqueId) &&
                         ( (cg.SourceUniqueId.Equals(SourceId) && (cg.Status == (int)ChangeStatus.InProgress || cg.Status == (int)ChangeStatus.Pending 
                          || cg.Status == (int)ChangeStatus.PendingConflictDetection || cg.Status == (int)ChangeStatus.AnalysisMigrationInstruction)))
                         select cg);
                return query.Count();
            }
        }

        /// <summary>
        /// Remove all change groups in current session which are in progress. 
        /// For delta table, this includes status of Delta, DeltaPending.
        /// For migration instruction, this includes Pending or Inprogress.
        /// </summary>
        public override void RemoveInProgressChangeGroups()
        {
            Guid sessionUniqueId = new Guid(Session.SessionUniqueId);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                IQueryable<RTChangeGroup> query;

                query = (from cg in context.RTChangeGroupSet
                         where cg.SessionUniqueId.Equals(sessionUniqueId) &&
                         ((cg.SourceUniqueId.Equals(SourceId) && (cg.Status == (int)ChangeStatus.Delta || cg.Status == (int)ChangeStatus.DeltaPending))
                          || (cg.SourceUniqueId.Equals(OtherSideChangeGroupManager.SourceId) &&
                          (cg.Status == (int)ChangeStatus.InProgress || cg.Status == (int)ChangeStatus.Pending
                          || cg.Status == (int)ChangeStatus.PendingConflictDetection || cg.Status == (int)ChangeStatus.AnalysisMigrationInstruction)))
                         select cg);
                foreach (RTChangeGroup rtChangeGroup in query)
                {
                    rtChangeGroup.Status = (int)ChangeStatus.Obsolete;
                    TraceManager.TraceInformation("In-progress changegroup {0} will be marked as obselete", rtChangeGroup.Id);
                }
                context.TrySaveChanges();
            }
        }

        public override IEnumerable<ChangeGroup> NextMigrationInstructionTablePage(
            int pageNumber, 
            int pageSize,
            bool isInConflictDetectionState,
            bool includeGroupInBacklog)
        {
            Guid sessionUniqueId = new Guid(Session.SessionUniqueId);
            int pendingDetection = (int)ChangeStatus.PendingConflictDetection;
            int pending = (int)ChangeStatus.Pending;

            List<ChangeGroup> deltaTableEntries = new List<ChangeGroup>();

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                IQueryable<RTChangeGroup> query;
                if (includeGroupInBacklog)
                {
                    if (isInConflictDetectionState)
                    {
                        query = (from cg in context.RTChangeGroupSet
                                 where cg.SessionUniqueId.Equals(sessionUniqueId)
                                 && cg.SourceUniqueId.Equals(SourceId)
                                 && (cg.Status == pendingDetection || cg.Status == pending)
                                 orderby cg.Id
                                 select cg).Skip(pageNumber * pageSize).Take(pageSize);
                    }
                    else
                    {
                        query = (from cg in context.RTChangeGroupSet
                                 where cg.SessionUniqueId.Equals(sessionUniqueId)
                                 && cg.SourceUniqueId.Equals(SourceId)
                                 && cg.Status == pending
                                 orderby cg.Id
                                 select cg).Skip(pageNumber * pageSize).Take(pageSize);
                    }
                }
                else
                {
                    if (isInConflictDetectionState)
                    {
                        query = (from cg in context.RTChangeGroupSet
                                 where cg.SessionUniqueId.Equals(sessionUniqueId)
                                 && cg.SourceUniqueId.Equals(SourceId)
                                 && (cg.Status == pendingDetection || cg.Status == pending)
                                 && !cg.ContainsBackloggedAction
                                 orderby cg.Id
                                 select cg).Skip(pageNumber * pageSize).Take(pageSize);
                    }
                    else
                    {
                        query = (from cg in context.RTChangeGroupSet
                                 where cg.SessionUniqueId.Equals(sessionUniqueId)
                                 && cg.SourceUniqueId.Equals(SourceId)
                                 && cg.Status == pending
                                 && !cg.ContainsBackloggedAction
                                 orderby cg.Id
                                 select cg).Skip(pageNumber * pageSize).Take(pageSize);
                    }
                }

                foreach (RTChangeGroup rtChangeGroup in query)
                {
                    SqlChangeGroup changeGroup = new SqlChangeGroup(this);
                    changeGroup.UseOtherSideMigrationItemSerializers = true;
                    changeGroup.RealizeFromEDM(rtChangeGroup);
                    deltaTableEntries.Add(changeGroup);
                }
            }

            return deltaTableEntries;
        }

        public override void BatchMarkDeltaTableEntriesAsDeltaCompleted()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Guid sessionUniqueId = new Guid(Session.SessionUniqueId);
                context.BatchUpdateChangeGroupsStatus(sessionUniqueId, SourceId, (int)ChangeStatus.DeltaPending, (int)ChangeStatus.DeltaComplete);
            } 
        }

        public override void BatchMarkMigrationInstructionsAsPending()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Guid sessionUniqueId = new Guid(Session.SessionUniqueId);
                context.BatchUpdateChangeGroupsStatus(sessionUniqueId, SourceId, (int)ChangeStatus.PendingConflictDetection, (int)ChangeStatus.Pending);
            }
        }

        internal override int DemoteInProgressActionsToPending(IMigrationTransaction trx)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                //context.PromoteChangeGroups(new Guid(Session.SessionUniqueId), SourceId, (int)ChangeStatus.InProgress, (int)ChangeStatus.Pending);
                context.BatchUpdateChangeGroupsStatus(new Guid(Session.SessionUniqueId), SourceId, (int)ChangeStatus.InProgress, (int)ChangeStatus.Pending);
                // ToDo Bugbug, need to return the real value
            }
            return 0;
        }

        internal override int PromoteAnalysisToPending(IMigrationTransaction trx)
        {
            return SqlChangeGroup.PromoteAnalysisToPending(trx, Session.SessionUniqueId, SourceId);
        }

        public override ChangeGroup Create(string groupName)
        {
            ChangeGroup group = new SqlChangeGroup(this, groupName, ChangeStatus.AnalysisMigrationInstruction);
            return group;
        }

        public override ChangeGroup CreateForDeltaTable(string groupName)
        {
            ChangeGroup group = new SqlChangeGroup(this, groupName, ChangeStatus.Delta);
            return group;
        }


        public override void BatchUpdateStatus(ChangeGroup[] changeGroups)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                foreach (SqlChangeGroup sqlChangeGroup in changeGroups)
                {
                    sqlChangeGroup.PersistCurrentStatus(context);
                }
                context.TrySaveChanges();
            }
        }

        public override string GetChangeIdFromConversionHistory(string id, Guid peerSourceId, out bool contentChanged)
        {
            if (m_conversionHistory.ContainsKey(id))
            {
                contentChanged = m_conversionHistoryContentChanged[id];
                return m_conversionHistory[id];
            }
            contentChanged = false;
            string targetItemId = string.Empty;

            Guid sessionId = new Guid(Session.SessionUniqueId);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                IQueryable<RTMigrationItem> migrationItemResult;
                if (String.Equals(id, Constants.TfsVCLatestVersionSpec, StringComparison.OrdinalIgnoreCase))
                {
                    // If they passed in VersionSpec.Latest for the id then get the latest changeset on the target
                    // side from the conversion history
                    migrationItemResult =
                        (from mi in context.RTMigrationItemSet
                         where mi.MigrationSource.UniqueId.Equals(peerSourceId)
                             && mi.ItemVersion.Equals(Constants.ChangeGroupGenericVersionNumber) // include only non-versioned migration items
                         orderby mi.Id descending
                         select mi).Take(1);
                }
                else
                {
                    migrationItemResult =
                        from mi in context.RTMigrationItemSet
                        where mi.ItemId.Equals(id)
                            && mi.MigrationSource.UniqueId.Equals(peerSourceId)
                            && mi.ItemVersion.Equals(Constants.ChangeGroupGenericVersionNumber) // include only non-versioned migration items
                        select mi;
                }

                if (migrationItemResult.Count() == 0)
                {
                    if (m_conversionHistory.Count < m_cacheSize)
                    {
                        m_conversionHistory.Add(id, targetItemId);
                        m_conversionHistoryContentChanged.Add(id, contentChanged);
                    }
                    return targetItemId;
                }

                RTMigrationItem sourceItem = migrationItemResult.First();
                var itemConvPairResult =
                    from p in context.RTItemRevisionPairSet
                    where (p.LeftMigrationItem.Id == sourceItem.Id || p.RightMigrationItem.Id == sourceItem.Id)
                        && (p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionId))
                    select p;

                if (itemConvPairResult.Count() == 0)
                {
                    if (m_conversionHistory.Count < m_cacheSize)
                    {
                        m_conversionHistory.Add(id, targetItemId);
                        m_conversionHistoryContentChanged.Add(id, contentChanged);
                    }
                    return targetItemId;
                }

                RTItemRevisionPair itemRevisionPair = itemConvPairResult.First();
                itemRevisionPair.ConversionHistoryReference.Load();
                contentChanged = itemRevisionPair.ConversionHistory.ContentChanged;
                if (itemRevisionPair.LeftMigrationItem == sourceItem)
                {
                    itemRevisionPair.RightMigrationItemReference.Load();
                    targetItemId = itemRevisionPair.RightMigrationItem.ItemId;
                }
                else
                {
                    itemRevisionPair.LeftMigrationItemReference.Load();
                    targetItemId = itemRevisionPair.LeftMigrationItem.ItemId;
                }
            }
            if (m_conversionHistory.Count < m_cacheSize)
            {
                m_conversionHistory.Add(id, targetItemId);
                m_conversionHistoryContentChanged.Add(id, contentChanged);
            }
            return targetItemId;
        }

        public override ChangeGroup CreateForMigrationInstructionTable(ChangeGroup deltaTableChangeGroup)
        {
            ChangeGroup group = new SqlChangeGroup(this, (SqlChangeGroup)deltaTableChangeGroup);
            group.Status = ChangeStatus.PendingConflictDetection;
            return group;
        }

        public override ReadOnlyCollection<KeyValuePair<MigrationAction, MigrationAction>> DetectContentConflict()
        {
            List<KeyValuePair<MigrationAction, MigrationAction>> conflictActions = new List<KeyValuePair<MigrationAction, MigrationAction>>();
            Dictionary<long, SqlChangeGroup> loadedChangeGroups = new Dictionary<long, SqlChangeGroup>();
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                foreach (VCContentConflictResult contentConflictResults in context.QueryContentConflict(SourceId, new Guid(Session.SessionUniqueId)))
                {
                    RTChangeAction migrationInstructionAction =
                        context.RTChangeActionSet.Where(a => a.ChangeActionId == contentConflictResults.MigrationInstructionChangeActionId).First();

                    RTChangeAction deltaAction =
                        context.RTChangeActionSet.Where(a => a.ChangeActionId == contentConflictResults.DeltaChangeActionId).First();

                    SqlMigrationAction conflictActionSource = SqlMigrationAction.RealizeFromDB(migrationInstructionAction);
                    if (loadedChangeGroups.ContainsKey(migrationInstructionAction.ChangeGroupId))
                    {
                        conflictActionSource.ChangeGroup = loadedChangeGroups[migrationInstructionAction.ChangeGroupId];
                    }
                    else
                    {
                        RTChangeGroup migrationInstructionChangeGroup = context.RTChangeGroupSet.Where(c => c.Id == migrationInstructionAction.ChangeGroupId).First();
                        SqlChangeGroup conflictChangeGroupSource = new SqlChangeGroup(this);
                        conflictChangeGroupSource.RealizeFromEDMWithSingleAction(migrationInstructionChangeGroup, migrationInstructionAction);
                        loadedChangeGroups.Add(migrationInstructionAction.ChangeGroupId, conflictChangeGroupSource);
                        conflictActionSource.ChangeGroup = conflictChangeGroupSource;
                    }

                    SqlMigrationAction conflictActionTarget = SqlMigrationAction.RealizeFromDB(deltaAction);

                    if (loadedChangeGroups.ContainsKey(deltaAction.ChangeGroupId))
                    {
                        conflictActionTarget.ChangeGroup = loadedChangeGroups[deltaAction.ChangeGroupId];
                    }
                    else
                    {
                        RTChangeGroup deltaChangeGroup = context.RTChangeGroupSet.Where(c => c.Id == deltaAction.ChangeGroupId).First();
                        SqlChangeGroup conflictChangeGroupTarget = new SqlChangeGroup(this);
                        conflictChangeGroupTarget.RealizeFromEDMWithSingleAction(deltaChangeGroup, deltaAction);
                        loadedChangeGroups.Add(deltaAction.ChangeGroupId, conflictChangeGroupTarget);
                        conflictActionTarget.ChangeGroup = conflictChangeGroupTarget;
                    }

                    conflictActions.Add(new KeyValuePair<MigrationAction, MigrationAction>(conflictActionSource, conflictActionTarget));
                }
            }
            return conflictActions.AsReadOnly();
        }

        private IEnumerable<ChangeGroup> GetChangeGroupTablePageByStatus(
            int pageNumber, 
            int pageSize,
            Guid sessionUniqueId,
            ChangeStatus status,
            bool useOtherSideMigrationItemSerializers,
            bool includeGroupInBacklog)
        {
            List<ChangeGroup> deltaTableEntries = new List<ChangeGroup>();

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int statusVal = (int)status;

                IQueryable<RTChangeGroup> query;
                if (includeGroupInBacklog)
                {
                    query = (from cg in context.RTChangeGroupSet
                             where cg.SessionUniqueId.Equals(sessionUniqueId)
                             && cg.SourceUniqueId.Equals(SourceId)
                             && cg.Status == statusVal
                             orderby cg.Id
                             select cg).Skip(pageNumber * pageSize).Take(pageSize);
                }
                else
                {
                    query = (from cg in context.RTChangeGroupSet
                             where cg.SessionUniqueId.Equals(sessionUniqueId)
                             && cg.SourceUniqueId.Equals(SourceId)
                             && cg.Status == statusVal
                             && !cg.ContainsBackloggedAction
                             orderby cg.Id
                             select cg).Skip(pageNumber * pageSize).Take(pageSize);
                }
                
                foreach (RTChangeGroup rtChangeGroup in query)
                {
                    SqlChangeGroup changeGroup = new SqlChangeGroup(this);
                    changeGroup.UseOtherSideMigrationItemSerializers = useOtherSideMigrationItemSerializers;
                    changeGroup.RealizeFromEDM(rtChangeGroup);
                    deltaTableEntries.Add(changeGroup);
                }
            }

            return deltaTableEntries;
        }

        internal void BatchSaveGroupedChangeActions(IList<IMigrationAction> page)
        {
            // note that we assume all IMigrationActions in page belongs to the same change group
            if (page.Count == 0)
            {
                return;
            }

            ChangeGroup changeGroup = page.First().ChangeGroup;
            Debug.Assert(null != changeGroup, "IMigrationAction does not have a parent ChangeGroup");

            SqlChangeGroup sqlChangeGroup = changeGroup as SqlChangeGroup;
            Debug.Assert(null != sqlChangeGroup, "Cannot convert IMigrationAction.ChangeGroup to SqlChangeGroup");

            sqlChangeGroup.BatchSaveChangeActions(page);
        }

        internal override long? GetFirstConflictedChangeGroup(ChangeStatus status)
        {
            Guid sessionUniqueId = new Guid(Session.SessionUniqueId);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int statusVal = (int)status;

                var query = (from cg in context.RTChangeGroupSet
                             where cg.SessionUniqueId.Equals(sessionUniqueId)
                                && cg.SourceUniqueId.Equals(SourceId)
                                && cg.Status == statusVal
                                && cg.ContainsBackloggedAction
                             orderby cg.Id
                             select cg.Id).Take(1);

                if (query.Count() > 0)
                {
                    return query.First();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Discard a migration instruction change group and reactivate the corresponding delta table entry
        /// </summary>
        /// <param name="migrationInstructionEntry"></param>
        /// <returns>The corresponding delta table entry</returns>
        internal override ChangeGroup DiscardMigrationInstructionAndReactivateDelta(ChangeGroup migrationInstructionEntry)
        {
            Debug.Assert(migrationInstructionEntry.ReflectedChangeGroupId.HasValue, "migrationInstructionEntry.ReflectedChangeGroupId does not have value");

            RTChangeGroup rtChangeGroup = null;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                long migrInstrId = migrationInstructionEntry.ChangeGroupId;
                var migrInstr = context.RTChangeGroupSet.Where(g => g.Id == migrInstrId);

                long deltaId = migrationInstructionEntry.ReflectedChangeGroupId.Value;
                var delta = context.RTChangeGroupSet.Where(g => g.Id == deltaId);

                if (migrInstr.Count() != 1 || delta.Count() != 1)
                {
                    return null;
                }

                migrInstr.First().Status = (int)ChangeStatus.Obsolete;

                rtChangeGroup = delta.First();
                rtChangeGroup.Status = (int)ChangeStatus.DeltaPending;

                context.TrySaveChanges();

                context.Detach(rtChangeGroup);
            }

            SqlChangeGroup changeGroup = new SqlChangeGroup(this);
            changeGroup.UseOtherSideMigrationItemSerializers = true;
            changeGroup.RealizeFromEDM(rtChangeGroup);

            return changeGroup;
        }

        internal override void ReactivateMigrationInstruction(
            ChangeGroup migrationInstruction, 
            ChangeGroup deltaEntryToObsolete)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                long migrInstrId = migrationInstruction.ChangeGroupId;
                var migrInstr = context.RTChangeGroupSet.Where(g => g.Id == migrInstrId);

                long deltaId = deltaEntryToObsolete.ChangeGroupId;
                var delta = context.RTChangeGroupSet.Where(g => g.Id == deltaId);

                Debug.Assert(migrInstr.Count() == 1, "migrInstr.Count() != 1"); 
                Debug.Assert(delta.Count() == 1, "delta.Count() != 1");

                migrInstr.First().Status = (int)ChangeStatus.Pending;
                delta.First().Status = (int)ChangeStatus.DeltaComplete;

                context.TrySaveChanges();
            }
        }
    }
}
