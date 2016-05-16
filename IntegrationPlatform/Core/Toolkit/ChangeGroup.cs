// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Types that implement this interface are able to serialize IMigrationItems to and 
    /// from a string representation (must be valid XML).
    /// </summary>
    public interface IMigrationItemSerializer
    {
        IMigrationItem LoadItem(string itemBlob, ChangeGroupManager manager);
        string SerializeItem(IMigrationItem item);
    }

    // these values are persisted in SQL so do not change them between releases
    /// <summary>
    /// Change Group Status
    /// </summary>
    public enum ChangeStatus
    {
        Unintialized = -1,                  // 
        Delta = 0,                          // Initial state of delta table entry
        DeltaPending = 1,                   // Group contains delta table entry: NextDeltaTable works on this status
        DeltaComplete = 2,                  // Group, as delta table entry, is "complete", i.e the entry is no longer needed
        DeltaSynced = 8,                    // Goup, as delta table entry, has been "synced" to the other side. i.e. the entry is no longer needed for content conflict detection.
        AnalysisMigrationInstruction = 3,   // The initial status of the migration instruction table entry
        Pending    = 4,                     // Start processing migration instruction: NextMigrationInstruction works on this status
        InProgress = 5,                     // 
        Complete   = 6,                     //
        Skipped    = 7,                     //
        ChangeCreationInProgress = 20,      // change group creation is in progress during paged change actions insertion 
        PendingConflictDetection    = 9,    //
        Obsolete = 10                       // 
    }

    /// <summary>
    /// The base class for all change groups.  The derived class will be responsible for
    /// all persistence operations associated with the change group.
    /// </summary>
    public abstract class ChangeGroup
    {
        /// <summary>
        /// Creates a change group using the associated group manager.
        /// </summary>
        /// <param name="manager">The change group manager of this change group</param>
        protected ChangeGroup(ChangeGroupManager manager)
        {
            m_manager = manager;

            if (m_manager != null)
            {
                m_sourceId = m_manager.SourceId;
                if(m_manager.Session != null)
                {
                    m_sessionId = new Guid(m_manager.Session.SessionUniqueId);
                }
                
            }
        }

        /// <summary>
        /// Persists the change group to the data store.
        /// </summary>
        protected abstract void Create();

        /// <summary>
        /// Updates the existing change group in the data store.
        /// </summary>
        protected abstract void Update();

        /// <summary>
        /// Updates the status of an existing change group in the data store.
        /// </summary>
        /// <param name="newChangeStatus"></param>
        internal abstract void UpdateStatus(ChangeStatus newChangeStatus);

        /// <summary>
        /// Updates the child change action of this group in the data store.
        /// </summary>
        /// <param name="childAction"></param>
        protected abstract void UpdateChildAction(MigrationAction childAction);

        /// <summary>
        /// Creates and returns an object that implements the IMigrationAction interface.
        /// </summary>
        /// <returns>The newly created IMigrationAction instance</returns>
        public abstract IMigrationAction CreateAction(Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails);

        /// <summary>
        /// Creates and returns an object that implements the IMigrationAction interface.
        /// The action will be marked as skipped if skipped is true. 
        /// It is useful to created a skipped action for content conflict detection purpose. 
        /// </summary>
        /// <returns>The newly created IMigrationAction instance</returns>
        public abstract IMigrationAction CreateAction(Guid action, IMigrationItem sourceItem,
            string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, XmlDocument actionDetails, bool skipped);

        public abstract void UpdateConversionHistory(ConversionResult result);

        /// <summary>
        /// Mark the current change group as complete. This will also mark all change actions in this changegroup as complete.
        /// </summary>
        public abstract void Complete();
        
        /// <summary>
        /// The name of the change group.  This value is persisted in the version control ConversionHistory table
        /// as the source or target of the conversion operation.  For systems that support numbered changesets,
        /// such as TFS, this should be the changeset ID of the change being migrated.  Other systems will need to
        /// choose an appropriate value.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return m_groupName;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_Name);
                m_groupName = value;
            }
        }

        /// <summary>
        /// A unique change group ID set to an appropriate value after being persisted to the data store.
        /// </summary>
        public virtual long ChangeGroupId
        {
            get
            {
                return m_changeGroupId;
            }
            protected set
            {
                m_changeGroupId = value;
            }
        }

        /// <summary>
        /// A unique change group ID of the interal ID of the corresponding change group from 
        /// the other side of the migration pipeline.
        /// </summary>
        public virtual long? ReflectedChangeGroupId
        {
            get
            {
                return m_reflectedChangeGroupId;
            }
            internal set
            {
                m_reflectedChangeGroupId = value;
            }
        }

        /// <summary>
        /// The order in which the change group should be executed (relative to other change groups).
        /// Lower numbers are executed before higher values.
        /// </summary>
        public virtual long ExecutionOrder
        {
            get
            {
                return m_executionOrder;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_Order);
                m_executionOrder = value;
            }
        }

        /// <summary>
        /// The source system Id of this change group
        /// </summary>
        public Guid SourceId
        {
            get
            {
                return m_sourceId;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_SourceId);
                m_sourceId = value;
            }
        }

        /// <summary>
        /// The change group manager associated with this change group.
        /// </summary>
        public ChangeGroupManager Manager
        {
            get
            {
                return m_manager;
            }
        }

        /// <summary>
        /// The change group manager that has the IMigrationItem serializer to (de)serialize the 
        /// MigrationItems in this group.
        /// </summary>
        internal ChangeGroupManager ManagerWithMigrationItemSerializers
        {
            get
            {
                if (m_userOtherSideMigrationItemSerializers)
                {
                    return m_manager.OtherSideChangeGroupManager;
                }
                else
                {
                    return m_manager;
                }
            }
        }

        /// <summary>
        /// Gets/sets whether this change group should use the other-side MigrationItem serializers
        /// to persist the child migration actions.
        /// </summary>
        public bool UseOtherSideMigrationItemSerializers
        {
            get
            {
                return m_userOtherSideMigrationItemSerializers;
            }
            set
            {
                m_userOtherSideMigrationItemSerializers = value;
            }
        }

        /// <summary>
        /// The change owner (or author)
        /// </summary>
        public virtual string Owner
        {
            get
            {
                if (!m_ownerSet && m_owner == null)
                {
                    setDefaultOwner();
                }

                return m_owner;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_Owner);                    
                m_owner = value;
                m_ownerSet = true;
            }
        }

        /// <summary>
        /// The change comment
        /// </summary>
        public virtual string Comment
        {
            get
            {
                return m_comment;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_Comment);
                m_comment = (value == null) ? string.Empty : value;
            }
        }

        public virtual DateTime? RevisionTime
        {
            get
            {
                return m_revisionTime;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_RevisionTime);
                m_revisionTime = value;
            }
        }

        /// <summary>
        /// The ID of the migration session in which the change is being migrated.
        /// </summary>
        public virtual Guid SessionId
        {
            get
            {
                return m_sessionId;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_SessionId);
                m_sessionId = (value == null) ? Guid.Empty : value;
            }
        }

        /// <summary>
        /// The UTC time that the change was submitted to the source system
        /// </summary>
        public virtual DateTime ChangeTimeUtc
        {
            get
            {
                return m_changeTime;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_Time);
                m_changeTime = value;
            }
        }

        public virtual bool ContainsBackloggedAction
        {
            get
            {
                return m_ContainsBackloggedAction;
            }
            set
            {
                DemandUnlocked(MigrationToolkitResources.GroupingError_ContainsBackloggedAction);
                m_ContainsBackloggedAction = value;
            }
        }

        public virtual bool IsForcedSync
        {
            get
            {
                return m_isForcedSync;
            }
            set
            {
                m_isForcedSync = value;
            }
        }

        /// <summary>
        /// The current status of the change group.
        /// </summary>
        public virtual ChangeStatus Status
        {
            get
            {
                return m_status;
            }
            set
            {
                // status can be changed when locked.
                switch (value)
                {
                    case ChangeStatus.Delta:
                    case ChangeStatus.DeltaPending:
                    case ChangeStatus.DeltaComplete:
                    case ChangeStatus.AnalysisMigrationInstruction:
                    case ChangeStatus.Pending:
                    case ChangeStatus.InProgress:
                    case ChangeStatus.Complete:
                    case ChangeStatus.Skipped:
                    case ChangeStatus.PendingConflictDetection:
                    case ChangeStatus.ChangeCreationInProgress:
                    case ChangeStatus.Obsolete:
                        m_status = value;
                        break;
                    default:
                        throw new MigrationException(
                            string.Format(MigrationToolkitResources.Culture, 
                            MigrationToolkitResources.UnknownChangeStatusValue, 
                            value));
                }
            }
        }

        /// <summary>
        /// The actions that are a part of this change group.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual IList<IMigrationAction> Actions
        {
            get
            {
                return m_actions;
            }
            protected set
            {
                m_actions.Clear();
                if (null != value)
                {
                    foreach (var action in value)
                    {
                        m_actions.Add(action);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new action to the change group.
        /// </summary>
        /// <param name="action">The action to add.</param>
        internal virtual void AddAction(IMigrationAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            DemandUnlocked(MigrationToolkitResources.GroupingError_Add);

            if (action.Order < 0)
            {
                action.Order = Actions.Count;
            }

            Actions.Add(action);
        }

        /// <summary>
        /// Saves the change group to the backing store.  By default this uses a transactional
        /// context returned by DataAccessManager.Current.StartTransaction().
        /// </summary>
        public virtual void Save()
        {
            Save(true);
        }

        private void Save(bool fireEvents)
        {
            Debug.Assert(!this.SessionId.Equals(Guid.Empty));
            Debug.Assert(this.SourceId != Guid.Empty); 
            Debug.Assert(this.Manager != null);
            Debug.Assert(this.Manager.ChangeGroupService != null);

            if (fireEvents)
            {
                this.Manager.ChangeGroupService.FirePreChangeGroupSaved(this);
            }

            if (m_changeGroupId == long.MinValue)
            {
                Create();
            }
            else
            {
                Update();
            }

            if (fireEvents)
            {
                this.Manager.ChangeGroupService.FirePostChangeGroupSaved(this);
            }
        }

        /// <summary>
        /// Saves the child change action of this group
        /// </summary>
        /// <param name="action"></param>
        public virtual void Save(MigrationAction action)
        {
            Debug.Assert(action.ChangeGroup == this, 
                         "Saving change action failed. The change action does not belong to this change group.");
            Debug.Assert(m_changeGroupId != long.MinValue,
                         "Saving change action failed. The change group has not been persisted to DB yet.");
            Debug.Assert(action.ActionId > 0,
                         "Saving change action failed. The change action has not been persisted to DB yet.");

            UpdateChildAction(action);
        }

        /// <summary>
        /// If the change group is currently locked this method should throw an exception.  Returning
        /// indicates that the change group is unlocked.
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void DemandUnlocked(string msg)
        {
            if (m_locked)
            {
                throw new MigrationException(msg);
            }
        }

        /// <summary>
        /// If true the change group is locked, unlocked otherwise.
        /// </summary>
        protected virtual bool Locked
        {
            get
            {
                return m_locked;
            }
            set
            {
                m_locked = value;
            }
        }

        private void setDefaultOwner()
        {
            Debug.Assert(!m_ownerSet);

            /* ToDo
             * if (!m_manager.Session.TryGetValue<string>(
                "DefaultChangeOwner", out m_owner))
            {
                m_owner = null;
            }
             * */

            m_ownerSet = true;
        }

        long m_changeGroupId = long.MinValue;
        long? m_reflectedChangeGroupId = null;
        long m_executionOrder;
        string m_owner;
        string m_comment;
        DateTime? m_revisionTime;
        DateTime m_changeTime;
        ChangeStatus m_status = ChangeStatus.Unintialized;
        Guid m_sessionId = Guid.Empty;
        string m_groupName = string.Empty;
        bool m_ContainsBackloggedAction = false;
        bool m_isForcedSync;

        Guid m_sourceId;

        bool m_locked;

        bool m_ownerSet;

        Collection<IMigrationAction> m_actions = new NotifyingCollection<IMigrationAction>();

        ChangeGroupManager m_manager;
        bool m_userOtherSideMigrationItemSerializers = false;
    }
}
