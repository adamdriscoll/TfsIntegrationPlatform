// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The state of the current action.  It is possible that a single change group could be loaded with
    /// migration actions already in the complete or skipped state.  Only pending actions should be executed
    /// during the migration process.
    /// </summary>
    public enum ActionState
    {
        Pending = 0,
        Complete = 1,
        Skipped = 2
    }

    /// <summary>
    /// The base type for all migration actions in a ChangeGroup.
    /// </summary>
    public abstract class MigrationAction : IMigrationAction
    {
        protected MigrationAction(ChangeGroup parent, Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails)
            : this(parent, DefaultActionId, action, sourceItem, fromPath, path, version, mergeVersionTo, itemTypeRefName, actionDetails)
        {
        }

        protected MigrationAction(ChangeGroup parent, long actionId, Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails)
        {
            Initialize(parent, actionId, action, sourceItem, fromPath, path, version, mergeVersionTo, itemTypeRefName, actionDetails);
        }

        private void Initialize(ChangeGroup parent, long actionId, Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails)
        {
            m_parent = parent;
            m_actionId = actionId;
            m_action = action;
            m_sourceItem = sourceItem;
            m_fromPath = fromPath;
            m_path = path;
            m_version = version;
            m_mergeVersionTo = mergeVersionTo;
            m_itemTypeRefName = itemTypeRefName;
            m_actionDetails = actionDetails;

            if (null != m_actionDetails)
            {
                m_actionDetails.NodeChanged += new XmlNodeChangedEventHandler(m_actionDetails_NodeChanged);
                m_actionDetails.NodeInserted += new XmlNodeChangedEventHandler(m_actionDetails_NodeInserted);
                m_actionDetails.NodeRemoved += new XmlNodeChangedEventHandler(m_actionDetails_NodeRemoved);
            }

            IsDirty = false;
        }

        void m_actionDetails_NodeRemoved(object sender, XmlNodeChangedEventArgs e)
        {
            IsDirty = true;
        }

        void m_actionDetails_NodeInserted(object sender, XmlNodeChangedEventArgs e)
        {
            IsDirty = true;
        }

        void m_actionDetails_NodeChanged(object sender, XmlNodeChangedEventArgs e)
        {
            IsDirty = true;
        }

        internal virtual bool IsDirty
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new migration action instance using a default transaction.
        /// </summary>
        internal abstract void CreateNew(IMigrationItemSerializer serializer);


        // <summary>
        // Creates a new migration action instannce in the context of the provided transaction.
        // </summary>
        // <param name="trx">The transaction in which the migration action is created.</param>
        //internal abstract void CreateNew(IMigrationTransaction trx);

        /// <summary>
        /// Updates an existing migration action using a default transaction.
        /// </summary>
        internal abstract void UpdateExisting();

        /// <summary>
        /// Updates an exsiting migration action using the provided transactions.
        /// </summary>
        /// <param name="trx">The transaction in which the migration action is updated</param>
        internal abstract void UpdateExisting(IMigrationTransaction trx);

        #region IMigrationAction<int> Members

        /// <summary>
        /// The ChangeGroup the migation action is associated with.
        /// </summary>
        public ChangeGroup ChangeGroup
        {
            get
            {
                return m_parent;
            }
            set
            {
                m_parent = value;
                IsDirty = true;
            }
        }

        /// <summary>
        /// The source item, of the migration action. 
        /// </summary>
        public IMigrationItem SourceItem
        {
            get
            {
                return m_sourceItem;
            }
        }

        /// <summary>
        /// The from path of the migration action. Rename, Branch and Merge may have a FromPath
        /// </summary>
        public string FromPath
        {
            get
            {
                return m_fromPath;
            }
            set
            {
                m_fromPath = value;
                IsDirty = true;
            }
        }

        /// <summary>
        /// The path of the migration action. All migration action have a path.
        /// </summary>
        public string Path
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = value;
                IsDirty = true;
            }
        }

        /// <summary>
        /// The change action this migration item contains.
        /// </summary>
        public Guid Action
        {
            get
            {
                return m_action;
            }
            set
            {
                m_action = value;
            }
        }


        /// <summary>
        /// If true the action is performed recursively, otherwise not.
        /// </summary>
        public bool Recursive
        {
            get
            {
                return m_recursive;
            }
            set
            {
                m_recursive = value;
                IsDirty = true;
            }
        }

        /// <summary>
        /// The order in which this action should be executed relative to other actions in the change group.
        /// </summary>
        public int Order
        {
            get
            {
                return m_order;
            }
            set
            {
                m_order = value;
                IsDirty = true;
            }
        }

        /// <summary>
        /// The current state of the action.
        /// </summary>
        public ActionState State
        {
            get
            {
                return m_state;
            }
            set
            {
                m_state = value;
                IsDirty = true;
                UpdateExisting();
            }
        }

        /// <summary>
        /// If the action is a Label operation this is the name of the label.
        /// </summary>
        public string Label
        {
            get
            {
                return m_label;
            }
        }

        /// <summary>
        /// If the action is an encoding operation this is the new encoding type.
        /// </summary>
        public string Encoding
        {
            get
            {
                return m_encoding;
            }
        }

        /// <summary>
        /// Type of the action item Any = 0, Folder = 1, File = 2,
        /// </summary>
        public string ItemTypeReferenceName
        {
            get
            {
                return m_itemTypeRefName;
            }
        }

        /// <summary>
        /// An value that uniquely identifies the action in the persistence store.  This value is used by UpdateExisting
        /// to identify the action in the data store.
        /// </summary>
        public long ActionId
        {
            get
            {
                return m_actionId;
            }
            internal set
            {
                m_actionId = value;
            }
        }

        /// <summary>
        /// This Property is only used for undelete, branch and merge action. 
        /// For undelete action, this is the version in which the item was deleted.
        /// For branch action, this is the branch from version
        /// For merge action, this is the start version of merge
        /// </summary>
        public string Version
        {
            get
            {
                return m_version;
            }
        }

        /// <summary>
        /// This Property is only used for merge action. 
        /// For merge action, this is the end version of merge
        /// </summary>
        public string MergeVersionTo
        {
            get
            {
                return m_mergeVersionTo;
            }
        }

        public XmlDocument MigrationActionDescription
        {
            get
            {
                return m_actionDetails;
            }
        }

        #endregion

        /// <summary>
        /// Updates the state without an immediate database upate.
        /// </summary>
        /// <param name="state">The new action state.</param>
        internal void setStateWithoutUpdatingDb(ActionState state)
        {
            m_state = state;
        }

        ChangeGroup m_parent;
        bool m_recursive;
        Guid m_action;
        IMigrationItem m_sourceItem;
        string m_fromPath;
        string m_path;
        string m_version;
        string m_mergeVersionTo;
        int m_order = -1;
        ActionState m_state = ActionState.Pending;
        long m_actionId = -1;

        string m_label;
        string m_encoding;

        string m_itemTypeRefName;

        XmlDocument m_actionDetails;

        // the default action ID will be set to something else when it is persisted into the data store.
        // For example in SQL this will be the row id of the action.
        const long DefaultActionId = -1;
    }
}
