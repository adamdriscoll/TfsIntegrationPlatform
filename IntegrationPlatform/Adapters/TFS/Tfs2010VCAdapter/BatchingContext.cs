// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;
using System.Linq;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    /// <summary>
    /// The BatchingContext defines a method of performing operations against a TFS server
    /// in batches instead of as single operations.  This reduces round-trips with the server
    /// for large changesets.
    /// 
    /// This class takes care of both the batching and also ensuring that the item exists in the
    /// workspace prior to pending the change.  For this reason it is not necessary to "Get" an 
    /// item before acting on it.  This class does not perform a "Get" though as item download is
    /// unnecessary since the item is coming from the source system during migration.
    /// </summary>
    public sealed class BatchingContext
    {
        /// <summary>
        /// Creates a batching context associated with the provided workspace.
        /// </summary>
        /// <param name="workspace"></param>
        public BatchingContext(
            Workspace workspace, 
            ConfigurationService configurationService, 
            ConflictManager conflictManager, 
            int localWorkspaceVersion)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            // the default batch size of 200 is the TFS server team recomendation
            m_batchSize = configurationService.GetValue<int>(
                TfsConstants.TfsBatchSizeVariableName, 200);

            m_retryLimit = configurationService.GetValue<int>("RetryLimit", 10);
            m_secondsToWait = configurationService.GetValue<int>("RetryDelaySeconds", 30);
            m_addItemNotFound = configurationService.GetValue<bool>("AddItemNotFound", true);
            m_sourceId = new Guid(configurationService.MigrationSource.InternalUniqueId);
            m_conflictManager = conflictManager;
            m_implicitRenames = m_changeOpt.ImplicitRenames;
            m_implicitAdds = m_changeOpt.ImplicitAdds;
            if (m_secondsToWait < 1)
            {
                m_secondsToWait = 1;
            }

            m_workspace = workspace;
            m_localWorkspaceVersion = localWorkspaceVersion;
        }

        /// <summary>
        /// Event fired when an error occurs while processing a single item (e.g. add, edit, delete)
        /// </summary>
        public event EventHandler<BatchedItemEventArgs> BatchedItemError;
        
        /// <summary>
        /// Event fired when a warning occurs while processing a single item (e.g. add/undelete 
        /// existing item, delete non existing item)
        /// </summary>
        public event EventHandler<BatchedItemEventArgs> BatchedItemWarning;

        /// <summary>
        /// Event fired when an error occurs while processing a merge action (e.g. merge)
        /// </summary>
        public event EventHandler<BatchedMergeErrorEventArgs> MergeError;


        /// <summary>
        /// Adds an item to TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="path">The TFS or local workspace path of the item to add to TFS.</param>
        /// <param name="downloadItem">The item to be downloaded from source system</param>
        public void PendAdd(string path, IMigrationItem downloadItem)
        {
            addSingleItem(null, path, downloadItem, WellKnownChangeActionId.Add);
        }

        /// <summary>
        /// Deletes an item from TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="path">The TFS or local workspace path of the item to delete from TFS.</param>
        public void PendDelete(string fromPath, string path)
        {
            addSingleItem(fromPath, path, null, WellKnownChangeActionId.Delete);
        }

        private void addSingleItem(string fromPath, string path, IMigrationItem downloadItem, Guid action)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string source = null;
            if (!string.IsNullOrEmpty(fromPath))
            {
                tryGetAsServerPath(fromPath, out source);
            }
            string target;
            if (tryGetAsServerPath(path, out target))
            {
                BatchedItem item = new BatchedItem(source, target, action, downloadItem);
                addServerPathsToList(item);
            }
        }

        /// <summary>
        /// Edits an item in TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="path">The TFS or local workspace path of the item to edit in TFS.</param>
        /// <param name="downloadItem">The item to be downloaded from source system</param>
        public void PendEdit(string fromPath, string path, string version, IMigrationItem downloadItem)
        {
            string source = null;
            if (!string.IsNullOrEmpty(fromPath))
            {
                tryGetAsServerPath(fromPath, out source);
            }
            string target;
            if (tryGetAsServerPath(path, out target))
            {
                BatchedItem item = new BatchedItem(source, target, WellKnownChangeActionId.Edit, version, downloadItem);
                addServerPathsToList(item);
            }
        }

        /// <summary>
        /// Undeletes an item in TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="source">The source path of the item to undelete in TFS.</param>
        /// <param name="target">The target path of the item to undelete in TFS.</param>
        /// <param name="version">The version in which the item was deleted</param>
        public void PendUndelete(string fromPath, string Path, string deletedVersion, IMigrationItem downloadItem)
        {
            string source;
            string target;

            if (tryGetAsServerPath(fromPath, out source) && tryGetAsServerPath(Path, out target))
            {
                BatchedItem undeleteItem = new BatchedItem(source, target, deletedVersion, downloadItem);
                addServerPathsToList(undeleteItem);
                m_undeletesToBePended.Add(undeleteItem.Source, false);
            }
        }

        /// <summary>
        /// Renames an item in TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="source">The TFS or local workspace path of the source item of the rename operation.</param>
        /// <param name="target">The TFS or local workspace path of the target item of the rename operation.</param>
        public void PendRename(string source, string target)
        {
            string leftItem;
            string rightItem;

            if (tryGetAsServerPath(source, out leftItem) && tryGetAsServerPath(target, out rightItem))
            {
                BatchedItem renameItem = new BatchedItem(leftItem, rightItem, WellKnownChangeActionId.Rename);

                addServerPathsToList(renameItem);
            }
        }

        /// <summary>
        /// Branches an item in TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="source">The TFS or local workspace path of the source item of the branch operation.</param>
        /// <param name="target">The TFS or local workspace path of the target item of the branch operation.</param>
        /// <param name="version">The branch from version</param>
        public void PendBranch(string source, string target, string version)
        {
            string sourceItem;
            string targetItem;

            if (tryGetAsServerPath(source, out sourceItem) && tryGetAsServerPath(target, out targetItem))
            {
                BatchedItem item = new BatchedItem(sourceItem, targetItem, RecursionType.Full, version);
                addServerPathsToList(item);
            }
        }


        /// <summary>
        /// Merges an item in TFS.  The pend action will be sent to TFS in a batch when the 
        /// Flush operation is performed.
        /// </summary>
        /// <param name="recurse">Recursive type of the merge operation</param>
        /// <param name="source">The TFS or local workspace path of the source item of the merge operation.</param>
        /// <param name="target">The TFS or local workspace path of the target item of the merge operation.</param>
        /// <param name="mergeVersionFrom">The starting version of the merge.</param>
        /// <param name="mergeVersionTo">The end version of the merge.</param>
        /// <param name="downloadItem">The item to be downloaded from source system</param>
        public void PendMerge(string source, 
            string target, 
            RecursionType recurse,
            string mergeVersionFrom,
            string mergeVersionTo,
            IMigrationItem downloadItem)
        {
            string sourceItem;
            string targetItem;

            if (tryGetAsServerPath(source, out sourceItem) && tryGetAsServerPath(target, out targetItem))
            {
                BatchedItem bmi = new BatchedItem(sourceItem, targetItem, recurse, mergeVersionFrom, mergeVersionTo, downloadItem);
                addServerPathsToList(bmi);
            }
        }

        private void addServerPathsToList(BatchedItem item)
        {
            /* -1 is infinite lock - flushing causes round-trips 
             * to the server so it could take a while
             */
            m_flushLock.AcquireReaderLock(-1);

            try
            {
                m_changeOpt.Add(item);
            }
            finally
            {
                m_flushLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Takes a local or server path and sets the out parameter to the TFS server path 
        /// (converting the local path to a server path) if, and only if, the resulting server
        /// path is also mapped in the workspace.  If the input path is either not a mapped path
        /// or is not a valid server path the out parameter is set to null and false is returned.
        /// </summary>
        /// <param name="path">A workspace local path or a TFS server path</param>
        /// <param name="serverPath">A TFS server path of the input item if the input item is mapped in the current workspace</param>
        /// <returns>True if a mapped server path was found, false otherwise.</returns>
        private bool tryGetAsServerPath(string path, out string serverPath)
        {
            serverPath = null;

            try
            {
                if (VersionControlPath.IsServerItem(path))
                {
                    if (m_workspace.IsServerPathMapped(path))
                    {
                        serverPath = path;
                    }
                }
                else
                {
                    serverPath = m_workspace.TryGetServerItemForLocalItem(path);
                }
            }
            catch (ArgumentException ex)
            {
                // Path is invalid
                OnBatchedItemWarning(new BatchedItem(path, WellKnownChangeActionId.Unknown), ex.Message);
            }

            return serverPath != null;
        }

        /// <summary>
        /// Batches the queued operations and submits them to the TFS server.  The operations are submitted
        /// in the following order:
        /// 
        /// 1) Deletes
        /// 2) Adds
        /// 3) Undeletes
        /// 4) Edits
        /// 5) Branches
        /// 6) Merges
        /// 7) Renames
        /// 
        /// The order is important as it allows new items to be added in the namespace of deleted items.  It
        /// also ensures that edits are pended before renames are executed.
        /// </summary>
        public void Flush()
        {
            /* 
             * This will prevent the collections from being modified during the flush operation.
             * Since the Pend* methods will end up clearing the collections this is important.
             */

            m_flushLock.AcquireWriterLock(-1);

            try
            {
                m_changeOpt.RevisePreviousNames();
                m_changeOpt.PreProcessMerges();
                m_items = m_changeOpt.Resolve();

                TraceManager.TraceInformation("Finished scheduling!");

                if (m_items.Count > 0)
                {
                    int currentPriority = m_items[0].Priority;

                    foreach (BatchedItem ci in m_items)
                    {
                        if (ci.Priority != currentPriority)
                        {
                            TraceManager.TraceVerbose(String.Format("Pending changes with priority {0}", currentPriority));
                            pendChanges();
                            currentPriority = ci.Priority;
                        }
                        if (!m_currentItems.ContainsKey(ci.Action))
                        {
                            m_currentItems.Add(ci.Action, new List<BatchedItem>());
                        }

                        m_currentItems[ci.Action].Add(ci);
                    }

                    TraceManager.TraceVerbose("Pending remaining changes");
                    pendChanges();

                    m_changeOpt.Clear();
                }
            }
            finally
            {
                m_flushLock.ReleaseWriterLock();
            }
        }

        void pendChanges()
        {
            pendUndeletes();
            pendBranches();
            pendDeletes();
            pendAdds();
            pendEdits();
            // Try add again for those edits without the original version on Tfs system
            pendAdds();
            pendRenames();
            pendMerges();

            m_currentItems.Clear();
        }

        private void pendMerges()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Merge);

            if (batchedItems.Count == 0)
            {
                return;
            }

            // Sort the batched items so that parent item is always processed before sub items. 
            batchedItems.Sort(compareBatchedItemByTargetPathLength);

            int progress = 0;
            int progressCount = 0;

            setLocalVersions(batchedItems, InvalidateAcceptMineOnMissingItem, RecursionType.None);

            foreach (BatchedItem mergeItem in batchedItems)
            {
                progress++;
                if (progress >= 1000)
                {
                    TraceManager.TraceInformation("Now processing {0} of {1} Merges", progressCount * 1000 + progress, batchedItems.Count);
                    progress = 0;
                    progressCount++;
                }

                GetStatus stat = null;

                mergeItem.DownloadItem.Download(m_workspace.GetLocalItemForServerItem(mergeItem.Target));

                VersionSpec mergeVersionFrom = new ChangesetVersionSpec(int.Parse(mergeItem.Version, CultureInfo.InvariantCulture));
                VersionSpec mergeVersionTo = new ChangesetVersionSpec(int.Parse(mergeItem.MergeVersionTo, CultureInfo.InvariantCulture));
                
                try
                {
                    stat = m_workspace.Merge(mergeItem.Source, mergeItem.Target, mergeVersionFrom, mergeVersionTo,
                        LockLevel.None, mergeItem.Recursion, mergeItem.MergeOption);

                    if (stat.NumConflicts > 0)
                    {
                        string[] filter = new string[] { mergeItem.Target };
                        Conflict[] conflicts = m_workspace.QueryConflicts(filter, mergeItem.Recursion != RecursionType.None);
                        foreach (Conflict conflict in conflicts.AsParallel())
                        {
                            conflict.Resolution = Resolution.AcceptTheirs;
                            m_workspace.ResolveConflict(conflict);
                            TraceManager.TraceInformation("Resolved conflict '{0}' as 'AcceptTheirs'", conflict.ConflictId);
                        }
                    }

                    if ((mergeItem.MergeOption & MergeOptions.AlwaysAcceptMine) != MergeOptions.AlwaysAcceptMine && !mergeItem.AlwaysAcceptMineInvalidated)
                    {
                        if (stat.NumFailures > 0 || !VerifyOrUndoMergeWithoutDiscard(mergeItem))
                        {
                            TraceManager.TraceWarning("Unable to perform regular merge on '{0}'; will retry merge with /discard.", mergeItem.Target);

                            // A merge attempt without the AlwaysAcceptMine option (/discard) generated unexpected implicit pending changes
                            // Retry with the merge options set to the /discard equivalent
                            mergeItem.MergeOption |= MergeOptions.AlwaysAcceptMine;

                            stat = m_workspace.Merge(mergeItem.Source, mergeItem.Target, mergeVersionFrom, mergeVersionTo,
                                LockLevel.None, mergeItem.Recursion, mergeItem.MergeOption);
                        }
                    }

                }
                catch (NoMergeRelationshipException)
                {
                    stat = m_workspace.Merge(mergeItem.Source, mergeItem.Target, mergeVersionFrom, mergeVersionTo,
                        LockLevel.None, mergeItem.Recursion, mergeItem.MergeOption | MergeOptions.AlwaysAcceptMine | MergeOptions.Baseless);
                }
                catch (VersionControlException vce)
                {
                    OnMergeError(
                        mergeItem,
                        null,
                        vce);

                    continue;
                }

                // OnMergeError could throw a VersionControlException so we do not want this code
                // inside the try block.
                if (stat == null || !getStatusIndicatesSuccess(stat))
                {
                    OnMergeError(
                        mergeItem,
                        stat,
                        null);
                }
                else
                {
                    TraceManager.TraceVerbose("Pended merge from '{0}' to '{1}'", mergeItem.Source, mergeItem.Target);
                }
            }
        }

        private static bool getStatusIndicatesSuccess(GetStatus status)
        {
            // if we have performed at least one operation or is no action is needed (merge /discard)
            // and we don't have failure, conflicts, warnings (resolvable or not), then we are true, otherwise false.
            return ((status.NumOperations > 0) || status.NoActionNeeded) &&
                    (status.NumFailures == 0) &&
                    (status.NumConflicts == 0);
        }

        private bool VerifyOrUndoMergeWithoutDiscard(BatchedItem mergeItem)
        {
            // Check the pending ChangeTypes are a subset of the ChangeTypes of the change types from the source system as recorded in ChangeOptimizer.MergeAssociatedChanges
            bool pendingMergeHasDifferentChangeTypes = false;
            PendingChange[] pendingChanges = m_workspace.GetPendingChanges(mergeItem.Target);
            if (pendingChanges.Length == 0)
            {
                string msg = String.Format(CultureInfo.InvariantCulture, 
                    "Unexpected error: Found no pending changes for merge Target path '{0}'", mergeItem.Target);
                TraceManager.TraceError(msg);
                Debug.Fail(msg);
                pendingMergeHasDifferentChangeTypes = true;
            }
            else
            {
                PendingChange pendingChange = pendingChanges[0];

                List<BatchedItem> changesToSameItemAsMerge;
                if (!m_changeOpt.MergeAssociatedChanges.TryGetValue(mergeItem.Target, out changesToSameItemAsMerge))
                {
                    string msg = String.Format(CultureInfo.InvariantCulture, 
                        "Unexpected error: Failed to find merge item Target path '{0}' in ChangeOptimizer.MergeAssociatedChanges", mergeItem.Target);
                    TraceManager.TraceError(msg);
                    Debug.Fail(msg); 
                    pendingMergeHasDifferentChangeTypes = true;
                }
                else
                {
                    ChangeType expectedChangeTypes = GetExpectedChangeTypesForMerge(changesToSameItemAsMerge);
                    // Remove any change types that we never migrate with a merge from the comparison
                    ChangeType strippedPendingChangeTypes = pendingChange.ChangeType
                        & ~ChangeType.Encoding & ~ChangeType.Lock & ~ChangeType.SourceRename & ~ChangeType.Rollback;
                    strippedPendingChangeTypes &= VersionSpecificUtils.SupportedChangeTypes;

                    if (((int)(strippedPendingChangeTypes & ~expectedChangeTypes)) != 0)
                    {
                        pendingMergeHasDifferentChangeTypes = true;
                    }
                }

                if (pendingMergeHasDifferentChangeTypes)
                {
                    m_workspace.Undo(pendingChanges);
                }
            }
           
            return !pendingMergeHasDifferentChangeTypes;
        }

        private ChangeType GetExpectedChangeTypesForMerge(List<BatchedItem> itemsAssociatedWithMerge)
        {
            ChangeType changeType = ChangeType.Merge;

            foreach (BatchedItem item in itemsAssociatedWithMerge)
            {
                if (item.Action == WellKnownChangeActionId.Add)
                {
                    changeType |= ChangeType.Add;
                }
                else if (item.Action == WellKnownChangeActionId.Branch)
                {
                    changeType |= ChangeType.Branch;
                }
                else if (item.Action == WellKnownChangeActionId.Delete)
                {
                    changeType |= ChangeType.Delete;
                }
                else if (item.Action == WellKnownChangeActionId.Edit)
                {
                    changeType |= ChangeType.Edit;
                }
                else if (item.Action == WellKnownChangeActionId.Rename)
                {
                    changeType |= ChangeType.Rename;
                }
                else if (item.Action == WellKnownChangeActionId.Undelete)
                {
                    changeType |= ChangeType.Undelete;
                }
                // Other types are not pended by the migration provider except for Merge which was assigned when changeType was initialized
            }

            return changeType;
        }

        private void pendBranches()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Branch);

            if (batchedItems.Count == 0)
            {
                return;
            }

            // Sort the batched items so that parent item is always processed before sub items. 
            batchedItems.Sort(compareBatchedItemBySourcePathLength);

            GetStatus stat = null;
            List<Conflict> allConflicts = new List<Conflict>();
            foreach (BatchedItem branchPath in batchedItems)
            {
                try
                {
                    stat = m_workspace.Merge(
                        branchPath.Source,
                        branchPath.Target,
                        null,
                        new ChangesetVersionSpec(branchPath.Version),
                        LockLevel.Unchanged,
                        RecursionType.None,
                        MergeOptions.Baseless);

                    if (stat.NumConflicts > 0)
                    {
                        string[] filter = new string[] { branchPath.Target };
                        Conflict[] conflicts = m_workspace.QueryConflicts(filter, false);
                        foreach (Conflict conflict in conflicts.AsParallel())
                        {
                            conflict.Resolution = Resolution.AcceptTheirs;
                            m_workspace.ResolveConflict(conflict);
                            TraceManager.TraceInformation("Resolved conflict '{0}' as 'AcceptTheirs'", conflict.ConflictId);
                        }
                    }
                }
                catch (VersionControlException vce)
                {
                    OnBatchedItemError(
                        branchPath,
                        vce);
                }

                // OnMergeError could throw a VersionControlException so we do not want this code
                // inside the try block.
                if (stat == null || !getStatusIndicatesSuccess(stat))
                {
                    OnMergeError(
                        branchPath,
                        stat,
                        null);
                }

                m_pendedAdditiveChanges.Add(branchPath.Target);
            }
        }

        /// <summary>
        /// compare source path length of two batcheditem. If x's source path length is smaller, x is less than y.
        /// </summary>
        /// <param name="x">batched item 1</param>
        /// <param name="y">batched item 2</param>
        /// <returns>positive value if x's source length is larger. negative value if y's source length is larger.
        /// 0 if length is equal or either one is null.</returns>
        private static int compareBatchedItemBySourcePathLength(BatchedItem x, BatchedItem y)
        {
            if ((x == null) || (y == null))
            {
                return 0;
            }

            return (x.Source.Length - y.Source.Length);
        }

        /// <summary>
        /// compare target path length of two batcheditem. If x's target path length is smaller, x is less than y.
        /// </summary>
        /// <param name="x">batched item 1</param>
        /// <param name="y">batched item 2</param>
        /// <returns>positive value if x's target length is larger. negative value if y's target length is larger.
        /// 0 if length is equal or either one is null.</returns>
        private static int compareBatchedItemByTargetPathLength(BatchedItem x, BatchedItem y)
        {
            if ((x == null) || (y == null))
            {
                return 0;
            }

            return (x.Target.Length - y.Target.Length);
        }

        void throwOnMissingItem(BatchedItem serverItem)
        {
            MigrationConflict missingItemConflict;

            string missingItem;
            if (serverItem.Action == WellKnownChangeActionId.Rename)
            {
                missingItem = serverItem.Source;
            }
            else
            {
                missingItem = serverItem.Target;
            }

            missingItemConflict = VCMissingItemConflictType.CreateConflict(missingItem);


            throw new MigrationException(
                string.Format(TfsVCAdapterResource.Culture,
                TfsVCAdapterResource.TfsItemMissing, 
                missingItem)
                );
        }

        void InvalidateAcceptMineOnMissingItem(BatchedItem serverItem)
        {
            serverItem.MergeOption = serverItem.MergeOption & (~MergeOptions.AlwaysAcceptMine);
            serverItem.AlwaysAcceptMineInvalidated = true;
        }

        void changeToAddOnMissingItem(BatchedItem serverItem)
        {
            BatchedItem changedItem = new BatchedItem(null, serverItem.Target, WellKnownChangeActionId.Add, serverItem.DownloadItem);
            if ( !m_currentItems.ContainsKey(WellKnownChangeActionId.Add))
            {
                m_currentItems.Add(WellKnownChangeActionId.Add, new List<BatchedItem>());
            }
            m_currentItems[WellKnownChangeActionId.Add].Add(changedItem);
            serverItem.Skip = true;
        }

        /// <summary>
        /// Revise the rename from name according to pended parent renames 
        /// E.g. rename fld1->fld2, rename fld1/1.txt->fld3/2.txt,
        /// If we already the pend rename for fld1->fld2, we need to revise the 2nd rename to fld2/1.txt->fld3/2.txt
        /// </summary>
        /// <param name="renameItem"></param>
        private void reviseSourceName(BatchedItem renameItem)
        {
            string immediateParentPendedRenameSource = null;
            KeyValuePair<string, string> immediateParentPendedRename = new KeyValuePair<string, string>();
            foreach (KeyValuePair<string, string> pendedRename in m_pendedRenames)
            {
                // Both renameItem.Source and pendedRename.Key are original paths before any rename operations
                if (VersionControlPath.IsSubItem(renameItem.Source, pendedRename.Key))
                {
                    if ((immediateParentPendedRenameSource == null) || (immediateParentPendedRename.Key.Length < pendedRename.Key.Length))
                    {
                        immediateParentPendedRename = pendedRename;
                        immediateParentPendedRenameSource = pendedRename.Key;
                    }
                }
            }

            if (immediateParentPendedRenameSource != null)
            {
                renameItem.AdjustSourceForParentRename(immediateParentPendedRename.Value + renameItem.Source.Substring(immediateParentPendedRename.Key.Length));
            }
        }

        private void pendRenames()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Rename);

            // If an Undelete|Rename is changed to Add|Rename, we should skip the Rename.
            removeItemsAlreadyAdded(batchedItems);

            // Sort rename actions so that the parent target items are always processed first. 
            // Parent target items need to be there first, otherwise, they will be pended as Add by child's rename
            batchedItems.Sort(compareBatchedItemByTargetPathLength);

            setLocalVersions(batchedItems, throwOnMissingItem, RecursionType.Full, true);

            for (int i = 0; i < batchedItems.Count; i++)
            {
                tryPendRename(i, batchedItems);
            }
        }

        /// <summary>
        /// Try pend the rename in the specified index. If there is a path length dependency, call tryPendRename() recursively.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="batchedItems"></param>
        private void tryPendRename(int index, List<BatchedItem> batchedItems)
        {
            if (m_pendedRenames.ContainsKey(batchedItems[index].Source))
            {
                // The rename has been pended earlier, just return. 
                return;
            }
            
            // Only re-schedule if
            // 1. parent item is renamed from short path to long path.
            // 2. sub item is renamed from long path to short path.
            // 3. The combined intermediate name is longer than 259 characters.
            if (batchedItems[index].Source.Length < batchedItems[index].Target.Length)
            {
                for (int j = index + 1; j < batchedItems.Count; j++)
                {
                    if (VersionControlPath.IsSubItem(batchedItems[j].Target, batchedItems[index].Target)
                        && VersionControlPath.IsSubItem(batchedItems[j].Source, batchedItems[index].Source))
                    {
                        try
                        {
                            VersionControlPath.Combine(batchedItems[index].Target, VersionControlPath.MakeRelative(batchedItems[j].Source, batchedItems[index].Source));
                        }
                        catch (Exception)
                        {
                            // Path too long, pend the sub item rename first.
                            tryPendRename(j, batchedItems);
                        }
                    }
                }
            }

            // All path length dependencies have been cleared, pend the rename. 
            pendRenameChange(batchedItems[index]);
        }

        private void pendRenameChange(BatchedItem batchedItem)
        {
            bool tryAgain = false;
            string orginalRenameSource = batchedItem.Source;
            reviseSourceName(batchedItem);
            try
            {
                TraceManager.TraceVerbose(String.Format(CultureInfo.InvariantCulture,
                    "Pending rename from '{0}' to '{1}' ...", batchedItem.Source, batchedItem.Target));
                m_workspace.PendRename(batchedItem.Source, batchedItem.Target, LockLevel.None,
                    true, false);
            }
            catch (SystemException)
            {
                // For case-only rename of folder, a nonfatal error of SystemException will be thrown. 
                // But the rename is actually pended successfully. We should continue in this situation.
                bool renamePended = false;
                PendingChange[] pendingChanges = m_workspace.GetPendingChanges(batchedItem.Target, RecursionType.None, false);
                for (int i = 0; i < pendingChanges.Length; i++)
                {
                    if ((pendingChanges[i].ChangeType & ChangeType.Rename) == ChangeType.Rename)
                    {
                        renamePended = true;
                        break;
                    }
                }
                if (!renamePended)
                {
                    throw;
                }
            }
            catch (ChangeAlreadyPendingException)
            {
                tryAgain = true;
                PendingChange[] pendingChanges = m_workspace.GetPendingChanges(batchedItem.Target);
                foreach (PendingChange change in pendingChanges)
                {
                    if (change.IsBranch)
                    {
                        m_workspace.Undo(batchedItem.Target);
                    }
                }
            }
            catch (VersionControlException vce)
            {
                tryAgain = true;
                OnBatchedItemError(
                    batchedItem,
                    vce);
            }

            if (tryAgain)
            {
                try
                {
                    TraceManager.TraceVerbose(String.Format(CultureInfo.InvariantCulture,
                        "Pending rename from '{0}' to '{1}' ...", batchedItem.Source, batchedItem.Target));
                    m_workspace.PendRename(batchedItem.Source, batchedItem.Target, LockLevel.None, true, false);
                }
                catch (SystemException)
                {
                    // For case-only rename of folder, a nonfatal error of SystemException will be thrown. 
                    // But the rename is actually pended successfully. We should continue in this situation.
                    bool renamePended = false;
                    PendingChange[] pendingChanges = m_workspace.GetPendingChanges(batchedItem.Target, RecursionType.None, false);
                    for (int i = 0; i < pendingChanges.Length; i++)
                    {
                        if ((pendingChanges[i].ChangeType & ChangeType.Rename) == ChangeType.Rename)
                        {
                            renamePended = true;
                            break;
                        }
                    }
                    if (!renamePended)
                    {
                        throw;
                    }
                }
                catch (VersionControlException vce)
                {
                    // Continue if the rename is actually pended. 
                    bool renamePended = false;
                    PendingChange[] pendingChanges = m_workspace.GetPendingChanges(batchedItem.Target, RecursionType.None, false);
                    for (int i = 0; i < pendingChanges.Length; i++)
                    {
                        if ((pendingChanges[i].ChangeType & ChangeType.Rename) == ChangeType.Rename)
                        {
                            renamePended = true;
                            break;
                        }
                    }
                    if (!renamePended)
                    {
                        throw vce;
                    }
                }
            }
            if (m_pendedRenameToPath.ContainsKey(orginalRenameSource))
            {
                // The source of this rename was the target of a previously pended rename indicating a rename chain
                // (used to break rename cycles for example)
                // Combine to a single entry in m_pendedRenames and m_pendedRenameToPath
                if (m_pendedRenameToPath.ContainsKey(batchedItem.Target))
                {
                    TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, "Unexpected condition: duplicate rename target path: '{0}'",
                        batchedItem.Target));
                }
                else
                {
                    string chainOriginalSource = m_pendedRenameToPath[orginalRenameSource];
                    m_pendedRenames[chainOriginalSource] = batchedItem.Target;
                    m_pendedRenameToPath.Remove(orginalRenameSource);
                    m_pendedRenameToPath.Add(batchedItem.Target, chainOriginalSource);
                }
            }
            else
            {
                if (m_pendedRenames.ContainsKey(orginalRenameSource))
                {
                    TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, "Unexpected condition: duplicate rename source path: '{0}'",
                        orginalRenameSource));
                }
                else if (m_pendedRenameToPath.ContainsKey(batchedItem.Target))
                {
                    TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, "Unexpected condition: duplicate rename target path: '{0}'",
                        batchedItem.Target));
                }
                else
                {
                    m_pendedRenames.Add(orginalRenameSource, batchedItem.Target);
                    m_pendedRenameToPath.Add(batchedItem.Target, orginalRenameSource);
                }
            }

            if (m_pendedAdditiveChanges.Contains(batchedItem.Target))
            {
                TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, "Unexpected condition: there was already an additive change pended for target path: '{0}'",
                    batchedItem.Target));
            }
            else
            {
                m_pendedAdditiveChanges.Add(batchedItem.Target);
            }
        }

        /// <summary>
        /// For rename|undelete and rename|edit, if the undelete is changed to add. We need to skip the combined edit and rename change.
        /// </summary>
        /// <param name="batchedItems"></param>
        private void removeItemsAlreadyAdded(List<BatchedItem> batchedItems)
        {
            // In the case when Undelete is changed to Add, Undelete|Edit becomes Add|Edit. Skip the Edit in this case. 
            for (int i = batchedItems.Count-1; i >=0; i --)
            {
                if (AddChangedFromUndelete.ContainsKey(batchedItems[i].Target) && AddChangedFromUndelete[batchedItems[i].Target])
                {
                    batchedItems.RemoveAt(i);
                }
            }
        }

        private void pendEdits()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Edit);

            if (batchedItems.Count == 0)
            {
                return;
            }

            int progress = 0;
            int progressCount = 0;

            // If an Undelete|Edit is changed to Add|Edit, we should skip the Edit.
            removeItemsAlreadyAdded(batchedItems);

            if (m_addItemNotFound)
            {
                setLocalVersions(batchedItems, changeToAddOnMissingItem, RecursionType.None);
            }
            else
            {
                setLocalVersions(batchedItems, throwOnMissingItem, RecursionType.None);
            }

            foreach (BatchedItem[] serverPaths in chunkCollection(batchedItems))
            {
                // For Branch|Merge|Edit, we need to re-do a get before pend edits.
                List<string> getBeforePendEdits = new List<string>();
                foreach (BatchedItem item in serverPaths)
                {
                    if ((!string.IsNullOrEmpty(item.Version)) && (item.Version.Equals("1", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        getBeforePendEdits.Add(item.Target);
                    }
                }
                if (getBeforePendEdits.Count > 0)
                {
                    m_workspace.Get(getBeforePendEdits.ToArray(), VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);
                }

                // Download will possibaly mark the item as skipped. 
                // So we need to keep this order.
                downloadItems(serverPaths);
                string[] items = createItemArray(serverPaths);


                if (items.Length == 0)
                {
                    continue;
                }

                bool tryAgain = true;
                try
                {
                    m_workspace.PendEdit(items);
                    tryAgain = false;
                }
                catch (VersionControlException vce)
                {
                    OnBatchedItemWarning(
                        null,
                        vce.Message);
                }

                if (tryAgain)
                {
                    int index = 0;
                    foreach (string item in items)
                    {
                        try
                        {
                            m_workspace.PendEdit(item);
                        }
                        catch (VersionControlException vceInner)
                        {
                            OnBatchedItemError(
                                new BatchedItem(item, WellKnownChangeActionId.Edit),
                                vceInner);
                        }

                        index++;
                    }
                }
                progress = progress + 200;
                if (progress >= 1000)
                {
                    TraceManager.TraceInformation("Now processing {0} of {1} Edits", progressCount * 1000 + progress, batchedItems.Count);
                    progress = 0;
                    progressCount++;
                }
            }
        }

        private void downloadItems(BatchedItem[] itemsToBeDownloaded)
        {
            foreach (BatchedItem itemToBeDownloaded in itemsToBeDownloaded)
            {
                downloadItem(itemToBeDownloaded);
            }            
        }

        private void downloadItem(BatchedItem itemToBeDownloaded)
        {
            try
            {
                itemToBeDownloaded.DownloadItem.Download(m_workspace.GetLocalItemForServerItem(itemToBeDownloaded.Target));
            }
            catch (VersionControlException e)
            {
                MigrationConflict itemNotFoundConflict = TfsItemNotFoundConflictType.CreateConflict(e.Message, itemToBeDownloaded.Target);
                List<MigrationAction> retActions;
                ConflictResolutionResult result = m_conflictManager.TryResolveNewConflict(m_sourceId, itemNotFoundConflict, out retActions);
                if ((result.Resolved) && (result.ResolutionType == ConflictResolutionType.SkipConflictedChangeAction))
                {
                    itemToBeDownloaded.Skip = true;
                    return;
                }
                else
                {
                    throw new MigrationUnresolvedConflictException(itemNotFoundConflict);
                }
            }
        }

        private string[] createItemArray(BatchedItem[] serverPaths)
        {
            string[] items = new string[serverPaths.Length];
            int counter = 0;
            
            foreach (BatchedItem serverItem in serverPaths)
            {
                if (!serverItem.Skip)
                {
                    items[counter++] = serverItem.Target;
                }
                else
                {
                    TraceManager.TraceInformation("Skipping item {0}", serverItem.Target);
                }
            }

            if (items.Length != counter)
            {
                Array.Resize<string>(ref items, counter);
            }

            return items;
        }

        List<BatchedItem> compressRecursiveDeletes(List<BatchedItem> batchedItems)
        {
            // Sort the list so that parent item is always list earlier.
            batchedItems.Sort(compareBatchedItemByTargetPathLength);
            // Reverse the list so that we can remove items from list in place
            batchedItems.Reverse();

            HashSet<string> parentDeletes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<BatchedItem> childDeleteToBeSkipped = new List<BatchedItem>();

            string parentPath;
            bool parentDeleted;
            for (int i = batchedItems.Count -1; i >= 0; i --)
            {
                parentPath = batchedItems[i].Target;
                parentDeleted = false;
                while (!VersionControlPath.Equals(parentPath, VersionControlPath.RootFolder))
                {
                    parentPath = VersionControlPath.GetFolderName(parentPath);
                    if (parentDeletes.Contains(parentPath))
                    {
                        parentDeleted = true;
                        break;
                    }
                }
                if (parentDeleted)
                {
                    batchedItems.RemoveAt(i);
                }
                else
                {
                    parentDeletes.Add(batchedItems[i].Target);
                }
            }
            return batchedItems;
        }

        /// <summary>
        /// This is a TFS2010 behavior change. The delete will take the rename-from-name if it is a combination of merge|rename|delete. 
        /// We need to revert the delete target back in this situation.
        /// </summary>
        /// <param name="batchedItems"></param>
        private void reviseDeleteTargetNames(List<BatchedItem> batchedItems)
        {
            string parentPath = null;

            foreach (BatchedItem deleteItem in batchedItems)
            {
                parentPath = deleteItem.Target;
                while (!VersionControlPath.Equals(parentPath, VersionControlPath.RootFolder))
                {
                    if (m_pendedRenames.ContainsKey(parentPath) && !m_pendedRenameToPath.ContainsKey(parentPath))
                    {
                        deleteItem.Target = VersionControlPath.Combine(m_pendedRenames[parentPath],
                                VersionControlPath.MakeRelative(deleteItem.Target, parentPath));
                        Trace.TraceInformation("{0} is changed based on rename from {1} to {2}. Dev10 merge|rename|delete issue.",
                            deleteItem.Target, parentPath, m_pendedRenames[parentPath]);
                        break;
                    }
                    parentPath = VersionControlPath.GetFolderName(parentPath);
                }
            }
        }

        private void pendDeletes()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Delete);
            if (batchedItems.Count == 0)
            {
                return;
            }

            batchedItems = compressRecursiveDeletes(batchedItems);
            reviseDeleteTargetNames(batchedItems);

            int progress = 0;
            int progressCount = 0;
            setLocalVersions(batchedItems, skipMissingItem, RecursionType.None);

            foreach (BatchedItem[] serverPaths in chunkCollection(batchedItems))
            {

                string[] items = createItemArray(serverPaths);

                if (items.Length > 0)
                {

                    bool tryAgain = true;
                    try
                    {
                        m_workspace.PendDelete(items);
                        tryAgain = false;
                    }
                    catch (VersionControlException vce)
                    {
                        OnBatchedItemWarning(
                            null,
                            vce.Message);
                    }

                    if (tryAgain)
                    {
                        int index = 0;

                        foreach (string item in items)
                        {
                            try
                            {
                                m_workspace.PendDelete(item);
                            }
                            catch (VersionControlException vceInner)
                            {
                                OnBatchedItemError(
                                    serverPaths[index],
                                    vceInner);
                            }

                            index++;
                        }
                    }
                }

                progress = progress + 200;
                if (progress >= 1000)
                {
                    TraceManager.TraceInformation("Now processing {0} of {1} Deletes", progressCount * 1000 + progress, batchedItems.Count);
                    progress = 0;
                    progressCount++;
                }
            }
        }

        private List<BatchedItem> getCurrent(Guid changeAction)
        {
            if (m_currentItems.ContainsKey(changeAction))
            {
                return m_currentItems[changeAction];
            }

            List<BatchedItem> newList = new List<BatchedItem>(0);
            m_currentItems.Add(changeAction, newList);

            return newList;
        }

        private void skipMissingItem(BatchedItem serverPath)
        {
            serverPath.Skip = true;
            OnBatchedItemWarning(serverPath, string.Format(TfsVCAdapterResource.Culture, TfsVCAdapterResource.SkipMissingItem, serverPath.Target,
                serverPath.Action.ToString()));
        }

        private void pendAdds()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Add);

            // Sort the batched items so that parent item is always processed before sub items. 
            // This step is necessary for recursive download. 
            batchedItems.Sort(compareBatchedItemByTargetPathLength);

            int progress = 0;
            int progressCount = 0;

            foreach (BatchedItem[] serverPaths in chunkCollection(batchedItems))
            {
                // Download will possibaly mark the item as skipped. 
                // So we need to keep this order.
                downloadItems(serverPaths);
                string[] items = createItemArray(serverPaths);

                if (items.Length <= 0)
                {
                    continue;
                }
                
                bool tryAgain = true;

                try
                {
                    m_workspace.PendAdd(items);
                    tryAgain = false;
                }
                catch (Exception e)
                {
                    if (e is VersionControlException || e is MigrationException) 
                    {
                        // If we catch ItemNotMappedException, then item to be added is mapped but its parent is not mapped.
                        // We will add its parent to the mapping and try pendAdd again.
                        if ( !(e is ItemNotMappedException))
                        {
                            OnBatchedItemError(
                                new BatchedItem("Unknown", WellKnownChangeActionId.Add),
                                e);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                if (tryAgain)
                {
                    foreach (string item in items)
                    {
                        int index = 0;
                        try
                        {
                            if (!m_workspace.IsServerPathMapped(VersionControlPath.GetFolderName(item)))
                            {
                                DirectoryInfo parentLocalDir = Directory.GetParent(m_workspace.GetLocalItemForServerItem(item));
                                m_workspace.Map(VersionControlPath.GetFolderName(item), parentLocalDir.FullName);
                            }
                            m_workspace.PendAdd(item);
                        }
                        catch (Exception eInner)
                        {
                            if (eInner is VersionControlException || eInner is MigrationException)
                            {
                                OnBatchedItemError(
                                    serverPaths[index],
                                    eInner);
                            }
                            else
                            {
                                throw;
                            }
                        }

                        index++;
                    }
                }

                foreach ( BatchedItem pendedAdd in serverPaths)
                {
                    m_pendedAdditiveChanges.Add(pendedAdd.Target);
                    if (AddChangedFromUndelete.ContainsKey(pendedAdd.Target))
                    {
                        AddChangedFromUndelete[pendedAdd.Target] = true;
                    }
                }

                progress = progress + 200;
                if (progress >= 1000)
                {
                    TraceManager.TraceInformation("Now processing {0} of {1} Adds", progressCount * 1000 + progress, batchedItems.Count);
                    progress = 0;
                    progressCount++;
                }
            }
            // Clear all pended add operations.
            batchedItems.Clear();
        }

        private static ItemSpec[] getItemSpecsFromServerPath(BatchedItem[] serverPaths)
        {
            ItemSpec[] specs = new ItemSpec[serverPaths.Length];

            int current = 0;
            int i = 0;
            for (; i < serverPaths.Length; i++)
            {
                if (!serverPaths[i].Skip)
                {
                    specs[current++] = new ItemSpec(serverPaths[i].Target, RecursionType.None);
                }
            }

            if (current != specs.Length)
            {
                Array.Resize<ItemSpec>(ref specs, current);
            }

            return specs;
        }

        private void pendUndeletes()
        {
            List<BatchedItem> batchedItems = getCurrent(WellKnownChangeActionId.Undelete);
            List<string> subUndeleteToBeUndone = new List<string>();

            if (batchedItems.Count > 0)
            {
                foreach (BatchedItem batchedItem in batchedItems)
                {
                    if (m_undeletesToBePended.ContainsKey(batchedItem.Source) && (m_undeletesToBePended[batchedItem.Source]))
                    {
                        // The undelete is already pended by the parent recursively.
                        if (m_undeletesToBePended[batchedItem.Source])
                        {
                            m_pendedAdditiveChanges.Add(batchedItem.Source);
                        }
                        continue;
                    }
                    // Get the deletionId of the item to be undeleted.
                    ItemSpec[] itemSpec = { new ItemSpec(batchedItem.Source, RecursionType.Full) };
                    ItemSet[] sets = m_workspace.VersionControlServer.GetItems(itemSpec, VersionSpec.Latest, DeletedState.Deleted,
                        Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any);
                    Debug.Assert(sets.Length == 1);
                    if (sets[0].Items.Length == 0)
                    {
                        // There is no deleted item to be undeleted, change to add.
                        downloadItem(batchedItem);
                        if (!batchedItem.Skip)
                        {
                            m_workspace.PendAdd(batchedItem.Target);
                            AddChangedFromUndelete.Add(batchedItem.Target, true);
                        }
                    }
                    else
                    {
                        Item deletedItem = sets[0].Items[0];
                        bool deletedVersionfound = false;

                        if (sets[0].Items.Length > 1)
                        {
                            int deletedChangeset = int.Parse(batchedItem.Version, CultureInfo.InvariantCulture);
                            foreach (Item item in sets[0].Items)
                            {
                                if (item.ChangesetId == deletedChangeset)
                                {
                                    deletedItem = item;
                                    deletedVersionfound = true;
                                    break;
                                }
                            }
                            if (!deletedVersionfound)
                            {
                                OnBatchedItemWarning(batchedItem,
                                    string.Format(TfsVCAdapterResource.Culture,
                                    TfsVCAdapterResource.DeletedVersionNotFound, 
                                    batchedItem.Target));                                    
                            }
                        }
                        using (UpdateLocalVersionQueue q = new UpdateLocalVersionQueue(m_workspace))
                        {
                            //Update the item version with the deleted changeset.
                            q.QueueUpdate(deletedItem.ItemId, m_workspace.GetLocalItemForServerItem(deletedItem.ServerItem),
                                deletedItem.ChangesetId);
                            q.Flush();
                        }
                        try
                        {
                            // Always undelete to previous name. 
                            m_workspace.PendUndelete(batchedItem.Source, deletedItem.DeletionId, batchedItem.Source);
                            PendingChange[] pendedChanges = m_workspace.GetPendingChanges(batchedItem.Source, RecursionType.Full);
                            if (pendedChanges.Length == 0)
                            {
                                m_implicitAdds.Add(batchedItem.Source);
                            }
                            else if (deletedItem.ItemType == ItemType.Folder)
                            {
                                foreach (PendingChange pendedChange in pendedChanges)
                                {
                                    if ((!VersionControlPath.Equals(batchedItem.Source, pendedChange.ServerItem)) && 
                                        ((pendedChange.ChangeType & ChangeType.Undelete) == ChangeType.Undelete))
                                    {
                                        if (m_undeletesToBePended.ContainsKey(pendedChange.ServerItem))
                                        {
                                            m_undeletesToBePended[pendedChange.ServerItem] = true;
                                        }
                                        else
                                        {
                                            subUndeleteToBeUndone.Add(pendedChange.ServerItem);
                                        }
                                    }
                                }

                            }
                        }
                        catch (VersionControlException vceInner)
                        {
                            OnBatchedItemError(
                                batchedItem,
                                vceInner);
                        }
                    }
                    m_pendedAdditiveChanges.Add(batchedItem.Source);
                }
            }

            // Undo undeletes pended by parent recursively that doesn't show up in the original change group.
            if (subUndeleteToBeUndone.Count > 0)
            {
                m_workspace.Undo(subUndeleteToBeUndone.ToArray());
            }
        }

        internal List<T[]> chunkCollection<T>(List<T> paths)
        {
            List<T[]> batches = new List<T[]>();
            int length = paths.Count;

            int currentIndex = 0;
            int remaining = length - currentIndex;

            while (remaining > 0)
            {
                int currentBatchSize = (remaining < m_batchSize) ? remaining : m_batchSize;
                batches.Add(
                    paths.GetRange(
                        currentIndex,
                        currentBatchSize
                    ).ToArray()
                );

                currentIndex += currentBatchSize;
                remaining = length - currentIndex;
            }

            return batches;
        }

        delegate void onMissingItem(BatchedItem serverPath);

        private void setLocalVersions(List<BatchedItem> serverPaths, onMissingItem missingItem, RecursionType recursionType)
        {
            setLocalVersions(serverPaths, missingItem, recursionType, false);
        }

        private void setLocalVersions(List<BatchedItem> serverPaths, onMissingItem missingItem, RecursionType recursionType, bool useSourcePath)
        {
            if ((serverPaths == null) || (serverPaths.Count == 0))
            {
                return;
            }

            List<ItemSpec> itemSpecs = new List<ItemSpec>();
            List<BatchedItem> batchedItemToBeQueried = new List<BatchedItem>();

            foreach (BatchedItem batchedItem in serverPaths)
            {
                bool hasPendingChange = false;

                if ((batchedItem.Action == WellKnownChangeActionId.Edit)
                    && (!string.IsNullOrEmpty(batchedItem.Version))
                    && (batchedItem.Version.Equals("1", StringComparison.InvariantCultureIgnoreCase)))
                {
                    hasPendingChange = true;
                }
 

                string currentPath = useSourcePath ? batchedItem.Source : batchedItem.Target;

                while (VersionControlPath.Compare(currentPath, VersionControlPath.RootFolder) != 0)
                {
                    if (useSourcePath)
                    {
                        if (m_pendedRenames.ContainsKey(currentPath))
                        {
                            reviseSourceName(batchedItem);
                            hasPendingChange = true;
                            break;
                        }
                    }
                    else
                    {
                        if (m_pendedRenames.ContainsValue(currentPath))
                        {
                            hasPendingChange = true;
                            break;
                        }
                    }
                    if (m_pendedAdditiveChanges.Contains(currentPath))
                    {
                        hasPendingChange = true;
                        break;
                    }
                    currentPath = VersionControlPath.GetFolderName(currentPath);
                }

                if (!hasPendingChange)
                {
                    itemSpecs.Add(new ItemSpec((useSourcePath) ? batchedItem.Source : batchedItem.Target, recursionType));
                    batchedItemToBeQueried.Add(batchedItem);
                }
            }

            if (itemSpecs.Count == 0)
            {
                return;
            }
            int batchSize = 10000;
            int index = 0;
            int round = -1;
            using (UpdateLocalVersionQueue q = new UpdateLocalVersionQueue(m_workspace))
            {
                ItemSpec[] itemSpecsArray = null;
                // Perform a fake get on the items
                while (index < itemSpecs.Count)
                {
                    
                    if ((index + batchSize) < itemSpecs.Count)
                    {
                        itemSpecsArray = new ItemSpec[batchSize];
                        itemSpecs.CopyTo(index, itemSpecsArray, 0, batchSize);
                        index = index + batchSize;
                        round++;
                    }
                    else
                    {
                        itemSpecsArray = new ItemSpec[itemSpecs.Count - index];
                        itemSpecs.CopyTo(index, itemSpecsArray, 0, itemSpecs.Count - index);
                        index = itemSpecs.Count;
                        round++;
                    }

                    ItemSet[] sets = m_workspace.VersionControlServer.GetItems(itemSpecsArray, new ChangesetVersionSpec(m_localWorkspaceVersion), DeletedState.NonDeleted,
                    Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any);
                    Debug.Assert(sets.Length == itemSpecsArray.Length);

                    bool succeeded = false;
                    int counter = 0;

                    while (!succeeded)
                    {
                        try
                        {
                            for (int i = 0; i < itemSpecsArray.Length; i++)
                            {
                                Item[] items = sets[i].Items;
                                if (items.Length > 0)
                                {
                                    foreach (Item item in items)
                                    {
                                        q.QueueUpdate(item.ItemId, m_workspace.GetLocalItemForServerItem(item.ServerItem), item.ChangesetId);
                                    }
                                }
                                else
                                {
                                    missingItem(batchedItemToBeQueried[round * batchSize + i]);
                                }
                            }

                            q.Flush();
                            succeeded = true;
                        }
                        catch (RepositoryNotFoundException)
                        {
                            counter++;
                            if (counter > m_retryLimit)
                            {
                                throw;
                            }

                            for (int i = 0; i < m_secondsToWait; i++)
                            {
                                System.Threading.Thread.Sleep(1000);
                            };
                        }
                    }

                }                
            }
        }

        private void OnBatchedItemError(BatchedItem item, Exception exception)
        {
            if (BatchedItemError != null)
            {
                BatchedItemError(this, new BatchedItemEventArgs(item, exception, exception.Message));
            }
            else
            {
                Debug.Fail("An error occurred but there was no listener");

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        private void OnBatchedItemWarning(BatchedItem item, string message)
        {
            if (BatchedItemWarning != null)
            {
                BatchedItemWarning(this, new BatchedItemEventArgs(item, null, message));
            }
        }


        private void OnMergeError(BatchedItem item, GetStatus status, Exception exception)
        {
            if (MergeError != null)
            {
                MergeError(this, new BatchedMergeErrorEventArgs(item, status, exception));
            }
            else
            {
                Debug.Fail("An error occurred but there was no listener");

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        internal void CacheLabel(IMigrationAction labelAction)
        {
            LabelFromMigrationAction labelFromMigrationAction = new LabelFromMigrationAction(labelAction);
            m_labelCache.Add(labelFromMigrationAction);
            foreach (ILabelItem unattachedLabelItem in m_unattachedLabelItems)
            {
                m_labelCache[m_labelCache.Count - 1].LabelItems.Add(unattachedLabelItem);
            }
            m_unattachedLabelItems.Clear();
        }

        internal void CacheLabelItem(IMigrationAction labelItemAction)
        {
            ILabelItem labelItemFromAction = new LabelItemFromMigrationAction(labelItemAction);
            if (m_labelCache.Count == 0)
            {
                // CacheLabel has not been called yet with a Label MigrationAction, so hold any
                // label items in m_unattachedLabelItems until one comes in (this code should
                // not assume the ordering)
                m_unattachedLabelItems.Add(labelItemFromAction);
            }
            else
            {
                // Add the label item to the Label for the most recently received label action
                m_labelCache[m_labelCache.Count - 1].LabelItems.Add(labelItemFromAction);
            }
        }

        public void CreateLabels()
        {
            foreach (ILabel label in m_labelCache)
            {
                if (label.LabelItems.Count > 0)
                {
                    VersionControlLabel tfsLabel = new VersionControlLabel(
                        m_workspace.VersionControlServer,
                        label.Name,
                        string.IsNullOrEmpty(label.OwnerName) ? null : label.OwnerName,
                        label.Scope,
                        label.Comment);

                    List<LabelItemSpec> labelItemSpecList = new List<LabelItemSpec>();
                    foreach (ILabelItem labelItem in label.LabelItems)
                    {
                        ItemSpec itemSpec = new ItemSpec(labelItem.ItemCanonicalPath, labelItem.Recurse ? RecursionType.Full : RecursionType.None);
                        labelItemSpecList.Add(new LabelItemSpec(itemSpec, VersionSpec.Latest, false));
                    }
                    AddLabelToServer(label, tfsLabel, labelItemSpecList);
                }
            }
        }

        private void AddLabelToServer(ILabel label, VersionControlLabel tfsLabel, List<LabelItemSpec> labelItemSpecList)
        {
            LabelResult[] labelResults = new LabelResult[0];
            Exception labelExistsException = null;
            try
            {
                labelResults = m_workspace.VersionControlServer.CreateLabel(tfsLabel, labelItemSpecList.ToArray(), LabelChildOption.Fail);
            }
            catch (IllegalLabelNameException illegalLabelNameException)
            {
                TraceManager.TraceError(illegalLabelNameException.ToString());
            }
            /* TODO: Reinstate this after changing VCInvalidLabelNameConflictType.CreateConflict to take ILabel rather than MigraitonAction

            catch (IllegalLabelNameException illegalLabelNameException)
            {
                MigrationConflict invalidLabelNameConflict =
                    VCInvalidLabelNameConflictType.CreateConflict((MigrationAction)action, illegalLabelNameException.Message);
                List<MigrationAction> retActions;
                ConflictResolutionResult result = m_conflictManager.TryResolveNewConflict(action.ChangeGroup.SourceId, invalidLabelNameConflict, out retActions);

                if ((result.Resolved) && (result.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
                {
                    // When the VCInvalidLabelNameConflictHandler resolves the conflict, it changes action.ChangeGroup.Name (which holds the label
                    // name for a label change group) to a valid label name.   Recreate the VersionControlLabel object with this name and try again.
                    tfsLabel = new VersionControlLabel(m_workspace.VersionControlServer, action.ChangeGroup.Name, action.ChangeGroup.Owner, action.Path, action.ChangeGroup.Comment);
                    labelResults = m_workspace.VersionControlServer.CreateLabel(tfsLabel, labelItemSpecList.ToArray(), LabelChildOption.Fail);
                }
            }
             */
            catch (LabelExistsException tfsLabelExistsException)
            {
                labelExistsException = tfsLabelExistsException;
            }
            catch (LabelDuplicateItemException tfsDuplicateItemException)
            {
                labelExistsException = tfsDuplicateItemException;
            }
            catch (Exception e)
            {
                // TODO: Add call to ErrorManager instead
                TraceManager.TraceError(String.Format(TfsVCAdapterResource.ExceptionCreatingLabel, tfsLabel.Name, e.Message));
                throw;
            }
            /* TODO: Reinstate this after changing VCLabelAlreadyExistsConflictType.CreateConflict to take ILabel rather than MigraitonAction
            if (labelExistsException != null)
            {
                MigrationConflict labelExistsConflict =
                    VCLabelAlreadyExistsConflictType.CreateConflict((MigrationAction)action, labelExistsException.Message);
                List<MigrationAction> retActions;
                ConflictResolutionResult result = m_conflictManager.TryResolveNewConflict(action.ChangeGroup.SourceId, labelExistsConflict, out retActions);

                if ((result.Resolved) && (result.ResolutionType == ConflictResolutionType.UpdatedConflictedChangeAction))
                {
                    // When the VCInvalidLabelNameConflictHandler resolves the conflict, it changes action.ChangeGroup.Name (which holds the label
                    // name for a label change group) to a valid label name.   Recreate the VersionControlLabel object with this name and try again.
                    tfsLabel = new VersionControlLabel(m_workspace.VersionControlServer, action.ChangeGroup.Name, action.ChangeGroup.Owner, action.Path, action.ChangeGroup.Comment);
                    labelResults = m_workspace.VersionControlServer.CreateLabel(tfsLabel, labelItemSpecList.ToArray(), LabelChildOption.Fail);
                }
            }
             */

            foreach (LabelResult result in labelResults)
            {
                if (result.Status == LabelResultStatus.Created || result.Status == LabelResultStatus.Updated)
                {
                    if (labelItemSpecList.Count == 1)
                    {
                        TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture,
                            "{0} label with name '{1}' and scope '{2}' with 1 item: '{3}' (recursive: {4})",
                            result.Status.ToString(),
                            tfsLabel.Name,
                            tfsLabel.Scope,
                            labelItemSpecList[0].ItemSpec.Item.ToString(),
                            labelItemSpecList[0].ItemSpec.RecursionType == RecursionType.Full));

                    }
                    else
                    {
                        // TODO: Put strings in resouces
                        TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture, "{0} label with name '{1}' and scope '{2}' with {3} items",
                            result.Status.ToString(), tfsLabel.Name, tfsLabel.Scope, labelItemSpecList.Count));
                    }
                }
                else
                {
                    TraceManager.TraceError(String.Format(CultureInfo.InvariantCulture, "Unexpected status when creating label '{0}': {1}",
                        tfsLabel.Name, result.Status.ToString()));
                }
            }
        }

        // bool is true if the add is already pended.
        public Dictionary<string, bool> AddChangedFromUndelete
        {
            get
            {
                return m_AddChangedFromUndelete;
            }
        }

        public HashSet<string> ImplicitRenames
        {
            get
            {
                return m_implicitRenames;
            }
        }

        public HashSet<string> ImplicitAdds
        {
            get
            {
                return m_implicitAdds;
            }
        }

        Workspace m_workspace;
        ConflictManager m_conflictManager;
        Guid m_sourceId;
        /* TODO: Consider using to translate Label Scope path
        VCTranslationService m_translationService;
        Guid m_otherSideSourceId;
        */

        ChangeOptimizer m_changeOpt = new ChangeOptimizer();

        Dictionary<Guid, List<BatchedItem>> m_currentItems = new Dictionary<Guid,List<BatchedItem>>();
        ReadOnlyCollection<BatchedItem> m_items;

        Dictionary<string, string> m_pendedRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> m_pendedRenameToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> m_pendedAdditiveChanges = new HashSet<string>();
        Dictionary<string, bool> m_undeletesToBePended = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, bool> m_AddChangedFromUndelete = new Dictionary<string, bool>();
        HashSet<string> m_implicitRenames;
        HashSet<string> m_implicitAdds;
        List<ILabel> m_labelCache = new List<ILabel>();
        List<ILabelItem> m_unattachedLabelItems = new List<ILabelItem>();
        
        // used to block write access to the collections by other threads when flushing
        ReaderWriterLock m_flushLock = new ReaderWriterLock();
        int m_localWorkspaceVersion;

        int m_batchSize;
        int m_retryLimit;
        int m_secondsToWait;
        bool m_addItemNotFound;
    }
}
