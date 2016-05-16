// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    internal class CCChangeOptimizer
    {
        List<CCBatchedItem> m_resolvedChanges = new List<CCBatchedItem>();
        List<CCBatchedItem> m_unresolvedChanges = new List<CCBatchedItem>();
        List<CCBatchedItem> m_unresolvedEdits = new List<CCBatchedItem>();
        Dictionary<string, CCBatchedItem> m_unresolvedAdds = new Dictionary<string, CCBatchedItem>();
        Dictionary<string, CCBatchedItem> m_unresolvedRenames = new Dictionary<string, CCBatchedItem>();
        Dictionary<string, CCBatchedItem> m_unresolvedDeletes = new Dictionary<string, CCBatchedItem>();

        internal void Add(CCBatchedItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (item.Action == WellKnownChangeActionId.Delete)
            {
                addDelete(item);
            }
            else if (item.Action == WellKnownChangeActionId.Rename)
            {
                addRename(item);
            }
            else if (item.Action == WellKnownChangeActionId.Add)
            {
                m_unresolvedAdds.Add(item.Target,item);
            }
            else if (item.Action == WellKnownChangeActionId.Edit)
            {
                m_unresolvedEdits.Add(item);
            }
        }

        private void addRename(CCBatchedItem item)
        {
            string removeItem = null;
            foreach (KeyValuePair<string, CCBatchedItem> renameItemInlist in m_unresolvedRenames)
            {
                if (ClearCasePath.Equals(ClearCasePath.MakeRelative(item.Target, renameItemInlist.Value.Target), 
                    ClearCasePath.MakeRelative(item.Source, renameItemInlist.Value.Source)))
                {
                    // Parent rename is already added, just return.
                    return;
                }
                else if (ClearCasePath.Equals(ClearCasePath.MakeRelative(renameItemInlist.Value.Target, item.Target),
                    ClearCasePath.MakeRelative(renameItemInlist.Value.Source, item.Source)))
                {
                    // A child rename is already, remove the child rename.
                    removeItem = renameItemInlist.Key;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(removeItem))
            {
                m_unresolvedRenames.Remove(removeItem);
            }

            m_unresolvedRenames.Add(item.Target, item);
        }

        private void addDelete(CCBatchedItem item)
        {
            string removeItem = null;
            foreach (KeyValuePair<string, CCBatchedItem> deleteItemInlist in m_unresolvedDeletes)
            {
                if (ClearCasePath.IsSubItem(item.Target, deleteItemInlist.Value.Target))
                {
                    // Parent delete is already added, just return.
                    return;
                }
                else if (ClearCasePath.IsSubItem(deleteItemInlist.Value.Target, item.Target))
                {
                    // A child delete is already added, remove the child delete.
                    removeItem = deleteItemInlist.Key;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(removeItem))
            {
                m_unresolvedDeletes.Remove(removeItem);
            }

            m_unresolvedDeletes.Add(item.Target, item);
        }

        private void detectConflicts()
        {
            // Add and Rename list
            // First, remove all rename for rename|undelete that was changed to rename|add.
            List<string> renameToBeRemoved = new List<string>();
            foreach (string renameToName in m_unresolvedRenames.Keys)
            {
                if (m_unresolvedAdds.ContainsKey(renameToName))
                {
                    renameToBeRemoved.Add(renameToName);
                }
            }

            foreach (string renameToName in renameToBeRemoved)
            {
                m_unresolvedRenames.Remove(renameToName);
            }

            foreach (CCBatchedItem addItem in m_unresolvedAdds.Values)
            {
                foreach (CCBatchedItem renameItem in m_unresolvedRenames.Values)
                {
                    if (ClearCasePath.IsSubItem(addItem.Target, renameItem.Target))
                    {
                        // Add folder2/1.txt, Rename folder1 to folder2. 
                        scheduleItem(addItem, renameItem);
                    }
                    else if (ClearCasePath.IsSubItem(renameItem.Target, addItem.Target))
                    {
                        // Rename folder1/1.txt to folder1/2.txt, Add folder1
                        scheduleItem(renameItem, addItem);
                    }
                    else if (ClearCasePath.IsSubItem(addItem.Target, renameItem.Source))
                    {
                        // Rename folder1 to folder2, Add folder1/1.txt
                        scheduleItem(addItem, renameItem);
                    }
                    else if (ClearCasePath.Equals(renameItem.Source, addItem.Target) && ClearCasePath.IsSubItem(renameItem.Target, addItem.Target))
                    {
                        // ToDo Rename to below itself.
                        throw new NotImplementedException("Don't know how to handle rename to below itself problem.");

                    }
                    else
                    {
                        Debug.Assert(!ClearCasePath.IsSubItem(renameItem.Source, addItem.Target), "Rename source cannot be the sub item of an Add");
                    }
                }
                m_unresolvedChanges.Add(addItem);
            }

            // Rename and itself
            foreach (CCBatchedItem renameItemOutLoop in m_unresolvedRenames.Values)
            {
                foreach (CCBatchedItem renameItemInnerLoop in m_unresolvedRenames.Values)
                {
                    if (ClearCasePath.Equals(renameItemInnerLoop.Target, renameItemOutLoop.Target))
                    {
                        continue;
                    }
                    if (ClearCasePath.IsSubItem(renameItemInnerLoop.Target, renameItemOutLoop.Source))
                    {
                        scheduleItem(renameItemInnerLoop, renameItemOutLoop);
                    }
                }
                m_unresolvedChanges.Add(renameItemOutLoop);
            }            
        }

        public ReadOnlyCollection<CCBatchedItem> Resolve()
        {
            detectConflicts();

            // walk backwards so that RemoveAt can work 
            // without the removals affecting indexing
            for (int i = m_unresolvedChanges.Count - 1; i >= 0; i--)
            {
                CCBatchedItem item = m_unresolvedChanges[i];
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
                        CCBatchedItem item = m_unresolvedChanges[i];

                        // if the conflict chain ends with the next item
                        // or the next item in the chain is already resolved
                        // then resolve by bumping our priority
                        if (item.ConflictItem.ConflictItem == null ||
                            item.ConflictItem.Resolved)
                        {
                            item.Resolved = true;
                            item.Priority = item.ConflictItem.Priority + 1;
                            m_resolvedChanges.Add(item);
                            m_unresolvedChanges.RemoveAt(i);
                        }
                    }

                    // identify cycles
                    for (int i = 0; i < m_unresolvedChanges.Count; i++)
                    {
                        CCBatchedItem item = m_unresolvedChanges[i];
                        CCBatchedItem next = item.ConflictItem;
                        while (next != null)
                        {
                            if (next.ConflictItem != null &&
                                next.ConflictItem.ID == item.ID)
                            {
                                //ToDo breakCycle(item);
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
                foreach (CCBatchedItem item in m_resolvedChanges)
                {
                    if (item.Priority > highestPriority)
                    {
                        highestPriority = item.Priority;
                    }
                }

                // Delay all Edits and Deletes to be processed at last.
                foreach (CCBatchedItem editItem in m_unresolvedEdits)
                {
                    editItem.Priority = editItem.Priority + highestPriority + 1;
                    m_resolvedChanges.Add(editItem);
                }
                foreach (CCBatchedItem deleteItem in m_unresolvedDeletes.Values)
                {
                    deleteItem.Priority = deleteItem.Priority + highestPriority + 1;
                    m_resolvedChanges.Add(deleteItem);
                }

                m_resolvedChanges.Sort(
                    delegate(CCBatchedItem lhs, CCBatchedItem rhs)
                    {
                        return lhs.Priority.CompareTo(rhs.Priority);
                    }    
                );

                return new ReadOnlyCollection<CCBatchedItem>(m_resolvedChanges);
        }

        private int breakCycle(CCBatchedItem item)
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
            CCBatchedItem intermediate1 =
                new CCBatchedItem(
                    item.Source,
                    intermediate,
                    WellKnownChangeActionId.Rename,
                    item.DownloadItem,
                    item.ItemTypeReferenceName,
                    item.InternalActionId,
                    null);
            intermediate1.Priority = item.Priority;

            item.ConflictItem.Priority++;

            CCBatchedItem intermediate2 =
                new CCBatchedItem(
                    intermediate,
                    item.Target,
                    WellKnownChangeActionId.Rename, 
                    item.DownloadItem, 
                    item.ItemTypeReferenceName, 
                    item.InternalActionId,
                    null);
            intermediate2.Priority = item.ConflictItem.Priority + 1;

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

        private void scheduleItem(CCBatchedItem lowPriorityItem, CCBatchedItem highPriorityItem)
        {
            if (lowPriorityItem.ConflictItem == null)
            {
                lowPriorityItem.ConflictItem = highPriorityItem;
            }
            else
            {
                Debug.Assert(lowPriorityItem.ConflictItem == highPriorityItem, "Item conflicted with two different items.");
            }
        }

        public void Clear()
        {
            m_resolvedChanges.Clear();
            m_unresolvedChanges.Clear();
            m_unresolvedRenames.Clear();
            m_unresolvedDeletes.Clear();
            m_unresolvedEdits.Clear();
            m_unresolvedAdds.Clear();
        }
    }
}
