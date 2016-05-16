// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ItemConversionHistory
    {
        public ItemConversionHistory(
            string sourceItemId,
            string sourceItemVersion,
            string targetItemId,
            string targetItemVersion)
        {
            SourceItemId = sourceItemId;
            SourceItemVersion = sourceItemVersion;
            TargetItemId = targetItemId;
            TargetItemVersion = targetItemVersion;
        }

        public string SourceItemId { get; set; }
        public string SourceItemVersion { get; set; }
        public string TargetItemId { get; set; }
        public string TargetItemVersion { get; set; }
    }
    
    /// <summary>
    /// This class encapsulates the conversion results for a particular change group.
    /// </summary>
    public class ConversionResult
    {
        private bool m_continueProcessing = true;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sourceSideSourceId">Source migration source Unique Id</param>
        /// <param name="targetSideSourceId">Target migration source Unique Id</param>
        public ConversionResult(Guid sourceSideSourceId, Guid targetSideSourceId)
        {
            SourceSideSourceId = sourceSideSourceId;
            TargetSideSourceId = targetSideSourceId;
        }

        List<ItemConversionHistory> m_itemConversionHistory = new List<ItemConversionHistory>();

        public Guid SourceSideSourceId { get; set; }
        public Guid TargetSideSourceId { get; set; }
        public string ChangeId { get; set; }

        /// <summary>
        /// Gets a collection of individual item conversion history
        /// </summary>
        public List<ItemConversionHistory> ItemConversionHistory
        {
            get
            {
                return m_itemConversionHistory;
            }
        }

        /// <summary>
        /// Gets whether the migration pipeline should proceed or not.
        /// </summary>
        public bool ContinueProcessing
        {
            get
            {
                return m_continueProcessing;
            }

            set
            {
                m_continueProcessing = value;
            }
        }

        /// <summary>
        /// Saves the conversion history and associate it with a particular session run and migration source.
        /// </summary>
        /// <param name="sessionRunId"></param>
        /// <param name="migrationSourceId"></param>
        /// <returns></returns>
        public bool Save(
            int sessionRunId,
            Guid migrationSourceId)
        {
            return Save(sessionRunId, migrationSourceId, Guid.Empty, null);
        }

        /// <summary>
        /// Saves the conversion history and associate it with a particular session run and migration source.
        /// </summary>
        /// <param name="migrationSourceId"></param>
        /// <returns></returns>
        public bool Save(
            Guid migrationSourceId)
        {
            return Save(int.MinValue, migrationSourceId, Guid.Empty, null);
        }

        /// <summary>
        /// Saves the conversion history and associate it with a particular session run and migration source;
        /// Additionally, mark the Reflected Change Group (the delta table entry of this processed change group) to be sync-ed.
        /// </summary>
        /// <param name="sessionRunId">int.MinValue if sessionRunId is not available</param>
        /// <param name="migrationSourceId"></param>
        /// <param name="sessionId"></param>
        /// <param name="reflectedChangeGroupId"></param>
        /// <returns></returns>
        internal bool Save(
            int sessionRunId,
            Guid migrationSourceId,
            Guid sessionId,
            long? reflectedChangeGroupId)
        {
            if (string.IsNullOrEmpty(ChangeId))
            {
                return false;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                if (ItemConversionHistory.Count > 0)
                {
                    var sessionRunQuery = (sessionRunId == int.MinValue) 
                        ? from sr in context.RTSessionRunSet
                          where sr.Config.LeftSourceConfig.MigrationSource.UniqueId.Equals(migrationSourceId)
                          || sr.Config.RightSourceConfig.MigrationSource.UniqueId.Equals(migrationSourceId)
                          select sr
                        : context.RTSessionRunSet.Where(sr => sr.Id == sessionRunId);
                    if (sessionRunQuery.Count() == 0)
                    {
                        return false;
                    }
                    RTSessionRun rtSessionRun = sessionRunQuery.First();

                    var migrationSourceQuery = context.RTMigrationSourceSet.Where(ms => ms.UniqueId.Equals(migrationSourceId));
                    if (migrationSourceQuery.Count() == 0)
                    {
                        return false;
                    }
                    RTMigrationSource rtMigrationSource = migrationSourceQuery.First();

                    RTConversionHistory rtConvHist = UpdateGroupConvHist(context, rtSessionRun, rtMigrationSource);
                    UpdateItemConversionHistory(rtConvHist, context);
                }

                if (!sessionId.Equals(Guid.Empty) && reflectedChangeGroupId.HasValue)
                {
                    MarkDeltaTableSynced(context, sessionId, reflectedChangeGroupId.Value);
                }

                context.TrySaveChanges();
                return true;
            }
        }

        private void MarkDeltaTableSynced(
            RuntimeEntityModel context,
            Guid sessionId,
            long reflectedChangeGroupId)
        {
            var runTimeChangeGroups = context.RTChangeGroupSet.Where
                (cg => cg.SessionUniqueId == sessionId
                    && cg.Id == reflectedChangeGroupId);

            if (runTimeChangeGroups.Count() > 0)
            {
                runTimeChangeGroups.First().Status = (int)ChangeStatus.DeltaSynced;
            }
        }

        private RTConversionHistory UpdateGroupConvHist(
            RuntimeEntityModel context,
            RTSessionRun rtSessionRun,
            RTMigrationSource rtMigrationSource)
        {
            RTConversionHistory runTimeConverHistory = RTConversionHistory.CreateRTConversionHistory(
                    DateTime.UtcNow,
                    -1,
                    false);           

            runTimeConverHistory.SessionRun = rtSessionRun;
            runTimeConverHistory.SourceMigrationSource = rtMigrationSource;

            context.AddToRTConversionHistorySet(runTimeConverHistory);
            return runTimeConverHistory;
        }

        private void UpdateItemConversionHistory(
            RTConversionHistory rtConvHist,
            RuntimeEntityModel context)
        {
            foreach (ItemConversionHistory hist in ItemConversionHistory)
            {
                if (string.IsNullOrEmpty(hist.SourceItemId)
                    || string.IsNullOrEmpty(hist.TargetItemId))
                {
                    throw new MigrationException(MigrationToolkitResources.InvalidConversionHistoryInfo);
                }

                string sourceItemVersionStr = string.IsNullOrEmpty(hist.SourceItemVersion)
                                               ? Constants.ChangeGroupGenericVersionNumber
                                               : hist.SourceItemVersion;
                RTMigrationItem sourceItem = FindCreateMigrationItem(
                    SourceSideSourceId,
                    hist.SourceItemId,
                    sourceItemVersionStr,
                    context);

                string targetItemVersionStr = string.IsNullOrEmpty(hist.TargetItemVersion)
                                               ? Constants.ChangeGroupGenericVersionNumber
                                               : hist.TargetItemVersion;
                RTMigrationItem targetItem = FindCreateMigrationItem(
                    TargetSideSourceId,
                    hist.TargetItemId,
                    targetItemVersionStr,
                    context);

                context.TrySaveChanges();

                // check if the pair is already in the item_revision_pair table
                var pairWithSourceItemQuery =
                    from p in context.RTItemRevisionPairSet
                    where (p.LeftMigrationItemId == sourceItem.Id || p.RightMigrationItemId == sourceItem.Id)                           
                    select p;
                if (pairWithSourceItemQuery.Count() > 0)
                {
                    var targetItemInPairQuery =
                        from p in pairWithSourceItemQuery
                        where p.LeftMigrationItemId == targetItem.Id || p.RightMigrationItemId == targetItem.Id
                        select p;

                    if (targetItemInPairQuery.Count() > 0)
                    {
                        continue;
                    }
                }

                RTItemRevisionPair pair = RTItemRevisionPair.CreateRTItemRevisionPair(
                        sourceItem.Id, targetItem.Id);
                pair.LeftMigrationItem = sourceItem;
                pair.RightMigrationItem = targetItem;
                pair.ConversionHistory = rtConvHist;
            }
        }

        private RTMigrationItem FindCreateMigrationItem(
            Guid sourceId,
            string itemId,
            string itemVersion,
            RuntimeEntityModel context)
        {
            // query for the item
            var itemQueryResult =
                (from i in context.RTMigrationItemSet
                 where i.ItemId.Equals(itemId, StringComparison.InvariantCultureIgnoreCase)
                    && i.ItemVersion.Equals(itemVersion, StringComparison.InvariantCultureIgnoreCase)
                    && (i.MigrationSource.UniqueId == sourceId)
                 select i);
            if (itemQueryResult.Count<RTMigrationItem>() == 1)
            {
                return itemQueryResult.First<RTMigrationItem>();
            }

            Debug.Assert(itemQueryResult.Count<RTMigrationItem>() == 0);

            RTMigrationItem migrationItem = RTMigrationItem.CreateRTMigrationItem(0, itemId, itemVersion);

            var migrationSourceQuery = context.RTMigrationSourceSet.Where(ms => ms.UniqueId.Equals(sourceId));
            if (migrationSourceQuery.Count() == 0)
            {
                throw new MigrationException("ERROR: Migration Source is not found.");
            }
            else
            {
                migrationItem.MigrationSource = migrationSourceQuery.First();
            }

            return migrationItem;
        }
    }
}
