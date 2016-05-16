// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Web.Services.Protocols;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    /// <summary>
    /// The message/warning/error callback
    /// </summary>
    /// <param name="message">The string to log</param>
    internal delegate void ProcessorCallback(string message);

    /// <summary>
    /// The basic TFS analysis algorithm delegate type
    /// </summary>
    /// <param name="change">The change being processed</param>
    /// <param name="group">The change group this change is to be a part of</param>
    internal delegate void TfsAnalysisAlgorithm(Change change, ChangeGroup group);

    internal abstract class TfsAnalysisAlgorithms
    {
        const int MAX_MERGE_ROOTS_COUNT = 100;
        Dictionary<ChangeType, TfsAnalysisAlgorithm> m_tfsChangeTranslators;
        TfsVCAnalysisProvider m_provider;

        Dictionary<string, Change> m_batchedBranches = new Dictionary<string, Change>();
        Dictionary<string, Change> m_batchedMerges = new Dictionary<string, Change>();
        SortedList<int, int> m_mergeDepth = new SortedList<int, int>();
        Change m_currentChange;

        protected TfsVCAnalysisProvider Provider
        {
            get
            {
                return m_provider;
            }
        }

        protected Dictionary<ChangeType, TfsAnalysisAlgorithm> TfsChangeTranslators
        {
            get
            {
                return m_tfsChangeTranslators;
            }
            set
            {
                m_tfsChangeTranslators = value;
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal TfsAnalysisAlgorithms(TfsVCAnalysisProvider provider)
        {
            m_provider = provider;
            BuildChangeTranslators();
        }

        /// <summary>
        /// Execute the algorithm to analyze a change
        /// </summary>
        /// <param name="change"></param>
        /// <param name="changeGroup"></param>
        internal void Execute(Change change, ChangeGroup changeGroup)
        {
            validateChangeAndGroup(change, changeGroup);

            TfsAnalysisAlgorithm algorithm;

            if (!m_tfsChangeTranslators.TryGetValue(stripChangeType(change.ChangeType), out algorithm))
            {
                algorithm = Unhandled;
            }

            algorithm(change, changeGroup);
        }

        /// <summary>
        /// Initialize the algorithms.
        /// </summary>
        internal void Initialize()
        {
            if (m_batchedBranches != null)
            {
                m_batchedBranches.Clear();
            }
            if (m_batchedMerges != null)
            {
                m_batchedMerges.Clear();
            }
        }


        /// <summary>
        /// Finish the processing of change group - submit all batched requests. 
        /// </summary>
        /// <param name="group"></param>
        internal void Finish(ChangeGroup group)
        {
            processBatchedBranches(group);
            processBatchedMerges(group);
        }

        /// <summary>
        /// Removed the lock, none and encoding bits from the changetypes to normalize them for hashtable lookup
        /// </summary>
        /// <param name="changeType">The changetype to normalize</param>
        /// <returns>The normalized changetype</returns>
        internal static ChangeType stripChangeType(ChangeType changeType)
        {
            changeType &= ~ChangeType.SourceRename;
            changeType &= ~ChangeType.Lock;
            changeType &= ~ChangeType.Encoding;
            changeType &= ~ChangeType.Rollback; // We just skip rollback changetype, other combined change type will be migrated. 
            if (changeType != ChangeType.None)
            {
                changeType &= ~ChangeType.None;
            }

            return changeType;
        }

        /// <summary>
        /// Called when the requested change type is unknown.  This indicates a previously unseen change operation and means that a
        /// new algorithm method needs to be created.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Unhandled(Change change, ChangeGroup group)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            ConflictResolutionResult result = raiseUnhandledChangeTypeConflict(group, change.ChangeType);
            if (!string.IsNullOrEmpty(result.Comment)) // if skip, comment is empty
            {
                ChangeType changeType;
                if (UnhandledChangeTypeConflictHandler.String2ChangeType(result.Comment, out changeType))
                {
                    TfsAnalysisAlgorithm algorithm;
                    if (m_tfsChangeTranslators.TryGetValue(stripChangeType(changeType), out algorithm))
                    {
                        algorithm(change, group);
                    }
                    else
                    {
                        Debug.Fail("Should not happen.");
                    }
                }
                else
                {
                    Debug.Fail("Should not happen.");
                }
            }
        }

        /// <summary>
        /// Add a merge action into the change group
        /// action.SourceItem -> merge to item in source Tfs; 
        /// action.BranchFromVersion -> merge starting version of the item on source tfs
        /// action.MergeVersionTo -> merge end version of the item on source tfs
        /// action.TargetSourceItem -> merge from item on target system; 
        /// action.TargetTargetItem -> merge to item on target system
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Merge(Change change, ChangeGroup group)
        {
            // TFS2010 only change: In TFS2010, a deleted item will be carried over with Merge|Rename action. 
            // Analysis should ignore the change action.
            if (((change.ChangeType & ChangeType.Delete) != ChangeType.Delete) && (change.Item.DeletionId != 0))
            {
                return;
            }
            if (m_batchedMerges.ContainsKey(change.Item.ServerItem))
            {
                if (change.Item.DeletionId != 0)
                {
                    return;
                }
                else
                {
                    // the following debug assertion is generating too much noise to the trace file
                    // Debug.Assert(m_batchedMerges[change.Item.ServerItem].Item.DeletionId != 0, " Duplicate non deleted items exist in the same changeset");
                    m_batchedMerges.Remove(change.Item.ServerItem);
                }
            }
            int depth = VersionControlPath.GetFolderDepth(change.Item.ServerItem);
            if (!m_mergeDepth.ContainsKey(depth))
            {
                m_mergeDepth.Add(depth, 0);
            }
            m_mergeDepth[depth] = m_mergeDepth[depth] + 1;
            m_batchedMerges.Add(change.Item.ServerItem, change);
        }

        internal static string convertContentType(ItemType tfsItemType)
        {
            switch (tfsItemType)
            {
                case ItemType.Any:
                    return WellKnownContentType.VersionControlledArtifact.ReferenceName;
                case ItemType.File:
                    return WellKnownContentType.VersionControlledFile.ReferenceName;
                case ItemType.Folder:
                    return WellKnownContentType.VersionControlledFolder.ReferenceName;
                default:
                    Debug.Fail("Unknown TFS item type");
                    return WellKnownContentType.VersionControlledArtifact.ReferenceName;

            }
        }

        /// <summary>
        /// A delete undelete is ignored.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void DeleteUndelete(Change change, ChangeGroup group)
        {
            // do nothing.
        }

        /// <summary>
        /// Adds an undelete operation to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Undelete(Change change, ChangeGroup group)
        {
            // For a undelete action, previousItem is the delete version of the item. 
            // This version is needed if the path being undeleted contains several deleted items.
            Item previous = null;
            try
            {
                foreach (Changeset previousChange in change.Item.VersionControlServer.QueryHistory(
                    change.Item.ServerItem,
                    new ChangesetVersionSpec(change.Item.ChangesetId),
                    0,
                    RecursionType.None,
                    null,
                    null,
                    new ChangesetVersionSpec(change.Item.ChangesetId - 1),
                    1,
                    true,
                    false,
                    false))
                {
                    previous = previousChange.Changes[0].Item;
                }
            }
            catch (ItemNotFoundException)
            {
                // We already set previous to null.
            }

            if (previous != null)
            {
                switch (Provider.IsPathMapped(previous.ServerItem, previous.ChangesetId))
                {
                    case MappingResult.Mapped:
                        group.CreateAction(
                            WellKnownChangeActionId.Undelete,
                            new TfsMigrationItem(change.Item),
                            previous.ServerItem,
                            change.Item.ServerItem,
                            previous.ChangesetId.ToString(CultureInfo.InvariantCulture),
                            null,
                            convertContentType(change.Item.ItemType),
                            null);
                        // Create an edit if this is a file undelete. 
                        if (change.Item.ItemType == ItemType.File)
                        {
                            createMigrationAction(change.Item, null, group, WellKnownChangeActionId.Edit);
                        }
                        break;
                    case MappingResult.MappedBeforeSnapshot:
                    case MappingResult.NotMapped:
                        // The Undelete from item is not mapped or the undelete from item is before the snapshot. Change it to Add.
                        createMigrationAction(change.Item, null, group, WellKnownChangeActionId.Add);
                        break;
                }
            }
        }

        /// <summary>
        /// Calls Edit and Undelete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void EditUndelete(Change change, ChangeGroup group)
        {
            Undelete(change, group);
            // Edit will always be pended for file undelete.
        }

        /// <summary>
        /// Adds a delete operation to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Delete(Change change, ChangeGroup group)
        {
            IMigrationAction deleteAction = createMigrationAction(change.Item, change.Item.ServerItem, group, WellKnownChangeActionId.Delete);
        }

        /// <summary>
        /// None is not a valid change type. We will change it to Edit action. 
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void None(Change change, ChangeGroup group)
        {
            Edit(change, group);
        }

        /// <summary>
        /// Adds an edit action to the change group
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Edit(Change change, ChangeGroup group)
        {
            if ((change.ChangeType & (ChangeType.Branch | ChangeType.Merge)) == (ChangeType.Branch | ChangeType.Merge)
                || (change.ChangeType & (ChangeType.Undelete | ChangeType.Merge)) == (ChangeType.Undelete | ChangeType.Merge))
            {
                IMigrationAction action = group.CreateAction(
                    WellKnownChangeActionId.Edit,
                    new TfsMigrationItem(change.Item),
                    change.Item.ServerItem,
                    change.Item.ServerItem,
                    "1", // Mark version to 1 for Branch|Merge|Edit and Merge|Undelete|Edit
                    null,
                    convertContentType(change.Item.ItemType),
                    null);
            }
            else
            {
                IMigrationAction action = createMigrationAction(change.Item, change.Item.ServerItem, group, WellKnownChangeActionId.Edit);
            }
        }

        /// <summary>
        /// Calls MergeUndelete and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void EditUndeleteMerge(Change change, ChangeGroup group)
        {
            MergeUndelete(change, group);
            // Edit will always be pended for file undelete.
        }

        /// <summary>
        /// Adds an add action to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Add(Change change, ChangeGroup group)
        {
            createMigrationAction(change.Item, null, group, WellKnownChangeActionId.Add);
        }

        /// <summary>
        /// Calls Branch and Merge to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void BranchMerge(Change change, ChangeGroup group)
        {
            //Branch(change, group);
            Merge(change, group);
        }

        // allowDeleted is used to filter out the case where deleted items are brought along with a directory rename.
        // in that case we don't want to pend the actions in the target system since they don't exist there.
        private void rename(Change change, ChangeGroup group, bool allowDeleted)
        {
            if (allowDeleted || change.Item.DeletionId == 0)
            {
                Item previous = null;
                try
                {
                     foreach (Changeset previousChange in change.Item.VersionControlServer.QueryHistory(
                        change.Item.ServerItem,
                        new ChangesetVersionSpec(change.Item.ChangesetId),
                        0,
                        RecursionType.None,
                        null,
                        null,
                        new ChangesetVersionSpec(change.Item.ChangesetId - 1),
                        1,
                        true,
                        false,
                        false))
                    {
                        previous = previousChange.Changes[0].Item;
                    }
                }
                catch (ItemNotFoundException)
                {
                    // We already set previous to null.
                }


                if (previous == null)
                {
                    raiseBranchParentNotFoundConflictForRename(group, change);
                }
                else 
                {
                    switch (Provider.IsPathMapped(previous.ServerItem, previous.ChangesetId))
                    {
                        case MappingResult.NotMapped:
                            // Rename from path is not mapped
                            MigrationConflict pathNotMappedConflict = VCPathNotMappedConflictType.CreateConflict(previous.ServerItem);
                            List<MigrationAction> retActions;
                            ConflictResolutionResult resolutionResult =
                                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, pathNotMappedConflict, out retActions);
                            if (resolutionResult.Resolved)
                            {
                                switch (resolutionResult.ResolutionType)
                                {
                                    case ConflictResolutionType.UpdatedConflictedChangeAction:
                                        // Don't change to Add if it a deleted rename carry over by parent rename.
                                        if (change.Item.DeletionId == 0)
                                        {
                                            createMigrationAction(change.Item, null, group, WellKnownChangeActionId.Add);
                                        }
                                        break;
                                    case ConflictResolutionType.ChangeMappingInConfiguration:
                                        throw new MigrationUnresolvedConflictException(pathNotMappedConflict);
                                    default:
                                        throw new MigrationUnresolvedConflictException(pathNotMappedConflict);
                                }
                            }
                            else
                            {
                                throw new MigrationUnresolvedConflictException(pathNotMappedConflict);
                            }
                            break;
                        case MappingResult.Mapped:
                            IMigrationAction action = createMigrationAction(change.Item, previous.ServerItem, group, WellKnownChangeActionId.Rename);
                            break;
                        case MappingResult.MappedBeforeSnapshot:
                            if ((change.ChangeType & ChangeType.Undelete) != 0)
                            {
                                // For RenameUndelete case, if item is renameundeleted from an item before the snapshot, just return.
                                // The undelete will change the action to Add.                                
                                return;
                            }
                            else
                            {
                                createMigrationAction(change.Item, previous.ServerItem, group, WellKnownChangeActionId.Rename);
                            }
                            break;
                    }
                }
            }
            else
            {
                TraceManager.TraceInformation("Skipping deleted item carried over with a rename");
            }
        }


        /// <summary>
        /// Adds a rename action to the change group.  If the source operation of the rename operation is not mapped
        /// an add operation is added to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Rename(Change change, ChangeGroup group)
        {
            rename(change, group, false);
        }

        /// <summary>
        /// Calls Rename and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameEdit(Change change, ChangeGroup group)
        {
            Rename(change, group);
            Edit(change, group);
        }

        /// <summary>
        /// Calls Rename and Delete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameDeleteMerge(Change change, ChangeGroup group)
        {
            RenameDelete(change, group);
        }

        /// <summary>
        /// Calls Merge and Rename to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameMerge(Change change, ChangeGroup group)
        {
            Merge(change, group);
            Rename(change, group);
        }

        /// <summary>
        /// Calls RenameMerge and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameEditMerge(Change change, ChangeGroup group)
        {
            RenameMerge(change, group);
            Edit(change, group);
        }

        /// <summary>
        /// If the previous item (the source item of the rename) is mapped an undelete action is added and Rename is called to 
        /// add the actions to the change group.  If the previous item is not mapped Add is called to add the appropriate 
        /// actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameUndelete(Change change, ChangeGroup group)
        {
            Rename(change, group);
            Undelete(change, group);
        }


        /// <summary>
        /// Calls RenameUndelete and Merge to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameUndeleteMerge(Change change, ChangeGroup group)
        {
            RenameUndelete(change, group);
            Merge(change, group);
        }

        /// <summary>
        /// Calls RenameUndelete, Merge and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameEditUndeleteMerge(Change change, ChangeGroup group)
        {
            RenameUndelete(change, group);
            Merge(change, group);
            // Edit will always be pended for file undelete.
        }

        private void processBatchedMerges(ChangeGroup group)
        {
            if (m_batchedMerges.Count == 0)
            {
                return;
            }

            TraceManager.TraceInformation("BatchedMerges: Calculating depth threshold from {0} items", m_batchedMerges.Count);
            // Calculate the depth threshold for 100 non-recursive merges. 
            int depthThreshold = 0;
            int accumulatedNonRecursiveMerges = 0;

            foreach (KeyValuePair<int, int> depthEntry in m_mergeDepth)
            {
                if (depthEntry.Value + accumulatedNonRecursiveMerges > 100)
                {
                    break;
                }
                depthThreshold = depthEntry.Key;
                accumulatedNonRecursiveMerges = accumulatedNonRecursiveMerges + depthEntry.Value;
            }

            Dictionary<string, Change> recursiveMerges = new Dictionary<string, Change>();

            foreach (string batchedMerge in m_batchedMerges.Keys)
            {
                if (VersionControlPath.GetFolderDepth(batchedMerge) > depthThreshold)
                {
                    recursiveMerges.Add(batchedMerge, m_batchedMerges[batchedMerge]);
                }
            }

            TraceManager.TraceInformation("BatchedMerges: Finding merge roots from {0} items", recursiveMerges.Count);

            // Find common roots for items with depth larger than depth threshold. These common roots will be queried recursively. 
            HashSet<string> mergeRoots = GetPathsCommonRoots(recursiveMerges.Keys);

            VersionControlServer tfsClient = m_currentChange.Item.VersionControlServer;
            ChangesetVersionSpec version = new ChangesetVersionSpec(m_currentChange.Item.ChangesetId);

            TraceManager.TraceInformation("Selected {0} merge roots", mergeRoots.Count);

            CollapseToParentTillMappedRoot(recursiveMerges, mergeRoots, GetMappedRootPathsInConfig());

            TraceManager.TraceInformation("Selected {0} merge roots after compression", mergeRoots.Count);
            // For each branch root, call QueryMergeWithDetails() recursively.
            int mergeIndex = 0;
            int targetDeletionId = 0;
            foreach (string mergeRoot in mergeRoots)
            {
                mergeIndex++;

                if (m_batchedMerges.ContainsKey(mergeRoot))
                {
                    targetDeletionId = m_batchedMerges[mergeRoot].Item.DeletionId;
                }
                else
                {
                    targetDeletionId = 0;
                }

                ChangesetMergeDetails mergeDetails;
                try
                {
                    mergeDetails = tfsClient.QueryMergesWithDetails(
                        null,
                        version,
                        0,
                        mergeRoot,
                        version,
                        targetDeletionId,
                        version,
                        version,
                        RecursionType.Full);
                }
                catch (XmlException xe)
                {
                    throw new VersionControlMigrationException(
                               string.Format(
                                   TfsVCAdapterResource.Culture,
                                   TfsVCAdapterResource.ClientNotCompatible), xe);

                }
                catch (VersionControlException vce)
                {
                    if (vce.InnerException is SoapException)
                    {
                        throw new VersionControlMigrationException(
                                   string.Format(
                                       TfsVCAdapterResource.Culture,
                                       TfsVCAdapterResource.ServerNotCompatible), vce);
                    }
                    else
                    {
                        throw;
                    }
                }

                ItemMerge[] mergedItems = mergeDetails.MergedItems;
                TraceManager.TraceInformation("Analyzing Recursive call {0}/{1}: {2} items in root: {3}",
                    mergeIndex, mergeRoots.Count,
                    mergedItems.Length, mergeRoot);
                Dictionary<string, ItemMerge> mergeFromVerions = new Dictionary<string, ItemMerge>();
                Dictionary<string, ItemMerge> mergeToVerions = new Dictionary<string, ItemMerge>();

                if (mergedItems != null && mergedItems.Length > 0)
                {
                    foreach (ItemMerge itemMerge in mergedItems)
                    {
                        if (mergeFromVerions.ContainsKey(itemMerge.TargetServerItem))
                        {
                            if (mergeFromVerions[itemMerge.TargetServerItem].SourceVersionFrom > itemMerge.SourceVersionFrom)
                            {
                                mergeFromVerions[itemMerge.TargetServerItem] = itemMerge;
                            }
                            if (mergeToVerions[itemMerge.TargetServerItem].SourceVersionFrom < itemMerge.SourceVersionFrom)
                            {
                                mergeToVerions[itemMerge.TargetServerItem] = itemMerge;
                            }
                        }
                        else
                        {
                            mergeFromVerions.Add(itemMerge.TargetServerItem, itemMerge);
                            mergeToVerions.Add(itemMerge.TargetServerItem, itemMerge);
                        }
                    }

                    foreach (KeyValuePair<string, ItemMerge> mergeItem in mergeFromVerions)
                    {
                        if (m_batchedMerges.ContainsKey(mergeItem.Key))
                        {
                            mergeOneItem(group, m_batchedMerges[mergeItem.Key], mergeItem.Value, mergeToVerions[mergeItem.Key]);
                            m_batchedMerges.Remove(mergeItem.Key);
                        }
                    }
                }
            }

            TraceManager.TraceInformation("Making non-recursive call for {0} items", m_batchedMerges.Count);
            mergeIndex = 0;
            // If we still have remaining items at this point, query them non recursively.
            foreach (KeyValuePair<string, Change> remainingMerge in m_batchedMerges)
            {
                mergeIndex++;
                if (recursiveMerges.ContainsKey(remainingMerge.Key))
                {
                    // This merge should be processed in recursive merges. If we still see it here, this is a branch not found conflict. 
                    raiseBranchParentNotFoundConflictForMerge(group, remainingMerge.Value);
                }
                else
                {

                    ChangesetMergeDetails mergeDetails = tfsClient.QueryMergesWithDetails(
                            null,
                            version,
                            0,
                            remainingMerge.Key,
                            version,
                            remainingMerge.Value.Item.DeletionId,
                            version,
                            version,
                            RecursionType.None);

                    ItemMerge[] mergedItems = mergeDetails.MergedItems;
                    TraceManager.TraceInformation("Analyzing non-recursive call {0}: {1} items in root: {2}",
                        mergeIndex, mergedItems.Length, remainingMerge.Key);
                    if (mergedItems != null && mergedItems.Length > 0)
                    {
                        ItemMerge mergeVersionToItem = mergedItems[0];
                        ItemMerge mergeVersionFromItem = mergedItems[0];

                        foreach (ItemMerge itemMerge in mergedItems)
                        {
                            if (mergeVersionFromItem.SourceVersionFrom > itemMerge.SourceVersionFrom)
                            {
                                mergeVersionFromItem = itemMerge;
                            }
                            if (mergeVersionToItem.SourceVersionFrom < itemMerge.SourceVersionFrom)
                            {
                                mergeVersionToItem = itemMerge;
                            }
                        }

                        mergeOneItem(group, remainingMerge.Value, mergeVersionFromItem, mergeVersionToItem);
                    }
                    else
                    {
                        raiseBranchParentNotFoundConflictForMerge(group, remainingMerge.Value);
                    }
                }
            }
            m_batchedMerges.Clear();
            m_mergeDepth.Clear();
        }

        private void mergeOneItem(ChangeGroup group, Change mergeChange, ItemMerge mergeVersionFromItem, ItemMerge mergeVersionToItem)
        {
            switch (Provider.IsMergePathMapped(mergeVersionToItem.SourceServerItem, mergeChange.Item.ServerItem, mergeVersionToItem.SourceVersionFrom))
            {
                case MappingResult.NotMapped:
                    raisePathNotMappedConflictForMerge(group, mergeChange, mergeVersionToItem.SourceServerItem);
                    break;
                case MappingResult.Mapped:
                    if ((mergeChange.ChangeType & (ChangeType.Branch | ChangeType.Merge)) == (ChangeType.Branch | ChangeType.Merge))
                    {
                        // It is a branch|merge
                        IMigrationAction action = group.CreateAction(
                            WellKnownChangeActionId.BranchMerge,
                            new TfsMigrationItem(mergeChange.Item),
                            mergeVersionToItem.SourceServerItem,
                            mergeChange.Item.ServerItem,
                            mergeVersionFromItem.SourceVersionFrom.ToString(CultureInfo.InvariantCulture),
                            mergeVersionToItem.SourceVersionFrom.ToString(CultureInfo.InvariantCulture),
                            convertContentType(mergeChange.Item.ItemType),
                            null);
                    }
                    else
                    {
                        IMigrationAction action = group.CreateAction(
                             WellKnownChangeActionId.Merge,
                             new TfsMigrationItem(mergeChange.Item),
                             mergeVersionToItem.SourceServerItem,
                             mergeChange.Item.ServerItem,
                             mergeVersionFromItem.SourceVersionFrom.ToString(CultureInfo.InvariantCulture),
                             mergeVersionToItem.SourceVersionFrom.ToString(CultureInfo.InvariantCulture),
                             convertContentType(mergeChange.Item.ItemType),
                             null);
                    }
                    break;
                case MappingResult.MappedBeforeSnapshot:
                    if (handleHistoryBeforeSnapshot(mergeVersionToItem.SourceVersionFrom.ToString(), group))
                    {
                        // The merge from item is before the snapshot.
                        // If it is a branch|merge, change it to Add.
                        // If it is a pure merge, skip it
                        if ((mergeChange.ChangeType & (ChangeType.Branch | ChangeType.Merge)) == (ChangeType.Branch | ChangeType.Merge))
                        {
                            createMigrationAction(mergeChange.Item, null, group, WellKnownChangeActionId.Add);
                        }
                    }
                    else
                    {
                        throw new MigrationException("Don't know how to handle historynotfound conflict");
                    }

                    break;
                case MappingResult.OutOfMergeScope:
                    // The merge from item is out of the merge scope. 
                    // If it is a branch|merge, change it to Add.
                    // If it is a pure merge, skip it
                    if ((mergeChange.ChangeType & (ChangeType.Branch | ChangeType.Merge)) == (ChangeType.Branch | ChangeType.Merge))
                    {
                        createMigrationAction(mergeChange.Item, null, group, WellKnownChangeActionId.Add);
                    }
                    break;
            }
        }

        private ReadOnlyCollection<string> GetMappedRootPathsInConfig()
        {
            Debug.Assert(m_provider.ConfigurationService != null,
                         "The ConfigurationService for the TfsVCAdapter has not been properly initialized");

            List<string> retVal = new List<string>(m_provider.ConfigurationService.Filters.Count);
            foreach (MappingEntry filter in m_provider.ConfigurationService.Filters)
            {
                if (filter.Cloak)
                {
                    continue;
                }

                if (retVal.Contains(filter.Path))
                {
                    continue;
                }

                retVal.Add(filter.Path);
            }

            return retVal.AsReadOnly();
        }

        private HashSet<string> GetPathsCommonRoots(Dictionary<string, Change>.KeyCollection paths)
        {
            HashSet<string> commonRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> pathList = new List<string>(paths.Count);

            foreach (string path in paths)
            {
                pathList.Add(path);
            }
            // Sort the path sot that parent item is always list earlier. 
            pathList.Sort();

            string parentPath;
            foreach (string path in paths)
            {
                bool addPathToCommonRoots = true;
                parentPath = VersionControlPath.GetFolderName(path);

                while (!VersionControlPath.Equals(parentPath, VersionControlPath.RootFolder))
                {
                    if (commonRoots.Contains(parentPath))
                    {
                        addPathToCommonRoots = false;
                        break;
                    }
                    parentPath = VersionControlPath.GetFolderName(parentPath);
                }

                if (addPathToCommonRoots)
                {
                    commonRoots.Add(path);
                }
            }

            return commonRoots;
        }

        /// <summary>
        /// collapse the paths one level up, but stop at the mapped root path.
        /// </summary>
        /// <param name="mergeRoots"></param>
        /// <returns>true if all mergeRoots are collapsed to the mapped root path; false otherwise.</returns>
        private void CollapseToParentTillMappedRoot(Dictionary<string, Change> cache, HashSet<string> mergeRoots, ReadOnlyCollection<string> mappedPaths)
        {
            // No need to collapse to a parent folder for small # of branches.
            if (mergeRoots.Count <= MAX_MERGE_ROOTS_COUNT)
            {
                return;
            }

            // put paths into depth-based sockets
            int maxDepth = 0;
            var layeredPaths = new Dictionary<int, HashSet<string>>();
            Change change;
            foreach (string path in mergeRoots)
            {
                // don't collapse an item whose deletion id is not 0
                if (cache.TryGetValue(path, out change))
                {
                    if (change.Item.DeletionId != 0)
                    {
                        continue;
                    }
                }

                int depth = VersionControlPath.GetFolderDepth(path);
                if (!layeredPaths.ContainsKey(depth))
                {
                    layeredPaths.Add(depth, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }

                Debug.Assert(!layeredPaths[depth].Contains(path));
                layeredPaths[depth].Add(path);

                maxDepth = maxDepth > depth ? maxDepth : depth;
            }

            while (maxDepth > 0)
            {
                if (!layeredPaths.ContainsKey(maxDepth))
                {
                    --maxDepth;
                    continue;
                }

                var obsoletePaths = new List<string>();
                foreach (var path in layeredPaths[maxDepth])
                {
                    // skip if the path is in the filter mapping paths collection
                    if (IsPathInMappedPathsCollection(path, mappedPaths))
                    {
                        continue;
                    }

                    // skip if parent is in the filter mapping paths collection
                    string parentFolder = VersionControlPath.GetFolderName(path);
                    if (IsPathInMappedPathsCollection(parentFolder, mappedPaths))
                    {
                        continue;
                    }

                    // mark the path to be obsolete
                    obsoletePaths.Add(path);

                    // add to the "path.Depth -1" socket
                    int parentDepth = VersionControlPath.GetFolderDepth(parentFolder);
                    bool parentIsInMergeRoots = true;
                    if (!layeredPaths.ContainsKey(parentDepth))
                    {
                        layeredPaths.Add(parentDepth, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        parentIsInMergeRoots = false;
                    }
                    if (!layeredPaths[parentDepth].Contains(parentFolder))
                    {
                        layeredPaths[parentDepth].Add(parentFolder);
                        parentIsInMergeRoots = false;
                    }

                    // add to the actual list
                    if (!parentIsInMergeRoots)
                    {
                        mergeRoots.Add(parentFolder);
                    }

                    if (mergeRoots.Count - obsoletePaths.Count <= MAX_MERGE_ROOTS_COUNT)
                    {
                        break;
                    }
                }

                foreach (string obsoletePath in obsoletePaths)
                {
                    mergeRoots.Remove(obsoletePath);
                    layeredPaths[maxDepth].Remove(obsoletePath);
                }

                --maxDepth;

                if (mergeRoots.Count <= MAX_MERGE_ROOTS_COUNT)
                {
                    // remove child paths that are contained by the parent path
                    foreach (string childPath in layeredPaths[maxDepth + 1])
                    {
                        foreach (string parentPath in layeredPaths[maxDepth])
                        {
                            if (VersionControlPath.IsSubItem(childPath, parentPath))
                            {
                                mergeRoots.Remove(childPath);
                            }
                        }
                    }
                    break;
                }
            }
        }

        private bool IsPathInMappedPathsCollection(string path, ReadOnlyCollection<string> mappedPaths)
        {
            foreach (string mappedPath in mappedPaths)
            {
                if (VersionControlPath.Compare(path, mappedPath) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void processBatchedBranches(ChangeGroup group)
        {
            if (m_batchedBranches.Count == 0)
            {
                return;
            }

            TraceManager.TraceInformation("BatchedBranches: Finding branch roots from {0} branches", m_batchedBranches.Count);
            ItemSpec[] branchItemSpecs = new ItemSpec[m_batchedBranches.Count];
            HashSet<string> branchRoots = GetPathsCommonRoots(m_batchedBranches.Keys);

            VersionControlServer tfsClient = m_currentChange.Item.VersionControlServer;
            ChangesetVersionSpec version = new ChangesetVersionSpec(m_currentChange.Item.ChangesetId);

            TraceManager.TraceInformation("Selected {0} branch roots", branchRoots.Count);

            CollapseToParentTillMappedRoot(m_batchedBranches, branchRoots, GetMappedRootPathsInConfig());

            TraceManager.TraceInformation("Selected {0} branch roots after compression", branchRoots.Count);
            // For each branch root, call QueryMergeWithDetails() recursively.
            int branchIndex = 0;
            int targetDeletionId = 0;
            foreach (string branchRoot in branchRoots)
            {
                branchIndex++;

                if (m_batchedBranches.ContainsKey(branchRoot))
                {
                    targetDeletionId = m_batchedBranches[branchRoot].Item.DeletionId;
                }
                else
                {
                    // If the branchroot is not cached, it is not branched in current changeset. We are safe to use 0 as targetDeletionId.
                    targetDeletionId = 0;
                }

                ChangesetMergeDetails mergeDetails = tfsClient.QueryMergesWithDetails(
                    null,
                    version,
                    0,
                    branchRoot,
                    version,
                    targetDeletionId,
                    null,
                    version,
                    RecursionType.Full);

                ItemMerge[] mergedItems = mergeDetails.MergedItems;
                // We try to find the latest SourceVersionFrom version, which is the branch from version.
                if (mergedItems != null && mergedItems.Length > 0)
                {
                    Dictionary<string, ItemMerge> mergedItemVersions = new Dictionary<string, ItemMerge>();
                    foreach (ItemMerge mergedItem in mergedItems)
                    {
                        if (mergedItemVersions.ContainsKey(mergedItem.TargetServerItem))
                        {
                            if (mergedItemVersions[mergedItem.TargetServerItem].SourceVersionFrom < mergedItem.SourceVersionFrom)
                            {
                                mergedItemVersions[mergedItem.TargetServerItem] = mergedItem;
                            }
                        }
                        else
                        {
                            mergedItemVersions.Add(mergedItem.TargetServerItem, mergedItem);
                        }
                    }
                    TraceManager.TraceInformation("Analyzing {0}/{1}: {2} items in root: {3}",
                        branchIndex, branchRoots.Count,
                        mergedItems.Length, branchRoot);

                    foreach (KeyValuePair<string, ItemMerge> mergedItemVersion in mergedItemVersions)
                    {
                        if (m_batchedBranches.ContainsKey(mergedItemVersion.Key))
                        {
                            switch (Provider.IsMergePathMapped(mergedItemVersion.Value.SourceServerItem, mergedItemVersion.Value.TargetServerItem, mergedItemVersion.Value.SourceVersionFrom))
                            {
                                case MappingResult.NotMapped:
                                    // branch source not found conflict.
                                    MigrationConflict pathNotMappedConflict = VCPathNotMappedConflictType.CreateConflict(mergedItemVersion.Value.SourceServerItem);
                                    List<MigrationAction> retActions;
                                    ConflictResolutionResult resolutionResult =
                                        m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, pathNotMappedConflict, out retActions);
                                    if (resolutionResult.Resolved)
                                    {
                                        switch (resolutionResult.ResolutionType)
                                        {
                                            case ConflictResolutionType.UpdatedConflictedChangeAction:
                                                //TraceManager.TraceInformation("The branch operation will be changed to add as the branch parent {0} is not migrated.",
                                                //    mergedItems[i].SourceServerItem);
                                                group.CreateAction(
                                                    WellKnownChangeActionId.Add,
                                                    new TfsMigrationItem(m_batchedBranches[mergedItemVersion.Key].Item),
                                                    null,
                                                    mergedItemVersion.Key,
                                                    null,
                                                    null,
                                                    convertContentType(m_batchedBranches[mergedItemVersion.Key].Item.ItemType),
                                                    null);
                                                break;
                                            case ConflictResolutionType.ChangeMappingInConfiguration:
                                                throw new MigrationUnresolvedConflictException(pathNotMappedConflict);
                                            default:
                                                throw new MigrationUnresolvedConflictException(pathNotMappedConflict);
                                        }
                                    }
                                    else
                                    {
                                        throw new MigrationUnresolvedConflictException(pathNotMappedConflict);
                                    }
                                    break;
                                case MappingResult.Mapped:
                                    group.CreateAction(
                                        WellKnownChangeActionId.Branch,
                                        new TfsMigrationItem(m_batchedBranches[mergedItemVersion.Key].Item),
                                        mergedItemVersion.Value.SourceServerItem,
                                        mergedItemVersion.Value.TargetServerItem,
                                        mergedItemVersion.Value.SourceVersionFrom.ToString(CultureInfo.InvariantCulture),
                                        null,
                                        convertContentType(m_batchedBranches[mergedItemVersion.Key].Item.ItemType),
                                        null);
                                    break;
                                case MappingResult.MappedBeforeSnapshot:
                                    if (handleHistoryBeforeSnapshot(mergedItemVersion.Value.SourceVersionFrom.ToString(), group))
                                    {
                                        // The item is branched from a version before the snapshot, change it to Add
                                        createMigrationAction(m_batchedBranches[mergedItemVersion.Key].Item, null, group, WellKnownChangeActionId.Add);
                                    }
                                    else
                                    {
                                        throw new MigrationException("Don't know how to handle historynotfound conflict");
                                    }
                                    break;
                                case MappingResult.OutOfMergeScope:
                                    // The item is branched from a path outof merge scope, change it to Add
                                    createMigrationAction(m_batchedBranches[mergedItemVersion.Key].Item, null, group, WellKnownChangeActionId.Add);
                                    break;
                            }
                            m_batchedBranches.Remove(mergedItemVersion.Key);
                        }
                    }
                }
            }

            // If we still have remaining items at this point, their branch parents cannot be found. 
            foreach (Change branch in m_batchedBranches.Values)
            {
                raiseBranchParentNotFoundConflictForBranch(group, branch);
            }

            m_batchedBranches.Clear();
        }

        private bool handleHistoryBeforeSnapshot(string changesetId, ChangeGroup group )
        {
            MigrationConflict historyNotFoundConflict = TFSHistoryNotFoundConflictType.CreateConflict(changesetId, null);
            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult = 
                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, historyNotFoundConflict, out retActions);
            // Just skip the history action, E.g. Branch will be changed to Add, Merge will be ingonre, Branch|Merge will be changed to Add.
            if (resolutionResult.Resolved && resolutionResult.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction)
            {
                return true;
            }
            else
            {
                // SupressedConflictedChangeGroup is not allowed for MappedBeforeSnapshot
                throw new MigrationUnresolvedConflictException(historyNotFoundConflict);
            }
        }

        private ConflictResolutionResult raiseUnhandledChangeTypeConflict(ChangeGroup group, ChangeType changeType)
        {
            MigrationConflict unhandledChangeTypeConflict = UnhandledChangeTypeConflictType.CreateConflict(changeType.ToString(), m_tfsChangeTranslators.Keys);
            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, unhandledChangeTypeConflict, out retActions);
            if (!resolutionResult.Resolved)
            {
                throw new MigrationUnresolvedConflictException(unhandledChangeTypeConflict);
            }
            else
            {
                return resolutionResult;
            }
        }

        private void raiseBranchParentNotFoundConflictForBranch(ChangeGroup group, Change conflictChange)
        {
            MigrationConflict branchParentNotFoundConflict = VCBranchParentNotFoundConflictType.CreateConflict(conflictChange.Item.ServerItem);
            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, branchParentNotFoundConflict, out retActions);
            if ((resolutionResult.Resolved) && (resolutionResult.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
            {
                IMigrationAction action = group.CreateAction(
                    WellKnownChangeActionId.Add,
                    new TfsMigrationItem(conflictChange.Item),
                    null,
                    conflictChange.Item.ServerItem,
                    null,
                    null,
                    convertContentType(conflictChange.Item.ItemType),
                    null);
            }
            else
            {
                throw new MigrationUnresolvedConflictException(branchParentNotFoundConflict);
            }
        }

        private void raiseBranchParentNotFoundConflictForRename(ChangeGroup group, Change conflictChange)
        {
            MigrationConflict branchParentNotFoundConflict = VCBranchParentNotFoundConflictType.CreateConflict(conflictChange.Item.ServerItem);
            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, branchParentNotFoundConflict, out retActions);
            if ((resolutionResult.Resolved) && (resolutionResult.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
            {
                // Don't change to Add if it a deleted rename carry over by parent rename.
                if (conflictChange.Item.DeletionId == 0)
                {
                    createMigrationAction(conflictChange.Item, null, group, WellKnownChangeActionId.Add);
                }                
            }
            else
            {
                throw new MigrationUnresolvedConflictException(branchParentNotFoundConflict);
            }
        }

        private void raisePathNotMappedConflictForMerge(ChangeGroup group, Change conflictChange, string unmappedPath)
        {
            // branch source not found conflict.
            MigrationConflict contentConflict = VCPathNotMappedConflictType.CreateConflict(unmappedPath);
            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, contentConflict, out retActions);
            if ((resolutionResult.Resolved) && (resolutionResult.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
            {
                if ((conflictChange.ChangeType & (ChangeType.Branch | ChangeType.Merge)) == (ChangeType.Branch | ChangeType.Merge))
                {
                    // In case of branch|merge only change type, change it to Add. 
                    IMigrationAction action = group.CreateAction(
                    WellKnownChangeActionId.Add,
                    new TfsMigrationItem(conflictChange.Item),
                    null,
                    conflictChange.Item.ServerItem,
                    null,
                    null,
                    convertContentType(conflictChange.Item.ItemType),
                    null);
                }
                else
                {
                    // Just skip the merge bit.
                }
            }
            else
            {
                throw new MigrationUnresolvedConflictException(contentConflict);
            }
        }

        private void raiseBranchParentNotFoundConflictForMerge(ChangeGroup group, Change conflictChange)
        {
            MigrationConflict branchParentNotFoundConflict = VCBranchParentNotFoundConflictType.CreateConflict(conflictChange.Item.ServerItem);
            List<MigrationAction> retActions;
            ConflictResolutionResult resolutionResult =
                m_provider.ConflictManager.TryResolveNewConflict(group.SourceId, branchParentNotFoundConflict, out retActions);
            if ((resolutionResult.Resolved) && (resolutionResult.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
            {
                if ((conflictChange.ChangeType & (ChangeType.Branch | ChangeType.Merge))
                    == (ChangeType.Branch | ChangeType.Merge))
                {
                    // In case of branch|merge only change type, change it to Add. 
                    IMigrationAction action = group.CreateAction(
                    WellKnownChangeActionId.Add,
                    new TfsMigrationItem(conflictChange.Item),
                    null,
                    conflictChange.Item.ServerItem,
                    null,
                    null,
                    convertContentType(conflictChange.Item.ItemType),
                    null);
                }
                else
                {
                    // Just skip the merge bit.
                }
            }
            else
            {
                throw new MigrationUnresolvedConflictException(branchParentNotFoundConflict);
            }
        }

        /// <summary>
        /// Add a branch action to the change group. 
        /// action.SourceItem -> branched to item in source Tfs; 
        /// action.BranchFromVersion -> branch parent item version on source tfs
        /// action.TargetSourceItem -> branch parent on target system; 
        /// action.TargetTargetItem -> branched to item on target system
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void Branch(Change change, ChangeGroup group)
        {
            m_batchedBranches.Add(change.Item.ServerItem, change);
        }

        /// <summary>
        /// Calls Merge and Undelete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void MergeUndelete(Change change, ChangeGroup group)
        {
            Merge(change, group);
            Undelete(change, group);
        }

        /// <summary>
        /// Calls Merge and Delete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void MergeDelete(Change change, ChangeGroup group)
        {
            // In whidbey and Sp1, you cannot merge a delete action on an item.
            //Merge(change, group);
            Delete(change, group);
        }

        /// <summary>
        /// Calls Branch and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void BranchEdit(Change change, ChangeGroup group)
        {
            Branch(change, group);
            Edit(change, group);
        }

        /// <summary>
        /// Calls Branch and Delete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void BranchDelete(Change change, ChangeGroup group)
        {
            // We will change Branch to Merge() and change MergeDelete() to Delete.
            // So BranchDelete will be skipped.
        }

        /// <summary>
        /// Calls Branch and Delete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void BranchMergeDelete(Change change, ChangeGroup group)
        {
            // We will change BranchMerge() to Merge() and change MergeDelete() to Delete.
            // So BranchMergeDelete will be skipped.
        }

        /// <summary>
        /// Calls BranchMerge and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void BranchMergeEdit(Change change, ChangeGroup group)
        {
            BranchMerge(change, group);
            Edit(change, group);
        }

        /// <summary>
        /// Calls Merge and DeleteUndelete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void MergeDeleteUndelete(Change change, ChangeGroup group)
        {
            Merge(change, group);
            DeleteUndelete(change, group);
        }

        /// <summary>
        /// Calls Merge and RenameDeleteUndelete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void MergeRenameDeleteUndelete(Change change, ChangeGroup group)
        {
            Merge(change, group);
            RenameDeleteUndelete(change, group);
        }

        /// <summary>
        /// Calls RenameUndelete and Delete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameDeleteUndelete(Change change, ChangeGroup group)
        {
            Undelete(change, group);
            RenameDelete(change, group);
        }

        /// <summary>
        /// Calls RenameUndelete and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameEditUndelete(Change change, ChangeGroup group)
        {
            RenameUndelete(change, group);
            // Edit will always be pended for file undelete.
        }

        /// <summary>
        /// Calls MergeUndelete and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void MergeEditUndelete(Change change, ChangeGroup group)
        {
            MergeUndelete(change, group);
            // Edit will always be pended for file undelete.
        }


        /// <summary>
        /// Calls Merge and Edit to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void MergeEdit(Change change, ChangeGroup group)
        {
            Merge(change, group);
            Edit(change, group);
        }

        /// <summary>
        /// Performs the rename operation using the same rules as the Rename method (without calling Rename) and then
        /// calls Delete to add the appropriate actions to the change group.
        /// </summary>
        /// <param name="change">The change being processed</param>
        /// <param name="group">The change grouing this change is to be a part of</param>
        internal virtual void RenameDelete(Change change, ChangeGroup group)
        {
            rename(change, group, false);
            Delete(change, group);
        }

        private void validateChangeAndGroup(Change change, ChangeGroup group)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            Debug.Assert(group.Status != ChangeStatus.Complete);
            m_currentChange = change;
        }

        /// <summary>
        /// Create a basic action.
        /// </summary>
        /// <param name="changeItem"></param>
        /// <param name="group"></param>
        /// <param name="actionId"></param>
        /// <returns></returns>
        private IMigrationAction createMigrationAction(
            Item changeItem,
            string fromPath,
            ChangeGroup group,
            Guid actionId)
        {

            IMigrationAction action = group.CreateAction(
                actionId,
                new TfsMigrationItem(changeItem),
                fromPath,
                changeItem.ServerItem,
                null,
                null,
                convertContentType(changeItem.ItemType),
                null);

            return action;
        }

        internal abstract void BuildChangeTranslators();

        protected void addHandler(ChangeType changeType, TfsAnalysisAlgorithm algorithm)
        {
            Debug.Assert(m_tfsChangeTranslators != null);

            if (m_tfsChangeTranslators.ContainsKey(changeType))
            {
                throw new MigrationException(string.Format(CultureInfo.InvariantCulture, "There already exists a handler for: {0}", changeType));
            }

            m_tfsChangeTranslators[changeType] = algorithm;
        }
    }

    /// <summary>
    /// TFS2008 implementation of analysis algorithms.
    /// </summary>
    internal class Tfs2008AnalysisAlgorithms : TfsAnalysisAlgorithms
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal Tfs2008AnalysisAlgorithms(TfsVCAnalysisProvider provider)
            : base(provider)
        {
        }

        internal override void BuildChangeTranslators()
        {
            TfsChangeTranslators = new Dictionary<ChangeType, TfsAnalysisAlgorithm>();

            addHandler(ChangeType.Add, Add);
            addHandler(ChangeType.Add | ChangeType.Edit, Add);

            addHandler(ChangeType.Branch, Branch);
            addHandler(ChangeType.Branch | ChangeType.Merge, BranchMerge);

            addHandler(ChangeType.Branch | ChangeType.Merge | ChangeType.Delete, BranchMergeDelete);
            addHandler(ChangeType.Branch | ChangeType.Merge | ChangeType.Edit, BranchMergeEdit);

            addHandler(ChangeType.Rename, Rename);
            addHandler(ChangeType.Rename | ChangeType.Edit, RenameEdit);
            addHandler(ChangeType.Rename | ChangeType.Undelete, RenameUndelete);
            addHandler(ChangeType.Rename | ChangeType.Merge, RenameMerge);
            addHandler(ChangeType.Rename | ChangeType.Merge | ChangeType.Delete, RenameDeleteMerge);
            addHandler(ChangeType.Rename | ChangeType.Merge | ChangeType.Delete | ChangeType.Edit, RenameDeleteMerge);
            addHandler(ChangeType.Rename | ChangeType.Edit | ChangeType.Merge, RenameEditMerge);
            addHandler(ChangeType.Rename | ChangeType.Undelete | ChangeType.Merge, RenameUndeleteMerge);
            addHandler(ChangeType.Rename | ChangeType.Edit | ChangeType.Undelete, RenameEditUndelete);
            addHandler(ChangeType.Rename | ChangeType.Edit | ChangeType.Undelete | ChangeType.Merge, RenameEditUndeleteMerge);
            addHandler(ChangeType.Rename | ChangeType.Delete | ChangeType.Undelete, RenameDeleteUndelete);
            addHandler(ChangeType.Rename | ChangeType.Delete, RenameDelete);

            addHandler(ChangeType.Merge, Merge);
            addHandler(ChangeType.Merge | ChangeType.Delete, MergeDelete);
            addHandler(ChangeType.Merge | ChangeType.Delete | ChangeType.Undelete, MergeDeleteUndelete);
            addHandler(ChangeType.Merge | ChangeType.Rename | ChangeType.Delete | ChangeType.Undelete, MergeRenameDeleteUndelete);
            addHandler(ChangeType.Merge | ChangeType.Undelete, MergeUndelete);
            addHandler(ChangeType.Merge | ChangeType.Edit, MergeEdit);
            addHandler(ChangeType.Merge | ChangeType.Edit | ChangeType.Undelete, MergeEditUndelete);
            // Special change type combinations.
            addHandler(ChangeType.Merge | ChangeType.Rename | ChangeType.Add, BranchMerge);
            addHandler(ChangeType.Merge | ChangeType.Rename | ChangeType.Add | ChangeType.Undelete, BranchMerge);
            addHandler(ChangeType.Merge | ChangeType.Add, Add);

            addHandler(ChangeType.Branch | ChangeType.Edit, BranchEdit);
            addHandler(ChangeType.Branch | ChangeType.Delete, BranchDelete);

            addHandler(ChangeType.Undelete, Undelete);
            addHandler(ChangeType.Delete, Delete);
            addHandler(ChangeType.Edit, Edit);
            addHandler(ChangeType.Edit | ChangeType.Undelete, EditUndelete);
            addHandler(ChangeType.Delete | ChangeType.Undelete, DeleteUndelete);

            // Rollback|Delete|Edit will be mapped to delete only.
            addHandler(ChangeType.Delete | ChangeType.Edit, Delete);

            addHandler(ChangeType.None, None);


            addHandler(ChangeType.Rollback, None); 
            addHandler(ChangeType.SourceRename, None); 
        }
    }
}
