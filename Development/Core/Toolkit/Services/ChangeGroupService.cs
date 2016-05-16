// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class ChangeGroupEventArgs : EventArgs
    {
        private Guid m_sourceId;
        private ChangeGroup m_changeGroup;

        /// <summary>
        /// Get the MigrationSourceId of the ChangeGroup assoicated with the event
        /// </summary>
        /// <value></value>
        public Guid SourceId
        {
            get { return m_sourceId; }
        }

        /// <summary>
        /// Get the ChangeGroup assoicated with the event
        /// </summary>
        /// <value></value>
        public ChangeGroup ChangeGroup
        {
            get { return m_changeGroup; }
        }

        internal ChangeGroupEventArgs(Guid sourceId, ChangeGroup changeGroup)
        {
            m_sourceId = sourceId;
            m_changeGroup = changeGroup;
        }
    }

    /// <summary>
    /// The change group service.
    /// </summary>
    public class ChangeGroupService : IServiceProvider
    {
        ChangeGroupManager m_changeGroupManager;
        ChangeGroup m_currentChangeGroup;
        HashSet<string> m_existingActionPaths = new HashSet<string>();
        DateTime m_latestActionTime;
        static readonly TimeSpan MaxGroupTimeSpan = new TimeSpan(0, 10, 0); // Max time span is 10 minutes.

        internal event EventHandler<ChangeGroupEventArgs> PreChangeGroupSaved;
        internal event EventHandler<ChangeGroupEventArgs> PostChangeGroupSaved;

        /// <summary>
        /// Creates a change group service.
        /// </summary>
        /// <param name="changeGroupManager"></param>
        internal ChangeGroupService(ChangeGroupManager changeGroupManager)
        {
            if (changeGroupManager == null)
            {
                throw new ArgumentNullException("changeGroupManager");
            }
            m_changeGroupManager = changeGroupManager;
        }

        public ChangeGroupManager ChangeGroupManager
        {
            get { return m_changeGroupManager; }
        }

        /// <summary>
        /// Provide the method to return the current service object
        /// </summary>
        /// <param name="serviceType">Type of the service being requested</param>
        /// <returns>Returns this service object if the requested type is ChangeGroupService; otherwise, null is returned.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(ChangeGroupService)))
            {
                return this;
            }
            return null;
        }

        /// <summary>
        /// Creates a new change group with the specified name.
        /// </summary>
        /// <param name="groupName">The name of the new change group.</param>
        /// <returns>The newly created change group</returns>
        public ChangeGroup CreateChangeGroupForDeltaTable(string groupName)
        {
            return m_changeGroupManager.CreateForDeltaTable(groupName);
        }

        /// <summary>
        /// Add a migration action to delta table. The migration actions will be grouped together to create a change group. 
        /// The following rules are used to group migration actions.
        /// 1. Action path and fromPath does not collide with any existing paths.
        /// 2. Action comment is the same.
        /// 3. Action owner is the same.
        /// 4. Action time is not more than 10 minutes away from the previous action.
        /// The return value is the delta table entry the migration action is added to. 
        /// If a new changegroup is generated, oldChangeGroup contains the previous changegroup.
        /// </summary>
        /// <param name="defaultGroupName"></param>
        /// <param name="comment"></param>
        /// <param name="owner"></param>
        /// <param name="executionOrder"></param>
        /// <param name="action"></param>
        /// <param name="sourceItem"></param>
        /// <param name="fromPath"></param>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <param name="mergeVersionTo"></param>
        /// <param name="itemTypeRefName"></param>
        /// <param name="actionDetails"></param>
        /// <param name="actionTime"></param>
        /// <param name="oldChangeGroup"></param>
        /// <returns></returns>
        public ChangeGroup AddMigrationActionToDeltaTable(string defaultGroupName, string comment, string owner, long executionOrder, 
            Guid action, IMigrationItem sourceItem, string fromPath, string path, string version, string mergeVersionTo, string itemTypeRefName, 
            XmlDocument actionDetails, DateTime actionTime, out ChangeGroup oldChangeGroup)
        {
            if (m_currentChangeGroup == null)
            {
                oldChangeGroup = null;
                m_existingActionPaths.Clear();
                m_currentChangeGroup = CreateChangeGroupForDeltaTable(defaultGroupName);
                m_currentChangeGroup.Comment = comment;
                m_currentChangeGroup.Owner = owner;
                m_currentChangeGroup.ExecutionOrder = executionOrder;
            }
            else if (m_existingActionPaths.Contains(path)
                || (!string.IsNullOrEmpty(fromPath) && m_existingActionPaths.Contains(fromPath))
                || !string.Equals(comment, m_currentChangeGroup.Comment, StringComparison.Ordinal)
                || !string.Equals(owner, m_currentChangeGroup.Owner, StringComparison.Ordinal)
                || (m_latestActionTime != DateTime.MinValue && actionTime != DateTime.MinValue && (actionTime - m_latestActionTime > MaxGroupTimeSpan)))
            {
                m_currentChangeGroup.Save();
                oldChangeGroup = m_currentChangeGroup;
                m_currentChangeGroup = CreateChangeGroupForDeltaTable(defaultGroupName);
                m_currentChangeGroup.Comment = comment;
                m_currentChangeGroup.Owner = owner;
                m_currentChangeGroup.ExecutionOrder = executionOrder;
                m_existingActionPaths.Clear();
            }
            else
            {
                oldChangeGroup = m_currentChangeGroup;
            }
            m_currentChangeGroup.CreateAction(action, sourceItem, fromPath, path, version, mergeVersionTo, itemTypeRefName, actionDetails);
            m_existingActionPaths.Add(path);
            if (!string.IsNullOrEmpty(fromPath))
            {
                m_existingActionPaths.Add(fromPath);
            }
            m_latestActionTime = actionTime;

            return m_currentChangeGroup;
        }

        /// <summary>
        /// Remove the partially created change groups.
        /// </summary>
        public void RemoveIncompleteChangeGroups()
        {
            m_changeGroupManager.RemoveIncompleteChangeGroups();
        }

        /// <summary>
        /// Identifies and promotes any change groups in the current session whose status is Analysis
        /// to the Pending state.
        /// This method is called during the analysis phase when the items in the analysis 
        /// state are ready for migration.  The promotion occurs in the transaction context provided.
        /// </summary>
        /// <returns>The number of change groups reverted from "Analysis" to "Pending"</returns>
        public int PromoteAnalysisToPending()
        {
            return m_changeGroupManager.PromoteAnalysisToPending();
        }
        /// <summary>
        /// Identifies and promotes any change groups in the current session whose status is Delta
        /// to the DeltaPending state.
        /// This method is called during the analysis phase when the items in the analysis 
        /// state are ready for migration.  The promotion occurs in the transaction context provided.
        /// </summary>
        public void PromoteDeltaToPending()
        {
            m_changeGroupManager.PromoteDeltaToPending();
        }

        /// <summary>
        /// Identifies and demotes any change groups in the current session whose status is InProgress
        /// to the Pending state.
        /// This method is called as a part of crash recovery to ensure that any pending operations are reverted
        /// so they can be retried.  By default this uses a transactional context returned 
        /// by DataAccessManager.Current.StartTransaction().
        /// </summary>
        /// <returns>The number of change groups reverted from "InProgress" to "Pending"</returns>
        public int DemoteInProgressActionsToPending()
        {
            return m_changeGroupManager.DemoteInProgressActionsToPending();
        }

        /// <summary>
        /// Registers the specified IMigrationItemSerializer as the default source serializer with the ChangeGroupManager.
        /// </summary>
        /// <param name="serializer">The serializer to register</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.IMigrationItemSerializer"/>
        public void RegisterDefaultSourceSerializer(IMigrationItemSerializer serializer)
        {
            m_changeGroupManager.RegisterDefaultSerializer(serializer);
        }

        /// <summary>
        /// Registers a source serializer for a ContentType with the ChangeGroupManager.
        /// </summary>
        /// <param name="type">The ContentType id</param>
        /// <param name="serializer">The IMigrationItemSerializer to register for the specified ContentType</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.IMigrationItemSerializer"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.Services.ContentType"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.Services.WellKnownContentType"/>
        public void RegisterSourceSerializer(Guid type, IMigrationItemSerializer serializer)
        {
            m_changeGroupManager.RegisterMigrationItemSerializer(type, serializer);
        }

        public ReadOnlyCollection<KeyValuePair<MigrationAction, MigrationAction>> DetectContentConflict()
        {
            return m_changeGroupManager.DetectContentConflict();
        }

        public int NumOfDeltaTableEntries()
        {
            return m_changeGroupManager.NumOfDeltaTableEntries();
        }

        /// <summary>
        /// Retrieves the next delta table entry for processing.
        /// </summary>
        /// <param name="pageNumber">The page number to to retrieve</param>
        /// <param name="pageSize">The size of the page to retrieve</param>
        /// <returns>A page of ChangeGroup objects</returns>
        /// <exception cref="System.ArgumentException">System.ArgumentException is thrown if the page number or page size are less than zero.</exception>
        public IEnumerable<ChangeGroup> NextDeltaTablePage(int pageNumber, int pageSize, bool includeConflicts)
        {
            if (pageNumber < 0)
            {
                throw new ArgumentException("pageNumber");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("pageSize");
            }

            return m_changeGroupManager.NextDeltaTablePage(pageNumber, pageSize, includeConflicts);
        }

        /// <summary>
        /// Retrieves the next migration instruction table page for processing.
        /// </summary>
        /// <param name="pageNumber">The page number to to retrieve</param>
        /// <param name="pageSize">The size of the page to retrieve</param>
        /// <returns>A page of ChangeGroup objects</returns>
        /// <exception cref="System.ArgumentException">System.ArgumentException is thrown if the page number or page size are less than zero.</exception>
        public IEnumerable<ChangeGroup> NextMigrationInstructionTablePage(
            int pageNumber, 
            int pageSize,
            bool isInConflictDetectionState,
            bool includeGroupInBacklog)
        {
            if (pageNumber < 0)
            {
                throw new ArgumentException("pageNumber");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("pageSize");
            }

            return m_changeGroupManager.NextMigrationInstructionTablePage(pageNumber, pageSize, isInConflictDetectionState, includeGroupInBacklog);
        }

        /// <summary>
        /// Get the in progress change group count from this side. 
        /// In progress change group from this side includes the delta table entries from this source and migration instructions from the other side. (translated)
        /// </summary>
        /// <returns></returns>
        public int GetInProgressMigrationInstructionCount()
        {
            return m_changeGroupManager.GetInProgressMigrationInstructionCount();
        }

        public void RemoveInProgressChangeGroups()
        {
            m_changeGroupManager.RemoveInProgressChangeGroups();
        }


        /// <summary>
        /// Marks the entire batch of entries in the ChangeGroupManager context as ChangeStatus DeltaCompleted.
        /// </summary>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.ChangeGroup"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.ChangeStatus"/>
        public void BatchMarkDeltaTableEntriesAsDeltaCompleted()
        {
            m_changeGroupManager.BatchMarkDeltaTableEntriesAsDeltaCompleted();
        }

        public void BatchMarkMigrationInstructionsAsPending()
        {
            m_changeGroupManager.BatchMarkMigrationInstructionsAsPending();
        }

        /// <summary>
        /// TODO - Not Implemented
        /// </summary>
        /// <param name="id">TODO - Not Implemented</param>
        /// <param name="utcWhen">TODO - Not Implemented</param>
        public void UpdateConversionHistory(string id, DateTime utcWhen)
        {
            // Todo 
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Looks up the target item in the conversion history for a specified source item.
        /// </summary>
        /// <param name="id">The source item id to attempt to find in the conversion history</param>
        /// <param name="peerSourceId">The MigrationSource Id</param>
        /// <param name="contentChanged">Output flag indicating whether the context was changed</param>
        /// <returns>The target item id from the conversion history</returns>
        public string GetChangeIdFromConversionHistory(string id, Guid peerSourceId, out bool contentChanged)
        {
            return m_changeGroupManager.GetChangeIdFromConversionHistory(id, peerSourceId, out contentChanged);
        }

        /// <summary>
        /// Loads a IMigrationAction based on the specified id.
        /// </summary>
        /// <param name="actionInternalId">The </param>
        /// <returns>
        /// The IMigrationAction associated with the specified id.  Returns null if the action
        /// associated with the id cannot be found.
        /// </returns>
        public IMigrationAction LoadSingleAction(long actionInternalId)
        {
            return m_changeGroupManager.LoadSingleAction(actionInternalId);
        }

        /// <summary>
        /// Creates and initializes a new change group with the specified name in migration instruction table.
        /// </summary>
        /// <param name="deltaTableChangeGroup">The name of the ChangeGroup to create</param>
        /// <returns>The new ChangeGroup</returns>
        public ChangeGroup CreateChangeGroupForMigrationInstructionTable(ChangeGroup deltaTableChangeGroup)
        {
            return m_changeGroupManager.CreateForMigrationInstructionTable(deltaTableChangeGroup);
        }

        /// <summary>
        /// Gets the internal Id of the first conflicted change group in the given status
        /// </summary>
        /// <param name="changeStatus"></param>
        /// <returns></returns>
        internal long? GetFirstConflictedChangeGroup(ChangeStatus changeStatus)
        {
            return m_changeGroupManager.GetFirstConflictedChangeGroup(changeStatus);
        }

        /// <summary>
        /// Initialize the change group manager with the current session run information.
        /// </summary>
        /// <param name="sessionRunStorageId"></param>
        internal void Initialize(int sessionRunStorageId)
        {
            m_changeGroupManager.Initialize(sessionRunStorageId);
        }

        /// <summary>
        /// Discard a migration instruction change group and reactivate the corresponding delta table entry
        /// </summary>
        /// <param name="migrationInstructionEntry"></param>
        /// <returns>The corresponding delta table entry</returns>
        internal ChangeGroup ReactivateDeltaEntry(ChangeGroup migrationInstructionEntry)
        {
            return m_changeGroupManager.DiscardMigrationInstructionAndReactivateDelta(migrationInstructionEntry);
        }

        internal void ReactivateMigrationInstruction(ChangeGroup migrationInstruction, ChangeGroup deltaEntryToObsolete)
        {
            m_changeGroupManager.ReactivateMigrationInstruction(migrationInstruction, deltaEntryToObsolete);
        }

        internal void FirePreChangeGroupSaved(ChangeGroup changeGroup)
        {
            if (PreChangeGroupSaved != null)
            {
                ChangeGroupEventArgs eventArgs = new ChangeGroupEventArgs(m_changeGroupManager.SourceId, changeGroup);
                PreChangeGroupSaved(this, eventArgs);
            }
        }

        internal void FirePostChangeGroupSaved(ChangeGroup changeGroup)
        {
            if (PostChangeGroupSaved != null)
            {
                ChangeGroupEventArgs eventArgs = new ChangeGroupEventArgs(m_changeGroupManager.SourceId, changeGroup);
                PostChangeGroupSaved(this, eventArgs);
            }
        }
    }

}
