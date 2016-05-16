// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    /// <summary>
    /// A batched item (used in a BatchingContext) representing a merge operation.
    /// </summary>
    public class BatchedItem
    {
        /// <summary>
        /// Creates a batch item for the specific change action.
        /// </summary>
        /// <param name="target">The target path of the operation</param>
        /// <param name="action">The change action for the batched item</param>
        public BatchedItem(string target, Guid action)
            : this(null, target, action, null)
        {
        }

        /// <summary>
        /// Creates a batch item for the specific change action.
        /// </summary>
        /// <param name="target">The target path of the operation</param>
        /// <param name="action">The change action for the batched item</param>
        public BatchedItem(string source, string target, Guid action, IMigrationItem downloadItem)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }
            m_source = source;
            m_target = target;
            m_action = action;
            m_downloadItem = downloadItem;
            m_priority = 1;
        }


        /// <summary>
        /// Creates an edit item that need to be downloaded before pend - in Branch|Merge|Edit case, after Branch|Merge is pended, we need to do a Get before pendedit
        /// </summary>
        /// <param name="version">1 to do a get from server before pend edit</param>
        /// <param name="target">The target path of the operation</param>
        /// <param name="action">The change action for the batched item</param>
        /// <param name="source">The target path of the operation</param>
        /// <param name="downloadItem"></param>
        public BatchedItem(string source, string target, Guid action, string version, IMigrationItem downloadItem)
            : this(source, target, action, downloadItem)
        {
            m_version = version;
        }

        /// <summary>
        /// Creates a batch item for the specific change action.
        /// </summary>
        /// <param name="source">The source path of the operation</param>
        /// <param name="target">The target path of the operation</param>
        /// <param name="action">The change action for the batched item</param>
        public BatchedItem(string source, string target, Guid action)
            : this(source, target, action, 1)
        {
        }

        /// <summary>
        /// Creates a batch item for the specific change action.
        /// </summary>
        /// <param name="source">The source path of the operation</param>
        /// <param name="target">The target path of the operation</param>
        /// <param name="action">The change action for the batched item</param>
        /// <param name="priority">The default priority of the batched item</param>
        public BatchedItem(
            string source,
            string target,
            Guid action,
            int priority)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            m_source = source;
            m_target = target;
            m_action = action;
            m_priority = priority;
        }

        /// <summary>
        /// Creates a Merge batch item with the specified options.
        /// </summary>
        /// <param name="source">The source path of the merge operation</param>
        /// <param name="target">The target path of the merge operation</param>
        /// <param name="recursion">The recursion type of the merge operation</param>
        /// <param name="mergeVersionFrom">The starting version of the merge item.</param>
        /// <param name="mergeVersionTo">The end version of the merge item</param>
        public BatchedItem(string source, string target, RecursionType recursion, string mergeVersionFrom, string mergeVersionTo, 
            IMigrationItem downloadItem)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            m_action = WellKnownChangeActionId.Merge;
            m_source = source;
            m_target = target;
            m_version = mergeVersionFrom;
            m_mergeVersionTo = mergeVersionTo;
            m_recursionType = recursion;
            m_downloadItem = downloadItem;
            // Merge operation has the highest priority
            m_priority = 0;
        }

        /// <summary>
        /// Creates a branch batch item with the specified options.
        /// </summary>
        /// <param name="source">The source path of the merge operation</param>
        /// <param name="target">The target path of the merge operation</param>
        /// <param name="version">The version of the item branched from</param>
        /// <param name="recursion">The recursivity of the merge operation</param>
        public BatchedItem(string source, string target, RecursionType recursion, string version)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            m_action = WellKnownChangeActionId.Branch;
            m_source = source;
            m_target = target;
            m_recursionType = recursion;
            m_version = version;
        }

        /// <summary>
        /// Create a undelete batch item with the specified options.
        /// </summary>
        /// <param name="source">The source path of the undelete operation</param>
        /// <param name="target">The target path of the undelete operation</param>
        /// <param name="deletedChangeset">The changeset number of which the item was deleted.</param>
        public BatchedItem(string source, string target, string deletedVersion, IMigrationItem downloadItem)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("source");
            }

            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("target");
            }

            m_action = WellKnownChangeActionId.Undelete;
            m_source = source;
            m_target = target;
            m_version = deletedVersion;
            m_downloadItem = downloadItem;
        }

        /// <summary>
        /// The source item of the merge operation
        /// </summary>
        public string Source
        {
            get
            {
                return m_source;
            }
            set
            {
                m_source = value;
            }
        }

        /// <summary>
        /// Adjust the source path of a Rename item due to parent rename. 
        /// </summary>
        /// <param name="newSource">The new source path.</param>
        internal void AdjustSourceForParentRename(string newSource)
        {
            m_source = newSource;
        }

        /// <summary>
        /// The target item of the merge operation
        /// </summary>
        public string Target
        {
            get
            {
                return m_target;
            }
            set
            {
                m_target = value;
            }
        }

        /// <summary>
        /// The merge recursivity for the batched operation
        /// </summary>
        public RecursionType Recursion
        {
            get
            {
                return m_recursionType;
            }
        }

        /// <summary>
        /// If true, the item can be skipped during later processing.  If false the item is to be processed.
        /// </summary>
        public bool Skip
        {
            get
            {
                return m_skip;
            }

            set
            {
                m_skip = value;
            }
        }

        public Guid Action
        {
            get
            {
                return m_action;
            }
        }
    

        internal BatchedItem ConflictItem
        {
            get
            {
                return m_conflictItem;
            }
            set
            {
                m_conflictItem = value;
            }
        }

        internal bool Resolved
        {
            get
            {
                return m_resolved;
            }
            set
            {
                m_resolved = value;
            }
        }

        internal int Priority
        {
            get
            {
                return m_priority;
            }
            set
            {
                m_priority = value;
            }
        }

        internal Guid ID
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
        /// This Property is used only for undelete, branch or merge items.
        /// For undelete, this is the version of the undeleted item was deleted
        /// For branch, this is the branch from version of the item. 
        /// For merge, this is the starting version of the merge item.
        /// </summary>
        public string Version
        {
            get
            {
                return m_version;
            }
        }

        /// <summary>
        /// This property is used only for merge items. 
        /// For merge, thie is the end version of the merge item.
        /// </summary>
        public string MergeVersionTo
        {
            get
            {
                return m_mergeVersionTo;
            }
        }

        public MergeOptions MergeOption
        {
            get
            {
                return m_mergeOption;
            }
            set
            {
                m_mergeOption = value;
            }
        }

        public IMigrationItem DownloadItem
        {
            get
            {
                return m_downloadItem;
            }
        }

        string m_source;
        string m_target;
        RecursionType m_recursionType;
        bool m_skip;
        Guid m_action;
        BatchedItem m_conflictItem;
        Guid m_id = Guid.NewGuid();
        bool m_resolved;
        int m_priority; 
        /// This private memer is used only for undelete, branch or merge items.
        /// For undelete, this is the version of the undeleted item was deleted
        /// For branch, this is the branch from version of the item. 
        /// For merge, this is the starting version of the merge item.
        string m_version;
        string m_mergeVersionTo;
        MergeOptions m_mergeOption = MergeOptions.AlwaysAcceptMine | MergeOptions.ForceMerge;
        IMigrationItem m_downloadItem;
    }

}
