// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    public class ChangeOptimizer
    {
        List<BatchedItem> m_resolvedChanges = new List<BatchedItem>();
        List<BatchedItem> m_unresolvedChanges = new List<BatchedItem>();
        List<BatchedItem> m_unresolvedRenames = new List<BatchedItem>();
        List<BatchedItem> m_unresolvedAdditiveActions = new List<BatchedItem>();
        HashSet<string> m_implicitRenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<BatchedItem>> m_mergeAssociatedChanges = new Dictionary<string, List<BatchedItem>>(StringComparer.OrdinalIgnoreCase);
        // Value is the parent rename BatchedItem causing the implicit rename
        Dictionary<string, BatchedItem> m_implicitRenamesWithParentItems = new Dictionary<string, BatchedItem>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> m_implicitAdds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, BatchedItem> m_renamePairs = new Dictionary<string, BatchedItem>();
        List<List<BatchedItem>> m_renameCycles = new List<List<BatchedItem>>();

        object m_locker = new object();

        bool acceptingNewChanges = true;

        public HashSet<string> ImplicitRenames
        {
            get
            {

                return m_implicitRenames;
            }
        }

        /// <summary>
        /// These adds are created implicitly by rename a folder to below itself. 
        /// On TFS 2008, it shows as an Add in the changeset. 
        /// On TFS 2010, it doesn't show up in the changeset.
        /// </summary>
        public HashSet<string> ImplicitAdds
        {
            get
            {
                return m_implicitAdds;
            }
        }

        /// <summary>
        /// Return all changes associated with a merge on a particular path
        /// The key is the server path that is the target of a merge operation
        /// The value is a list of all of the BatchedItems for the current change group that operate on the same path as the merge target path
        /// </summary>
        public Dictionary<string, List<BatchedItem>> MergeAssociatedChanges
        {
            get
            {
                return m_mergeAssociatedChanges;
            }
        }

        public void Clear()
        {
            lock (m_locker)
            {
                m_resolvedChanges.Clear();
                m_unresolvedChanges.Clear();
                m_unresolvedRenames.Clear();
                m_unresolvedAdditiveActions.Clear();
                m_implicitRenamesWithParentItems.Clear();
                m_renamePairs.Clear();
                m_renameCycles.Clear();
                m_mergeAssociatedChanges.Clear();
                acceptingNewChanges = true;
            }
        }

        /// <summary>
        /// Revise the Path of Edit, Delete and Merge according to renames in the same change group.
        /// </summary>
        /// <param name="group"></param>
        public void RevisePreviousNames()
        {
            if (m_renamePairs.Count == 0)
            {
                return;
            }

            foreach (BatchedItem change in m_unresolvedChanges)
            {
                if ((change.Action == WellKnownChangeActionId.Edit) || (change.Action == WellKnownChangeActionId.Delete))
                {
                    if (m_renamePairs.ContainsKey(change.Target))
                    {
                        change.Source = m_renamePairs[change.Target].Source;
                    }
                }

                else if ((change.Action == WellKnownChangeActionId.Merge))
                {
                    if (m_renamePairs.ContainsKey(change.Target))
                    {
                        change.Target = m_renamePairs[change.Target].Source;
                    }
                }
            }
        }

        public void Add(BatchedItem change)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            lock (m_locker)
            {
                if (!acceptingNewChanges)
                {
                    Debug.Fail("ChangeOprimizer is not accepting new changes");
                }

                // IMPORTANT NOTE: Currently every BatchedItem passed to this method is added to one of the three lists:
                // m_unresolvedRenames, m_unresolvedAdditiveActions, or m_unresolvedChanges
                // The method PreProcessMerges depends on that fact, so if that changes here, PreProcessMerges should be
                // checked for changes needed as well

                if (change.Action == WellKnownChangeActionId.Rename)
                {
                    m_unresolvedRenames.Add(change);

                    if (!m_renamePairs.ContainsKey(change.Target))
                    {
                        m_renamePairs.Add(change.Target, change);
                    }
                }
                else if ((change.Action == WellKnownChangeActionId.Branch)
                    || (change.Action == WellKnownChangeActionId.Add)
                    || (change.Action == WellKnownChangeActionId.Undelete))
                {
                    m_unresolvedAdditiveActions.Add(change);
                }
                else
                {
                    //Merges, Deletes, Edits or Encodings
                    m_unresolvedChanges.Add(change);
                }
            }
        }

        public void PreProcessMerges()
        {
            m_mergeAssociatedChanges.Clear();

            // Populate a list of just the merge BatchedItems keyed by the target path of the merge
            foreach (BatchedItem change in m_unresolvedChanges)
            {
                if ((change.Action == WellKnownChangeActionId.Merge))
                {
                    if (!m_mergeAssociatedChanges.ContainsKey(change.Target))
                    {
                        m_mergeAssociatedChanges.Add(change.Target, new List<BatchedItem>());
                        m_mergeAssociatedChanges[change.Target].Add(change);
                    }
                }
            }

            if (m_mergeAssociatedChanges.Count > 0)
            {
                int nonDiscardMergesFound = 0;
                foreach (List<BatchedItem> unresolvedItems in new List<BatchedItem>[] { m_unresolvedChanges, m_unresolvedAdditiveActions, m_unresolvedRenames })
                {
                    foreach (BatchedItem batchedItem in unresolvedItems)
                    {
                        // Merges have already been added to m_mergeAssociatedChanges above
                        if (batchedItem.Action != WellKnownChangeActionId.Merge)
                        {
                            string itemPath = (batchedItem.Source == null) ? batchedItem.Target : batchedItem.Source;

                            Debug.Assert(itemPath != null, String.Format("itemPath is null for action {0} on item with source '{1}' and target '{2}'",
                                batchedItem.Action, batchedItem.Source, batchedItem.Target));

                            List<BatchedItem> mergeChanges = null;
                            if (m_mergeAssociatedChanges.TryGetValue(itemPath, out mergeChanges))
                            {
                                if (batchedItem.Action == WellKnownChangeActionId.Delete ||
                                    batchedItem.Action == WellKnownChangeActionId.Edit ||
                                    batchedItem.Action == WellKnownChangeActionId.Undelete)
                                {
                                    // When any of the above types are combined with a merge are going to try the merge without the /discard option
                                    // (MergeOptions.AlwaysAcceptMine in the API)
                                    m_mergeAssociatedChanges[itemPath][0].MergeOption &= (~MergeOptions.AlwaysAcceptMine);
                                    nonDiscardMergesFound++;
                                }

                                // We will need to compare the ChangeTypes pended after pending the merge (because there may be implicit ones) and make sure there are no
                                // implicit change types we are not expecting.  So we need to keep track of all of the change types we are expecting
                                // for an item being merged.
                                m_mergeAssociatedChanges[itemPath].Add(batchedItem);
                            }
                        }
                    }
                }

                int discardMerges = m_mergeAssociatedChanges.Count - nonDiscardMergesFound;
                TraceManager.TraceVerbose("Found {0} Merge actions; {1} of these will use 'tf merge /discard' behavior",
                    m_mergeAssociatedChanges.Count, discardMerges);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void detectConflicts()
        {
            // Sort m_unresolvedRenames by path length with shorter paths first
            m_unresolvedRenames.Sort(delegate(BatchedItem lhs, BatchedItem rhs)
            {
                return lhs.Source.Length.CompareTo(rhs.Source.Length);
            });

            // Find implicit renames, add them to m_implictRenames and m_implicitRenamesWithParentItems
            // If they are not also explict renames, remove them from m_unresolvedRenames
            for (int childIndex = m_unresolvedRenames.Count - 1; childIndex >= 0; childIndex--)
            {
                BatchedItem change = m_unresolvedRenames[childIndex];

                bool removeFromUnresolvedRenames = false;
                for (int parentIndex = 0; parentIndex < childIndex; parentIndex++)
                {
                    BatchedItem shorterRename = m_unresolvedRenames[parentIndex];
                    bool childIsAlsoExplicitRename; 
                    if (TfsUtil.isChildItemOfRename(change, shorterRename, out childIsAlsoExplicitRename))
                    {
                        m_implicitRenames.Add(change.Target);
                        m_implicitRenamesWithParentItems.Add(change.Target, shorterRename);
                        
                        // There may be both an implicit rename of the item based on a parent rename and an explicit rename of this item
                        removeFromUnresolvedRenames = !childIsAlsoExplicitRename;
                        break;
                    }
                }
                if (removeFromUnresolvedRenames)
                {
                    m_unresolvedRenames.RemoveAt(childIndex);
                }
            }

            m_unresolvedAdditiveActions.Sort(delegate(BatchedItem lhs, BatchedItem rhs)
            {
                return lhs.Target.Length.CompareTo(rhs.Target.Length);
            });

            for (int index = m_unresolvedAdditiveActions.Count - 1; index >= 0; index--)
            {
                foreach (BatchedItem rename in m_unresolvedRenames)
                {
                    if (VersionControlPath.IsSubItem(m_unresolvedAdditiveActions[index].Target, rename.Source))
                    {
                        if ((m_unresolvedAdditiveActions[index].Action == WellKnownChangeActionId.Add)
                            && (VersionControlPath.Equals(rename.Source, m_unresolvedAdditiveActions[index].Target))
                            && (VersionControlPath.IsSubItem(rename.Target, rename.Source))
                            && (!VersionControlPath.Equals(rename.Target, rename.Source)))
                        {
                            // This Add is created by a Rename to below itself. Skip it. Example:
                            // Rename $/foo/bar to $/foo/bar/bar2
                            // We will get two change actions, 1) Rename $/foo/bar to $/foo/bar/bar2
                            // 2) Add $/foo/bar.
                            // This is to skip the Add action as it will be created implicitly by the Rename.
                            if (!m_implicitAdds.Contains(m_unresolvedAdditiveActions[index].Target))
                            {
                                m_implicitAdds.Add(m_unresolvedAdditiveActions[index].Target);
                            }
                            m_unresolvedAdditiveActions.RemoveAt(index);
                            break;
                        }
                        // The additive item should be scheduled after the Rename.
                        // Unless the rename is a case only rename which does not change the namespace slot.
                        if (!VersionControlPath.Equals(rename.Source, rename.Target))
                        {
                            m_unresolvedAdditiveActions[index].ConflictItem = rename;
                        }
                        break;
                    }
                }
            }

            for (int outerIndex = 0; outerIndex < m_unresolvedRenames.Count; outerIndex++)
            {
                BatchedItem processedChange = m_unresolvedRenames[outerIndex];

                if (processedChange.ConflictItem != null)
                {
                    continue;
                }

                // Find a parent item that was renamed
                for (int innerIndex = 0; innerIndex < m_unresolvedRenames.Count; innerIndex++)
                {
                    if (outerIndex == innerIndex)
                    {
                        continue;
                    }
                    BatchedItem change = m_unresolvedRenames[innerIndex];
                    if (VersionControlPath.IsSubItem(processedChange.Target, change.Source))
                    {
                        processedChange.ConflictItem = change;
                        break;
                    }
                }

                // Check for a rename cycles and chains that starts with the processedChange
                // To do this: Follow the chain of renames until it ends, it cycles back to the processedChange, or
                // the number of times through the loop reaches the size of m_renamePairs
                // Note that the renameCycle list is only added to m_renameCycles (a List of Lists) if a cycle is 
                // found; otherwise it is was just a potential renameCycle that is tossed
                List<BatchedItem> renameCycle = new List<BatchedItem>();
                BatchedItem next;
                if (!m_renamePairs.TryGetValue(processedChange.Target, out next))
                {
                    Debug.Fail("Target of item in m_unresolvedRenames not found in m_renamePairs");
                }
                for(int i = 0; i < m_renamePairs.Count && next != null; i++)
                {
                    string adjustedSource = null;
                    try
                    {
                        adjustedSource = adjustSourceForParentRenameIfNeeded(next.Source, next.Target, true);
                    }
                    catch
                    {
                        // Can occur if adjusted name is longer than max; in this case treat like not found in m_renamePairs by leaving adjustedSource null
                    }

                    BatchedItem renameSourceItem;
                    // m_renamePairs has the rename target as the key, so the following is checking if the
                    // source of the "next" rename item is the target of another rename
                    if (string.IsNullOrEmpty(adjustedSource) || !m_renamePairs.TryGetValue(adjustedSource, out renameSourceItem))
                    {
                        renameCycle = null;
                        break;
                    }
                    // Setting the ConflictItem is needed whether this is a cycle or just a chain
                    renameSourceItem.ConflictItem = next;

                    // Add to the renameCycle list though this instance of the list will be discarded unless a cycle is found below
                    renameCycle.Add(next);

                    next = renameSourceItem;

                    if (next.ID == processedChange.ID)
                    {
                        m_renameCycles.Add(renameCycle);
                        break;
                    }
                }

                foreach (BatchedItem additiveAction in m_unresolvedAdditiveActions)
                {
                    if (additiveAction.Action == WellKnownChangeActionId.Undelete)
                    {
                        if (VersionControlPath.IsSubItem(processedChange.Target, additiveAction.Source))
                        {
                            Debug.Assert(processedChange.ConflictItem == null);
                            processedChange.ConflictItem = additiveAction;
                            continue;
                        }
                    }
                    else
                    {
                        // Add fld2, rename fld1/1.txt to fld2/1.txt
                        // We need to pend the Add first.
                        if (VersionControlPath.IsSubItem(processedChange.Target, additiveAction.Target))
                        {
                            if ((processedChange.ConflictItem == null) || VersionControlPath.IsSubItem(additiveAction.Target, processedChange.ConflictItem.Target))
                            {
                                processedChange.ConflictItem = additiveAction;
                            }
                            else
                            {
                                Debug.Assert(VersionControlPath.IsSubItem(processedChange.ConflictItem.Target, additiveAction.Target),
                                    string.Format("Item {0} conflicted with two items: {1} and {2}",
                                    processedChange.Target, processedChange.ConflictItem.Target, additiveAction.Target));
                            }
                            continue;
                        }
                        // Add fld1/file1.txt; rename fld -> fld1; rename go first
                        if (VersionControlPath.IsSubItem(additiveAction.Target, processedChange.Target))
                        {
                            if ((additiveAction.ConflictItem == null) || (VersionControlPath.IsSubItem(processedChange.Target, additiveAction.ConflictItem.Target)))
                            {
                                additiveAction.ConflictItem = processedChange;
                            }
                            else
                            {
                                Debug.Assert(VersionControlPath.IsSubItem(additiveAction.ConflictItem.Target, processedChange.Target),
                                    string.Format("Item {0} conflicted with two items: {1} and {2}",
                                    additiveAction.Target, additiveAction.ConflictItem.Target, processedChange.Target));
                            }
                            continue;
                        }
                    }
                }

            }
            m_unresolvedChanges.AddRange(m_unresolvedAdditiveActions);
            m_unresolvedChanges.AddRange(m_unresolvedRenames);
        }

        static bool isAdditiveAction(BatchedItem item)
        {
            if (   (item.Action == WellKnownChangeActionId.Add)
                || (item.Action == WellKnownChangeActionId.Branch)
                || (item.Action == WellKnownChangeActionId.Rename)
                || (item.Action == WellKnownChangeActionId.Undelete))
            {
                return true;
            }
            return false;
        }

        public ReadOnlyCollection<BatchedItem> Resolve()
        {
            Debug.Assert(acceptingNewChanges, "this should be true when resolve is called");

            lock (m_locker)
            {
                acceptingNewChanges = false;

                processRecursiveChanges();

                detectConflicts();

                // walk backwards so that RemoveAt can work 
                // without the removals affecting indexing
                for (int i = m_unresolvedChanges.Count - 1; i >= 0; i--)
                {
                    BatchedItem item = m_unresolvedChanges[i];
                    if (item.ConflictItem == null)
                    {
                        item.Resolved = true;
                        m_resolvedChanges.Add(item);
                        m_unresolvedChanges.RemoveAt(i);
                    }
                }

                if (m_unresolvedChanges.Count != 0)
                {
                    int currentCount = m_unresolvedChanges.Count;
                    int prevCount = -1;

                    // detect and remove priority issues
                    while (currentCount > 0 && prevCount != currentCount)
                    {
                        // resolve cases that can be solved just with priority adjustment
                        for (int i = m_unresolvedChanges.Count - 1; i >= 0; i--)
                        {
                            BatchedItem item = m_unresolvedChanges[i];

                            // if the conflict chain ends with the next item
                            // or the next item in the chain is already resolved
                            // then resolve by bumping our priority
                            if (item.ConflictItem.ConflictItem == null ||
                                item.ConflictItem.Resolved)
                            {
                                if ((item.Action == WellKnownChangeActionId.Add) && (item.ConflictItem.Action == WellKnownChangeActionId.Rename) &&
                                    VersionControlPath.IsSubItem(item.ConflictItem.Target, item.Target))
                                {
                                    // For the case Add Folder1 and rename Folder1 to Folder1\Folder1, change the action sequence to 
                                    // rename Folder1 to intermediate, Add Folder1, rename intermediate to Folder1\Folder1
                                    string intermediateNameAsSource;
                                    string intermediateNameAsTarget;
                                    getIntermediateNames(item.ConflictItem.Source, item.ConflictItem.Target, out intermediateNameAsTarget, out intermediateNameAsSource);
                                    BatchedItem intermediateToOriginal =
                                        new BatchedItem(
                                            intermediateNameAsSource,
                                            item.ConflictItem.Target,
                                            WellKnownChangeActionId.Rename,
                                            item.ConflictItem.Priority + 2);
                                    m_resolvedChanges.Add(intermediateToOriginal);

                                    item.ConflictItem.Target = intermediateNameAsTarget;
                                }
                                item.Resolved = true;
                                item.Priority = item.ConflictItem.Priority + 1;
                                m_resolvedChanges.Add(item);
                                m_unresolvedChanges.RemoveAt(i);
                            }
                        }

                        // Identify cycles
                        for (int i = 0; i < m_renameCycles.Count; i++)
                        {
                            List<BatchedItem> renameCycle = m_renameCycles[i];
                            if (renameCycle[0].Resolved)
                            {
                                continue;
                            }
                            // Set the priority on the later cycles so that they are pended after the earlier cycles
                            if (i > 0)
                            {
                                List<BatchedItem> previousCycle = m_renameCycles[i - 1];
                                int newCyclePriority = previousCycle[0].Priority + previousCycle.Count;
                                foreach(BatchedItem renameCycleItem in renameCycle)
                                {
                                    renameCycleItem.Priority = newCyclePriority;
                                }
                            }
                            breakCycle(renameCycle[0]);
                        }

                        prevCount = currentCount;
                        currentCount = m_unresolvedChanges.Count;
                    }
                }

                Debug.Assert(m_unresolvedChanges.Count == 0, "Unable to resolve pending changes");

                int highestPriority = 0;
                foreach (BatchedItem item in m_resolvedChanges)
                {
                    if (item.Priority > highestPriority)
                    {
                        highestPriority = item.Priority;
                    }
                }
                foreach (BatchedItem item in m_resolvedChanges)
                {
                    // Delay all Edits and Deletes to be processed at last.
                    if ((item.Action == WellKnownChangeActionId.Edit) || (item.Action == WellKnownChangeActionId.Delete))
                    {
                        item.Priority = item.Priority + highestPriority + 1;
                    }
                }

                m_resolvedChanges.Sort(
                    delegate(BatchedItem lhs, BatchedItem rhs)
                    {
                        return lhs.Priority.CompareTo(rhs.Priority);
                    }
                );

                return new ReadOnlyCollection<BatchedItem>(m_resolvedChanges);
            }
        }

        private void processRecursiveChanges()
        {
        }


        private int breakCycle(BatchedItem item)
        {
            /*
             *  rename A B
                rename B A
                
             *  becomes
             * 
                rename A <intermediate>
                rename B A
                rename <intermediate> B

                the conflict is from A to B
             */
            if (item.Action != WellKnownChangeActionId.Rename)
            {
                throw new UnresolvableConflictException("Don't know how to break non-rename cycles");
            }

            if (item.Resolved)
            {
                return item.Priority;
            }

            string intermediateNameAsTarget;
            string intermediateNameAsSource;
            getIntermediateNames(item.Source, item.Target, out intermediateNameAsTarget, out intermediateNameAsSource);

            BatchedItem intermediate1 =
                new BatchedItem(
                    adjustSourceForParentRenameIfNeeded(item.Source, item.Target, false),
                    intermediateNameAsTarget,
                    WellKnownChangeActionId.Rename,
                    item.ConflictItem.Priority);

            item.ConflictItem.Priority++;

            BatchedItem intermediate2 =
                new BatchedItem(
                    intermediateNameAsSource,
                    item.Target,
                    WellKnownChangeActionId.Rename,
                    item.ConflictItem.Priority + 1);

            m_resolvedChanges.Add(intermediate1);
            m_resolvedChanges.Add(intermediate2);

            /* Uncomment for debugging
            foreach (BatchedItem cycleItem in new BatchedItem[] { intermediate1, item.ConflictItem, intermediate2 })
            {
                TraceManager.TraceInformation("ChangeOptimizer.breakCycle() result: Source: {0}, Target: {1}, Priority: {2}",
                    cycleItem.Source, cycleItem.Target, cycleItem.Priority);
            }
            */

            // remove the old change
            item.Resolved = true;
            m_unresolvedChanges.Remove(item);

            if (item.ConflictItem.ConflictItem != null &&
                item.ConflictItem.ConflictItem.ID == item.ID)
            {
                item.ConflictItem.Resolved = true;
                m_unresolvedChanges.Remove(item.ConflictItem);
                m_resolvedChanges.Add(item.ConflictItem);
            }
            else
            {
                intermediate2.Priority = breakCycle(item.ConflictItem);
            }

            return item.ConflictItem.Priority;
        }

        /// <summary>
        /// Get the intermediate server path names used when generating a rename to a temporary file/folder to re-generate the actions needed to create
        /// a cyclic rename. The tricky part is that the renamed item may also have been renamed causing an implicit rename as well as the explicit renames
        /// involved in the cyclic rename.  For this reason, this method may return different paths for the out arguments
        /// intermediateNameAsTarget and intermediateNameAsSource.
        /// </summary>
        /// <param name="sourceName">The source path for a rename operation for which we are generating an intermediate path</param>
        /// <param name="targetName">The target path for a rename operation for which we are generating an intermediate path</param>
        /// <param name="intermediateNameAsTarget">The intermediate path to use when the intermediate item is the target of a rename</param>
        /// <param name="intermediateNameAsSource">The intermediate path to use when the intermediate item is the source of a rename</param>
        private void getIntermediateNames(string sourceName, string targetName, out string intermediateNameAsTarget, out string intermediateNameAsSource)
        {
            intermediateNameAsTarget = targetName + Guid.NewGuid().ToString();

            intermediateNameAsSource = adjustSourceForParentRenameIfNeeded(intermediateNameAsTarget, targetName, false);
        }

        private string adjustSourceForParentRenameIfNeeded(string sourceName, string targetName, bool adjustToTarget)
        {
            string adjustedSource;
            BatchedItem parentRenameItem;
            if (!m_implicitRenamesWithParentItems.TryGetValue(targetName, out parentRenameItem))
            {
                adjustedSource = sourceName;
            }
            else
            {
                string parentPathToUse = adjustToTarget ? parentRenameItem.Target : parentRenameItem.Source;
                adjustedSource = VersionControlPath.Combine(parentPathToUse, VersionControlPath.GetFileName(sourceName));
            }
            return adjustedSource;
        }
    }
}
