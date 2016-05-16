// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class SqlChangeGroup : ChangeGroup
    {
        const int UsePagedCollectionSizeThreshold = 1000;
        int m_sqlChangeActionPageSize = 100000;
        int m_sqlChangeActionTimeToLive = 1 * 1000; // milliseconds that pages of ChangeActions live in memory
        bool m_usePagedActions = false; 

        public SqlChangeGroup(ChangeGroupManager manager)
            : base(manager)
        {
            ListenToDefaultActionCollectionChange();
        }

        /// <summary>
        /// constructor. Saves partial change group to db.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="groupName"></param>
        /// <param name="changeStatus"></param>
        public SqlChangeGroup(
            ChangeGroupManager manager,
            string groupName,
            ChangeStatus changeStatus)
            : base(manager)
        {
            Name = groupName;
            Status = changeStatus;
            
            ListenToDefaultActionCollectionChange();
        }

        /// <summary>
        /// constructor. Saves partial change group to db.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="copyFromGroup"></param>
        public SqlChangeGroup(
            ChangeGroupManager manager,
            SqlChangeGroup copyFromGroup)
            : base(manager)
        {
            Name = copyFromGroup.Name;
            ExecutionOrder = copyFromGroup.ExecutionOrder;
            Owner = copyFromGroup.Owner;
            ChangeTimeUtc = copyFromGroup.ChangeTimeUtc;
            Comment = copyFromGroup.Comment;
            RevisionTime = copyFromGroup.RevisionTime;
            ExecutionOrder = copyFromGroup.ExecutionOrder;
            Status = copyFromGroup.Status;
            IsForcedSync = copyFromGroup.IsForcedSync;

            ListenToDefaultActionCollectionChange();
        }


        public override IList<IMigrationAction> Actions
        {
            get
            {
                if (m_usePagedActions)
                {
                    if (m_actions == null)
                    {

                        // The provider assumes that the SqlChangeGroup is persisted to the data store
                        SqlChangeGroupProvider provider = new SqlChangeGroupProvider(m_runTimeChangeGroup, (SqlChangeGroupManager)Manager);
                        m_actions = new PagedCollection<IMigrationAction>(provider, m_sqlChangeActionPageSize, m_sqlChangeActionTimeToLive);
                    }
                    return m_actions;
                }
                else
                {
                    return base.Actions;
                }
            }
            protected set
            {
                // TODO
            }
        }

        /// <summary>
        ///  Mark the current change group as complete. 
        ///  As the SqlChangeAction does not persist the MigrationAction status, we don't update the Actions' status
        /// </summary>
        public override void Complete()
        {
            // uncomment the following code when SqlMigrationAction supports persisting action status
            //foreach (MigrationAction action in this.Actions)
            //{
            //    action.State = ActionState.Complete;
            //}

            this.UpdateStatus(ChangeStatus.Complete);
        }

        /// <summary>
        /// Create an action in current change group
        /// </summary>
        /// <param name="action"></param>
        /// <param name="sourceItem"></param>
        /// <param name="version"></param>
        /// <param name="mergeVersionTo"></param>
        /// <returns></returns>
        public override IMigrationAction CreateAction(
            Guid action,
            IMigrationItem sourceItem,
            string fromPath,
            string path,
            string version,
            string mergeVersionTo,
            string itemTypeRefName,
            XmlDocument actionDetails)
        {
            return this.CreateAction(
                action,
                sourceItem,
                fromPath,
                path,
                version,
                mergeVersionTo,
                itemTypeRefName,
                actionDetails,
                0);
        }

        public override IMigrationAction CreateAction(
            Guid action,
            IMigrationItem sourceItem,
            string fromPath,
            string path,
            string version,
            string mergeVersionTo,
            string itemTypeRefName,
            XmlDocument actionDetails,
            bool skipped)
        {
            return this.CreateAction(
                action,
                sourceItem,
                fromPath,
                path,
                version,
                mergeVersionTo,
                itemTypeRefName,
                actionDetails,
                0,
                skipped);
        }

        private IMigrationAction CreateAction(
            Guid action,
            IMigrationItem sourceItem,
            string fromPath,
            string path,
            string version,
            string mergeVersionTo,
            string itemTypeRefName,
            XmlDocument actionDetails,
            long internalChangeActionId)
        {
            return CreateAction(action, sourceItem, fromPath, path, version, 
                                mergeVersionTo, itemTypeRefName, actionDetails, 
                                internalChangeActionId, false);
        }

        private IMigrationAction CreateAction(
            Guid action,
            IMigrationItem sourceItem,
            string fromPath,
            string path,
            string version,
            string mergeVersionTo,
            string itemTypeRefName,
            XmlDocument actionDetails,
            long internalChangeActionId,
            bool isSubstituted)
        {
            if (!Manager.TargetActionids.ContainsKey(action))
            {
                // ToDo - Action not supported conflict
            }
            DemandUnlocked("Cannot Create an action on a group after the initial Create");

            SqlMigrationAction createdAction = new SqlMigrationAction(
                this, internalChangeActionId, action, sourceItem, 
                fromPath, path, version, mergeVersionTo, itemTypeRefName, actionDetails,
                isSubstituted ? ActionState.Skipped : ActionState.Pending);

            this.AddAction(createdAction);
            return createdAction;
        }

        public void RealizeFromEDM(
            RTChangeGroup runTimeChangeGroup)
        {
            RealizeChangeGroupProperties(runTimeChangeGroup);

            if (!m_usePagedActions)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                
                    // load actions proactively only when the use-paged-action threshold is not reached
                    foreach (RTChangeAction runTimeChangeAction in context.LoadChangeAction(this.ChangeGroupId))
                    {
                        RealizeSingleActionFromEDM(runTimeChangeAction);
                    }
                }
            }
        }

        private IMigrationAction RealizeSingleActionFromEDM(
            RTChangeAction runTimeChangeAction)
        {
            XmlDocument changeActionData = null;
            if (!string.IsNullOrEmpty(runTimeChangeAction.ActionData))
            {
                changeActionData = new XmlDocument();
                changeActionData.LoadXml(runTimeChangeAction.ActionData);
            }

            IMigrationItemSerializer itemSerializer = ManagerWithMigrationItemSerializers[runTimeChangeAction.ActionId];

            DemandUnlocked("Cannot Create an action on a group after the initial Create");

            SqlMigrationAction migrationAction = new SqlMigrationAction(
                this, runTimeChangeAction.ChangeActionId, runTimeChangeAction.ActionId, 
                itemSerializer.LoadItem(runTimeChangeAction.SourceItem, Manager),
                runTimeChangeAction.FromPath, runTimeChangeAction.ToPath, runTimeChangeAction.Version,
                runTimeChangeAction.MergeVersionTo, runTimeChangeAction.ItemTypeReferenceName, changeActionData,
                runTimeChangeAction.IsSubstituted ? ActionState.Skipped : ActionState.Pending);

            if (!m_usePagedActions)
            {
                AddAction(migrationAction);
            }

            migrationAction.IsDirty = false;
            return migrationAction;
        }

        public IMigrationAction RealizeFromEDMWithSingleAction(RTChangeGroup runTimeChangeGroup, RTChangeAction runTimeChangeAction)
        {
            RealizeChangeGroupProperties(runTimeChangeGroup);

            UseOtherSideMigrationItemSerializers = true;
            switch (Status)
            {
                case ChangeStatus.ChangeCreationInProgress:
                case ChangeStatus.Delta:
                case ChangeStatus.DeltaComplete:
                case ChangeStatus.DeltaPending:
                case ChangeStatus.DeltaSynced:
                    UseOtherSideMigrationItemSerializers = false;
                    break;
                default:
                    break;
            }

            return RealizeSingleActionFromEDM(runTimeChangeAction);
        }

        private void RealizeChangeGroupProperties(RTChangeGroup runTimeChangeGroup)
        {
          this.m_runTimeChangeGroup = runTimeChangeGroup;
          this.ChangeGroupId = runTimeChangeGroup.Id;
          this.ChangeTimeUtc = runTimeChangeGroup.RevisionTime == null ? (DateTime)runTimeChangeGroup.StartTime : (DateTime)runTimeChangeGroup.RevisionTime;
          this.Comment = runTimeChangeGroup.Comment;
          this.RevisionTime = runTimeChangeGroup.RevisionTime;
          this.ExecutionOrder = runTimeChangeGroup.ExecutionOrder;
          this.Locked = false;
          this.Name = runTimeChangeGroup.Name;
          this.Owner = runTimeChangeGroup.Owner;
          this.SessionId = runTimeChangeGroup.SessionUniqueId;
          this.SourceId = runTimeChangeGroup.SourceUniqueId;
          this.Status = (ChangeStatus)runTimeChangeGroup.Status;
          this.ContainsBackloggedAction = runTimeChangeGroup.ContainsBackloggedAction;
          this.IsForcedSync = runTimeChangeGroup.IsForcedSync.HasValue ? (bool)runTimeChangeGroup.IsForcedSync : false;
          this.m_usePagedActions = (runTimeChangeGroup.UsePagedActions.HasValue
                                    && runTimeChangeGroup.UsePagedActions.Value);
          this.ReflectedChangeGroupId = runTimeChangeGroup.ReflectedChangeGroupId.HasValue ? runTimeChangeGroup.ReflectedChangeGroupId : null;
        }

        public static int PromoteAnalysisToPending(string sessionId, Guid sourceId)
        {
            // ToDo
            throw new NotImplementedException();
        }

        public static int PromoteAnalysisToPending(IMigrationTransaction trx, string sessionId, Guid sourceId)
        {
            //ToDo
            throw new NotImplementedException();
        }

        public static int DemoteInProgressActionsToPending(string sessionId, Guid sourceId)
        {
            // ToDo
            throw new NotImplementedException();
        }


        public static int DemoteInProgressActionsToPending(IMigrationTransaction trx, string sessionId, Guid sourceId)
        {
            //ToDo
            throw new NotImplementedException();
        }

        protected override void Create()
        {
            // ToDo 
            ChangeStatus targetStatus = Status;

            SqlChangeGroupManager manager = this.Manager as SqlChangeGroupManager;
            Debug.Assert(null != manager, "Manager is not a SqlChangeGroupManager for SqlChangeGroup");

            // save the group (without child actions)
            SavePartialChangeGroup();

            // bulk save sliced child change actions
            const int bulkChangeActionInsertionSize = 1000;
            int changeActionCount = 0;
            
            while (Actions.Count > 0)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    try
                    {
                        context.Attach(m_runTimeChangeGroup);

                        while (Actions.Count > 0 && changeActionCount < bulkChangeActionInsertionSize)
                        {
                            SqlMigrationAction action = Actions[0] as SqlMigrationAction;
                            Debug.Assert(null != action);
                            action.ChangeGroup = this;

                            IMigrationItemSerializer serializer = ManagerWithMigrationItemSerializers[action.Action];
                            action.CreateNew(serializer);
                            action.RTChangeAction.ChangeGroup = m_runTimeChangeGroup;

                            // Remove processed child change actions.
                            // When this ObjectModel context is disposed,
                            // the RTChangeAction will be disposed as well.
                            Actions.RemoveAt(0);

                            ++changeActionCount;
                        }
                        context.TrySaveChanges();
                    }
                    finally
                    {
                        context.Detach(m_runTimeChangeGroup);
                    }
                }

                changeActionCount = 0;
            }

            // update group status in DB
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                try
                {
                    context.Attach(m_runTimeChangeGroup);
                    m_runTimeChangeGroup.Status = (int)targetStatus;
                    m_runTimeChangeGroup.UsePagedActions = m_usePagedActions;
                    context.TrySaveChanges();
                }
                finally
                {
                    context.Detach(m_runTimeChangeGroup);
                }
            }
        }

        private void SavePartialChangeGroup()
        {
          if (null != m_runTimeChangeGroup)
          {
            return;
          }

          SqlChangeGroupManager manager = this.Manager as SqlChangeGroupManager;
          Debug.Assert(null != manager, "Manager is not a SqlChangeGroupManager for SqlChangeGroup");

          using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
          {
            try
            {
              context.Attach(manager.RunTimeMigrationSource);
              context.Attach(manager.RuntimeSessionRun);

              // save group attributes
              m_runTimeChangeGroup = RTChangeGroup.CreateRTChangeGroup(-1, ExecutionOrder, SessionId, SourceId, (int)ChangeStatus.ChangeCreationInProgress, false);
              m_runTimeChangeGroup.Owner = Owner;
              m_runTimeChangeGroup.Comment = Comment;
              // Store the ChangeTimeUtc value in the RevsionTime column unless the ChangeTimeUtc is not set (MinValue)
              // If it's not set, store DateTime.MaxValue to indicate that because DateTime.MinValue is outside the range allowed by SQL
              m_runTimeChangeGroup.RevisionTime = ChangeTimeUtc.Equals(DateTime.MinValue) ? DateTime.MaxValue : ChangeTimeUtc;
              m_runTimeChangeGroup.StartTime = DateTime.UtcNow;
              m_runTimeChangeGroup.Name = Name;
              m_runTimeChangeGroup.ReflectedChangeGroupId = ReflectedChangeGroupId;
              m_runTimeChangeGroup.UsePagedActions = m_usePagedActions;
              m_runTimeChangeGroup.IsForcedSync = IsForcedSync;

              // establish SessionRun association
              m_runTimeChangeGroup.SessionRun = manager.RuntimeSessionRun;

              // estabilish MigrationSource association
              m_runTimeChangeGroup.SourceSideMigrationSource = manager.RunTimeMigrationSource;


              // save the group
              context.AddToRTChangeGroupSet(m_runTimeChangeGroup);
              context.TrySaveChanges();

              // record internal Id
              this.ChangeGroupId = m_runTimeChangeGroup.Id;
            }
            finally
            {
              context.Detach(m_runTimeChangeGroup);
              context.Detach(manager.RunTimeMigrationSource);
              context.Detach(manager.RuntimeSessionRun);
            }
          }
        }

        public override void UpdateConversionHistory(ConversionResult result)
        {
            SqlChangeGroupManager manager = this.Manager as SqlChangeGroupManager;
            Debug.Assert(null != manager, "Manager is not a SqlChangeGroupManager for SqlChangeGroup");
            result.Save(manager.RuntimeSessionRun.Id, manager.RunTimeMigrationSource.UniqueId, SessionId, ReflectedChangeGroupId);
            if (Manager.Session.SessionType == SessionTypeEnum.VersionControl)
            {
                updateVCSyncPoint(result);
            }
        }

        private void updateVCSyncPoint(ConversionResult result)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // Populate and save the SyncPoint info
                RTSyncPoint syncPoint =
                    RTSyncPoint.CreateRTSyncPoint(
                        0,                          // Id
                        SessionId,
                        result.SourceSideSourceId,
                        "HWMDelta",
                        result.ChangeId,
                        Constants.ChangeGroupGenericVersionNumber
                    );

                syncPoint.SourceHighWaterMarkValue = result.ItemConversionHistory.First().SourceItemId;
                syncPoint.LastChangeGroupId = m_runTimeChangeGroup.Id;

                context.AddToRTSyncPointSet(syncPoint);

                context.TrySaveChanges();
                    
            }
        }

        private RTChangeGroup getNativeRTChangeGroup(RuntimeEntityModel context)
        {
            if (null != m_runTimeChangeGroup &&
                null != m_runTimeChangeGroup.EntityKey)
            {
                return context.GetObjectByKey(m_runTimeChangeGroup.EntityKey) as RTChangeGroup;
            }
            else if (this.ChangeGroupId > 0)
            {
                var changeGroupQuery = context.RTChangeGroupSet.Where(cg => cg.Id == ChangeGroupId);
                if (changeGroupQuery.Count() > 0)
                {
                    return changeGroupQuery.First();
                }
            }

            throw new InvalidOperationException(MigrationToolkitResources.ErrorMissingParentChangeGroup);
        }
        
        /// <summary>
        /// bulk save sliced child change actions
        /// </summary>
        /// <param name="page">a page/slice of child change actions</param>
        internal void BatchSaveChangeActions(IList<IMigrationAction> page)
        {
            Debug.Assert(m_runTimeChangeGroup != null);
            
            const int bulkChangeActionInsertionSize = 1000;
            int changeActionCount = 0;
            int pageIndex = 0;

            while (pageIndex < page.Count)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    RTChangeGroup rtChangeGroupCache = getNativeRTChangeGroup(context);
                    Debug.Assert(null != rtChangeGroupCache); 
                    
                        while (pageIndex < page.Count && changeActionCount < bulkChangeActionInsertionSize)
                        {
                            SqlMigrationAction action = page[pageIndex] as SqlMigrationAction;
                            Debug.Assert(null != action);

                            // cache "Dirty" flag, as assignin 'this' to its ChangeGroup will set the flag to true
                            bool actionWasDirty = action.IsDirty;
                            action.ChangeGroup = this;

                            if (!action.IsPersisted)
                            {
                                IMigrationItemSerializer serializer = ManagerWithMigrationItemSerializers[action.Action];
                                action.CreateNew(serializer);
                                context.AddToRTChangeActionSet(action.RTChangeAction);
                                action.RTChangeAction.ChangeGroup = rtChangeGroupCache;
                                ++changeActionCount;
                            }
                            else if (actionWasDirty)
                            {
                                IMigrationItemSerializer serializer = ManagerWithMigrationItemSerializers[action.Action];
                                RTChangeAction rtChangeAction = action.RTChangeAction;
                                if (null != rtChangeAction)
                                {
                                    rtChangeAction = context.GetObjectByKey(rtChangeAction.EntityKey) as RTChangeAction;
                                }
                                else
                                {
                                    rtChangeAction = context.RTChangeActionSet.Where(ca => ca.ChangeActionId == action.ActionId).First();
                                }

                                rtChangeAction.Recursivity = action.Recursive;
                                rtChangeAction.IsSubstituted = action.State == ActionState.Skipped ? true : false;
                                rtChangeAction.ActionId = action.Action;
                                rtChangeAction.SourceItem = serializer.SerializeItem(action.SourceItem);
                                rtChangeAction.ToPath = action.Path;
                                rtChangeAction.ItemTypeReferenceName = action.ItemTypeReferenceName;
                                rtChangeAction.FromPath = action.FromPath;
                                rtChangeAction.Version = action.Version;
                                rtChangeAction.MergeVersionTo = action.MergeVersionTo;

                                if (action.MigrationActionDescription != null
                                    && action.MigrationActionDescription.DocumentElement != null)
                                {
                                    rtChangeAction.ActionData = action.MigrationActionDescription.DocumentElement.OuterXml;
                                }
                                ++changeActionCount;
                            }
                            
                            ++pageIndex;
                        }
                        context.TrySaveChanges();
                        changeActionCount = 0;                    

                }
            }
        }

        private static object getSqlSafeString(string str)
        {
            return (str == null) ? string.Empty : (object)str;
        }

        private void populateMetaData(RTChangeGroup runtimeChangeGroup)
        {
            runtimeChangeGroup.Owner = Owner;
            runtimeChangeGroup.Comment = Comment;
            runtimeChangeGroup.ExecutionOrder = ExecutionOrder;
            runtimeChangeGroup.UsePagedActions = m_usePagedActions;
        }

        protected override void Update()
        {
            if (m_usePagedActions)
            {
                PagedCollection<IMigrationAction> pagedActions = this.Actions as PagedCollection<IMigrationAction>;
                pagedActions.SaveActivePage();
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTChangeGroup rtChangeGroupCache = context.GetObjectByKey(m_runTimeChangeGroup.EntityKey) as RTChangeGroup;

                switch (Status)
                {
                    case ChangeStatus.InProgress:
                        rtChangeGroupCache.StartTime = DateTime.UtcNow;
                        break;
                    case ChangeStatus.DeltaComplete:
                    case ChangeStatus.Complete:
                        rtChangeGroupCache.FinishTime = DateTime.UtcNow;
                        break;
                    default:
                        break;
                }
                rtChangeGroupCache.Status = (int)Status;
                rtChangeGroupCache.ReflectedChangeGroupId = ReflectedChangeGroupId;
                populateMetaData(rtChangeGroupCache);
                context.TrySaveChanges();
            }
        }

        internal void PersistCurrentStatus(RuntimeEntityModel context)
        {
            RTChangeGroup rtChangeGroupCache = context.GetObjectByKey(m_runTimeChangeGroup.EntityKey) as RTChangeGroup;
            switch (Status)
            {
                case ChangeStatus.InProgress:
                    rtChangeGroupCache.StartTime = DateTime.UtcNow;
                    break;
                case ChangeStatus.DeltaComplete:
                case ChangeStatus.Complete:
                    rtChangeGroupCache.FinishTime = DateTime.UtcNow;
                    break;
                default:
                    break;
            }
            rtChangeGroupCache.Status = (int)Status;
        }

        internal override void UpdateStatus(ChangeStatus newChangeStatus)
        {
            if (Status != newChangeStatus)
            {
                Status = newChangeStatus;

                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    PersistCurrentStatus(context);
                    context.TrySaveChanges();
                }
            }
        }

        /// <summary>
        /// Updates child action's status to the data store.
        /// </summary>
        /// <param name="action"></param>
        protected override void UpdateChildAction(MigrationAction action)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var changeActionQuery = context.RTChangeActionSet.Where
                    (ca => ca.ChangeActionId == action.ActionId);
                int changeActionQueryCount = changeActionQuery.Count();
                if (changeActionQueryCount == 0)
                {
                    return;
                }

                Debug.Assert(changeActionQueryCount == 1);
                RTChangeAction rtChangeAction = changeActionQuery.First();

                bool needToUpdateRTChangeAction = false;

                if (action.State == ActionState.Skipped)
                {
                    rtChangeAction.IsSubstituted = true;
                    needToUpdateRTChangeAction = true;
                }
                if (rtChangeAction.ActionId != action.Action)
                {
                    rtChangeAction.ActionId = action.Action;
                    needToUpdateRTChangeAction = true;
                }

                if (needToUpdateRTChangeAction)
                {
                    context.TrySaveChanges();
                }
            }
        }

        internal static ChangeGroup Next(ChangeGroupManager manager)
        {
            // ToDo
            throw new NotImplementedException();
        }

        private static Collection<IMigrationAction> realizeActionList(SqlDataReader reader, ChangeGroupManager manager, ChangeGroup parent)
        {
            throw new NotImplementedException();
        }

        private static ChangeGroup realizeChangeGroup(SqlDataReader reader, ChangeGroupManager manager)
        {
            SqlChangeGroup group = null;

            if (reader != null && reader.HasRows)
            {
                if (reader.Read())
                {
                    loadOrdinalCache(reader);

                    group = new SqlChangeGroup(manager);
                    group.ChangeGroupId = reader.GetInt32(s_ordinalChangeGroupId);
                    group.ChangeTimeUtc = reader.GetDateTime(s_ordinalChangeTime);
                    group.Comment = reader.GetString(s_ordinalComment);
                    group.ExecutionOrder = reader.GetInt64(s_ordinalExecutionOrder);
                    group.Owner = reader.GetString(s_ordinalOwner);
                    group.Status = (ChangeStatus)reader.GetInt32(s_ordinalStatus);
                    group.SessionId = new Guid(reader.GetString(s_ordinalSessionId));
                    group.SourceId = reader.GetGuid(s_ordinalSourceId);
                    // TODO DB changed neededgroup.Direction = (MigrationDirection)reader.GetInt32(s_ordinalDirection);
                    group.Name = reader.GetString(s_ordinalName);
                }

                if (reader.Read())
                {
                    throw new MigrationException(MigrationToolkitResources.TooManyChangeGroupsReturned);
                }
            }

            return group;
        }

        static void loadOrdinalCache(SqlDataReader reader)
        {
            if (!s_ordinalCacheLoaded)
            {
                lock (s_ordinalCacheLocker)
                {
                    if (!s_ordinalCacheLoaded)
                    {
                        Debug.Assert(reader.FieldCount == 9);
                        s_ordinalChangeGroupId = reader.GetOrdinal("ChangeGroupID");
                        s_ordinalExecutionOrder = reader.GetOrdinal("ExecutionOrder");
                        s_ordinalOwner = reader.GetOrdinal("Owner");
                        s_ordinalComment = reader.GetOrdinal("Comment");
                        s_ordinalChangeTime = reader.GetOrdinal("ChangeTime");
                        s_ordinalStatus = reader.GetOrdinal("Status");
                        s_ordinalSessionId = reader.GetOrdinal("SessionId");
                        s_ordinalSourceId = reader.GetOrdinal("SourceId");
                        s_ordinalName = reader.GetOrdinal("Name");
                        s_ordinalCacheLoaded = true;
                    }
                }
            }
        }

        private void ListenToDefaultActionCollectionChange()
        {
            var defaultActions = base.Actions as INotifyCollectionChanged;
            if (null != defaultActions)
            {
                defaultActions.CollectionChanged += new NotifyCollectionChangedEventHandler(defaultActions_CollectionChanged);
            }
        }

        void defaultActions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (base.Actions.Count() > UsePagedCollectionSizeThreshold)
            {
                m_usePagedActions = true;

                SavePartialChangeGroup();

                if (m_actions == null)
                {
                    // The provider assumes that the SqlChangeGroup is persisted to the data store
                    SqlChangeGroupProvider provider = new SqlChangeGroupProvider(m_runTimeChangeGroup, (SqlChangeGroupManager)Manager);
                    m_actions = new PagedCollection<IMigrationAction>(provider, m_sqlChangeActionPageSize, m_sqlChangeActionTimeToLive);
                }

                foreach (var action in base.Actions)
                {
                    m_actions.Add(action);
                }
                base.Actions.Clear();
            }
        }

        static object s_ordinalCacheLocker = new object();
        static bool s_ordinalCacheLoaded;
        
        static int s_ordinalChangeGroupId;
        static int s_ordinalExecutionOrder;
        static int s_ordinalOwner;
        static int s_ordinalComment;
        static int s_ordinalChangeTime;
        static int s_ordinalStatus;
        static int s_ordinalSessionId;
        static int s_ordinalSourceId;
        static int s_ordinalName;

        RTChangeGroup m_runTimeChangeGroup;

        PagedCollection<IMigrationAction> m_actions;

    }

}
