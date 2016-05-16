// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// A batched item (used in a BatchingContext) representing a merge operation.
    /// </summary>
    public class CCBatchedItem
    {
        /// <summary>
        /// Creates a batch item for the specific change action.
        /// </summary>
        /// <param name="target">The target path of the operation</param>
        /// <param name="action">The change action for the batched item</param>
        public CCBatchedItem(
            string source, 
            string target, 
            Guid action, 
            IMigrationItem downloadItem, 
            string itemTypeReferenceName, 
            long internalActionId, 
            XmlDocument migrationActionDescription)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }
            m_source = source;
            m_target = target;
            m_action = action;
            m_downloadItem = downloadItem;
            ItemTypeReferenceName = itemTypeReferenceName;
            m_internalActionId = internalActionId;
            m_migrationActionDescription = migrationActionDescription;
        }

        /// <summary>
        /// The original migration action id of the batched item. 
        /// </summary>
        public long InternalActionId
        {
            get
            {
                return m_internalActionId;
            }
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
    

        internal CCBatchedItem ConflictItem
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
        internal string MergeVersionTo
        {
            get
            {
                return m_mergeVersionTo;
            }
        }

        internal IMigrationItem DownloadItem
        {
            get
            {
                return m_downloadItem;
            }
        }

        internal XmlDocument MigrationActionDescription
        {
            get
            {
                return m_migrationActionDescription;
            }
        }

        internal string ItemTypeReferenceName
        {
            get;
            set;
        }

        string m_source;
        string m_target;
        bool m_skip;
        Guid m_action;
        CCBatchedItem m_conflictItem;
        Guid m_id = Guid.NewGuid();
        bool m_resolved;
        int m_priority; 
        /// This private memer is used only for undelete, branch or merge items.
        /// For undelete, this is the version of the undeleted item was deleted
        /// For branch, this is the branch from version of the item. 
        /// For merge, this is the starting version of the merge item.
        string m_version;
        string m_mergeVersionTo;
        long m_internalActionId;
        IMigrationItem m_downloadItem;
        XmlDocument m_migrationActionDescription;

    }

}