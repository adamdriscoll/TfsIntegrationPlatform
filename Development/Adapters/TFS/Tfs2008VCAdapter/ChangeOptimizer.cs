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

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    public class ChangeOptimizer
    {
        List<BatchedItem> m_resolvedChanges = new List<BatchedItem>();
        List<BatchedItem> m_unresolvedChanges = new List<BatchedItem>();
        List<BatchedItem> m_unresolvedRenames = new List<BatchedItem>();
        List<BatchedItem> m_unresolvedAdditiveActions = new List<BatchedItem>();
        HashSet<string> m_implicitRenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> m_implicitAdds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> m_renamePairs = new Dictionary<string, string>();

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

        public void Clear()
        {
            lock (m_locker)
            {
                m_resolvedChanges.Clear();
                m_unresolvedChanges.Clear();
                m_unresolvedRenames.Clear();
                m_unresolvedAdditiveActions.Clear();
                m_renamePairs.Clear();
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
                        change.Source = m_renamePairs[change.Target];
                    }
                }

                else if ((change.Action == WellKnownChangeActionId.Merge))
                {
                    if (m_renamePairs.ContainsKey(change.Target))
                    {
                        change.Target = m_renamePairs[change.Target];
                    }
                }
            }

            m_renamePairs.Clear();
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

                if (change.Action == WellKnownChangeActionId.Rename)
                {
                    bool addToRenameList = true;
                    foreach (BatchedItem existingRenameChange in m_unresolvedRenames)
                    {
                        if (TfsUtil.isChildItemOf(change, existingRenameChange))
                        {
                            addToRenameList = false;
                            break;
                        }
                    }
                    if (addToRenameList)
                    {
                        m_unresolvedRenames.Add(change);
                    }
                    else
                    {
                        m_implicitRenames.Add(change.Target);
                    }

                    if (!m_renamePairs.ContainsKey(change.Target))
                    {
                        m_renamePairs.Add(change.Target, change.Source);
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
                    // Deletes, Edits or Encodings
                    m_unresolvedChanges.Add(change);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void detectConflicts()
        {
            m_unresolvedRenames.Sort(delegate(BatchedItem lhs, BatchedItem rhs)
                    {
                        return lhs.Source.Length.CompareTo(rhs.Source.Length);
                    });
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
            if (   (item.Action ==  WellKnownChangeActionId.Add)
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
                                    string intermediate = getIntermediateName(item.ConflictItem.Source);
                                    BatchedItem intermediateToOriginal =
                                        new BatchedItem(
                                            intermediate,
                                            item.ConflictItem.Target,
                                            WellKnownChangeActionId.Rename,
                                            item.ConflictItem.Priority + 2);
                                    m_resolvedChanges.Add(intermediateToOriginal);

                                    item.ConflictItem.Target = intermediate;
                                }
                                item.Resolved = true;
                                item.Priority = item.ConflictItem.Priority + 1;
                                m_resolvedChanges.Add(item);
                                m_unresolvedChanges.RemoveAt(i);
                            }
                        }

                        // identify cycles
                        for (int i = 0; i < m_unresolvedChanges.Count; i++)
                        {
                            BatchedItem item = m_unresolvedChanges[i];
                            BatchedItem next = item.ConflictItem;
                            while (next != null)
                            {
                                if (next.ConflictItem != null &&
                                    next.ConflictItem.ID == item.ID)
                                {
                                    breakCycle(item);
                                    break;
                                }

                                next = next.ConflictItem;
                            }
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

            string intermediate = getIntermediateName(item.Target);
            BatchedItem intermediate1 =
                new BatchedItem(
                    item.Source,
                    intermediate,
                    WellKnownChangeActionId.Rename,
                    item.ConflictItem.Priority);

            item.ConflictItem.Priority++;

            BatchedItem intermediate2 =
                new BatchedItem(
                    intermediate,
                    item.Target,
                    WellKnownChangeActionId.Rename,
                    item.ConflictItem.Priority + 1);

            m_resolvedChanges.Add(intermediate1);
            m_resolvedChanges.Add(intermediate2);

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

        private static string getIntermediateName(string targetName)
        {
            return targetName + Guid.NewGuid().ToString();
        }
    }
}
