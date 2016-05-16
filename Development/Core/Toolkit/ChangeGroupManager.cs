// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The change group manager contains the version control session, migration direction
    /// and the source and target item serializer instances.
    /// </summary>
    public abstract class ChangeGroupManager
    {
        /// <summary>
        /// Creates a change group manager using the provided parameters.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sourceId"></param>
        protected ChangeGroupManager(
            Session session,
            Guid sourceId)
        {
            m_session = session;
            m_sourceId = sourceId;
            m_targetActionIds = new Dictionary<Guid, string>();
            m_serializer = new Dictionary<Guid, IMigrationItemSerializer>();
            m_defaultSerializer = null;
        }

        public IMigrationItemSerializer this[Guid changeActionId]
        {
            get
            {
                if (m_serializer.ContainsKey(changeActionId))
                {
                    return m_serializer[changeActionId];
                }
                else
                {
                    return m_defaultSerializer;
                }
            }
        }

        /// <summary>
        /// The highwater mark of the last successfully migrated change operation
        /// </summary>
        public virtual IHighWaterMark LastHighWaterMark
        {
            get
            {
                return m_lastHwm;
            }
            set
            {
                m_lastHwm = value;
            }
        }

        public void RegisterDefaultSerializer(IMigrationItemSerializer defaultSerializer)
        {
            m_defaultSerializer = defaultSerializer;
        }

        public void RegisterMigrationItemSerializer(Guid changeActionType, IMigrationItemSerializer serializer)
        {
            if (this.m_serializer.ContainsKey(changeActionType))
            {
                m_serializer.Remove(changeActionType);
            }

            m_serializer.Add(changeActionType, serializer);
        }

        /// <summary>
        /// The session that is executing.
        /// </summary>
        public Session Session
        {
            get
            {
                return m_session;
            }
        }

        /// <summary>
        /// The source system Id
        /// </summary>
        internal Guid SourceId
        {
            get
            {
                return m_sourceId;
            }
        }

        internal ChangeGroupService ChangeGroupService
        {
            get;
            set;
        }

        internal ChangeGroupManager OtherSideChangeGroupManager
        {
            get;
            set;
        }

        public Dictionary<Guid, string> TargetActionids
        {
            get
            {
                return m_targetActionIds;
            }
            set
            {
                m_targetActionIds = value;
            }
        }

        /// <summary>
        /// Identifies and returns the next ChangeGroup instance for migration.  It is possible for this
        /// method to return the same group multiple times if the group's status has not changed.  Only
        /// change groups for the proper session are returned.  By default this uses a transactional
        /// context returned by DataAccessManager.Current.StartTransaction().
        /// </summary>
        /// <returns>The next available ChangeGroup</returns>
        public abstract ChangeGroup Next();

        /// <summary>
        /// Identifies and demotes any change groups in the current session whose status is InProgress
        /// to the Pending state.
        /// This method is called as a part of crash recovery to ensure that any pending operations are reverted
        /// so they can be retried.  By default this uses a transactional context returned 
        /// by DataAccessManager.Current.StartTransaction().
        /// </summary>
        /// <returns>The number of change groups reverted from "InProgress" to "Pending"</returns>
        public abstract int DemoteInProgressActionsToPending();

        internal virtual void Initialize(int sessionRunStorageId)
        {
        }

        /// <summary>
        /// Identifies and promotes any change groups in the current session whose status is Analysis
        /// to the Pending state.
        /// This method is called during the analysis phase when the items in the analysis 
        /// state are ready for migration.  The promotion occurs in the transaction context provided.
        /// </summary>
        /// <returns>The number of change groups reverted from "Analysis" to "Pending"</returns>
        public abstract int PromoteAnalysisToPending();

        public abstract void PromoteDeltaToPending();

        public abstract int GetInProgressMigrationInstructionCount();

        public abstract void RemoveInProgressChangeGroups();

        public abstract IEnumerable<ChangeGroup> NextDeltaTablePage(int pageNumber, int pageSize, bool includeConflicts);

        public abstract IEnumerable<ChangeGroup> NextMigrationInstructionTablePage(int pageNumber, int pageSize, bool isInConflictDetectionState, bool includeGroupInBacklog);

        public abstract string GetChangeIdFromConversionHistory(string id, Guid peerSourceId, out bool contentChanged);

        public abstract void BatchMarkDeltaTableEntriesAsDeltaCompleted();

        public abstract void BatchMarkMigrationInstructionsAsPending();

        public abstract IMigrationAction LoadSingleAction(long actionInternalId);

        public abstract void RemoveIncompleteChangeGroups();

        public abstract ReadOnlyCollection<KeyValuePair<MigrationAction, MigrationAction>> DetectContentConflict();

        internal abstract long? GetFirstConflictedChangeGroup(ChangeStatus status);

        /// <summary>
        /// Identifies and demotes any change groups in the current session whose status is InProgress
        /// to the Pending state.
        /// This method is called as a part of crash recovery to ensure that any pending operations are reverted
        /// so they can be retried.  By default this uses a transactional context returned 
        /// by DataAccessManager.Current.StartTransaction().
        /// </summary>
        /// <param name="trx">The transaction context to perform the promotion.</param>
        /// <returns>The number of change groups reverted from "InProgress" to "Pending"</returns>
        internal abstract int DemoteInProgressActionsToPending(IMigrationTransaction trx);


        /// <summary>
        /// Identifies and promotes any change groups in the current session whose status is Analysis
        /// to the Pending state.
        /// This method is called during the analysis phase when the items in the analysis 
        /// state are ready for migration.  The promotion occurs in the transaction context provided.
        /// </summary>
        /// <param name="trx">The transaction context to perform the promotion.</param>
        /// <returns>The number of change groups reverted from "Analysis" to "Pending"</returns>
        internal abstract int PromoteAnalysisToPending(IMigrationTransaction trx);

        /// <summary>
        /// Creates a new change group with the specified name in delta table
        /// </summary>
        /// <param name="groupName">The name of the new change group.</param>
        /// <returns>The newly created change group</returns>
        public abstract ChangeGroup CreateForDeltaTable(string groupName);

        /// <summary>
        /// Creates a new change group with the specified name.
        /// </summary>
        /// <param name="groupName">The name of the new change group.</param>
        /// <returns>The newly created change group</returns>
        public abstract ChangeGroup Create(string groupName);
        
        /// <summary>
        /// Creates a new change group with the specified name in migration instruction table
        /// </summary>
        /// <param name="deltaTableGroup"></param>
        /// <returns></returns>
        public abstract ChangeGroup CreateForMigrationInstructionTable(ChangeGroup deltaTableGroup);


        /// <summary>
        /// Persist the current status into underlying store.
        /// </summary>
        /// <param name="changeGroups"></param>
        public abstract void BatchUpdateStatus(ChangeGroup[] changeGroups);


        /// <summary>
        /// Gets the number of entries in the DeltaTable
        /// </summary>
        /// <returns></returns>
        public abstract int NumOfDeltaTableEntries();

        /// <summary>
        /// Discard a migration instruction change group and reactivate the corresponding delta table entry
        /// </summary>
        /// <param name="migrationInstructionEntry"></param>
        /// <returns>The corresponding delta table entry</returns>
        internal abstract ChangeGroup DiscardMigrationInstructionAndReactivateDelta(ChangeGroup migrationInstructionEntry);

        /// <summary>
        /// Discard a delta table entry and reactivate the corresponding migration instruction change group
        /// </summary>
        /// <param name="migrationInstruction"></param>
        /// <param name="deltaEntryToObsolete"></param>
        internal abstract void ReactivateMigrationInstruction(ChangeGroup migrationInstruction, ChangeGroup deltaEntryToObsolete);

        IMigrationItemSerializer m_defaultSerializer;
        Dictionary<Guid, IMigrationItemSerializer> m_serializer;
        Session m_session;
        Guid m_sourceId;
        IHighWaterMark m_lastHwm;
        Dictionary<Guid, string> m_targetActionIds;
    }

}
