// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class SessionControlEventArgs : EventArgs
    {
        // TODO
    }

    class SessionWorker : ISessionOrchestrator
    {
        private Thread m_thread;
        private RuntimeEntityModel m_context;
        private string m_threadName;
        private int m_sessionRunId;

        private SyncStateMachine m_syncStateMachine;
        private SessionOrchestrationPolicy m_orchPolicy;
        private ISyncStateManager m_syncStateManager;
        private ISyncCommandQueue m_syncCommandQueue;

        public event EventHandler<SessionControlEventArgs> SessionControlStop;
        public event EventHandler<SessionControlEventArgs> SessionControlPause;
        public event EventHandler<SessionControlEventArgs> SessionControlPauseForConflict;
        public event EventHandler<SessionControlEventArgs> SessionControlResume;

        #region properties
        public Guid SessionId { get; set; }
        public ManualResetEvent Event { get; set; }
        public AnalysisEngine AnalysisEngine { get; set; }
        public MigrationEngine MigrationEngine { get; set; }
        public LinkEngine LinkEngine { get; set; }
        public Guid LeftMigrationSourceid { get; set; }
        public Guid RightMigrationSourceid { get; set; }
        public WorkFlowType WorkFlowType { get; set; }
        public int MilliSecondsSyncWaitInterval { get; set; }
        public Thread Thread
        {
            get
            {
                return m_thread;
            }
        }

        public PipelineState CurrentState
        {
            get
            {
                return m_syncStateMachine.CurrentState;
            }
        }

        #endregion

        internal SessionWorker(
            Guid sessionId,
            ManualResetEvent sessionEvent,
            AnalysisEngine analysisEngine, 
            MigrationEngine migrationEngine,
            LinkEngine linkEngine,
            Guid leftMigrationSourceId, 
            Guid rightMigrationSourceId, 
            WorkFlowType workFlowType,
            int secondsSyncWaitInterval,
            string threadName)
        {
            SessionId = sessionId;
            Event = sessionEvent;
            AnalysisEngine = analysisEngine;
            MigrationEngine = migrationEngine;
            LinkEngine = linkEngine;
            LeftMigrationSourceid = leftMigrationSourceId;
            RightMigrationSourceid = rightMigrationSourceId;
            WorkFlowType = workFlowType;
            m_threadName = threadName;  
          
            SqlSyncStateManager manager = SqlSyncStateManager.GetInstance();
            m_syncStateManager = manager;
            m_syncCommandQueue = manager;

            m_syncStateMachine = new SyncStateMachine(PipelineState.Default, new SyncStateTransitionAlgorithm(),
                                                      OwnerType.Session, sessionId, m_syncStateManager);
            m_orchPolicy = new SessionOrchestrationPolicy(WorkFlowType, m_syncStateMachine);

            try
            {
                checked
                {
                    MilliSecondsSyncWaitInterval = secondsSyncWaitInterval * 1000;
                }
            }
            catch (OverflowException)
            {
                MilliSecondsSyncWaitInterval = int.MaxValue;
                TraceManager.TraceInformation(
                    "The speicified interval of {0} minutes is too long for the system to handle. The interval is now changed to {1} minutes.",
                    secondsSyncWaitInterval / 60,
                    (int)(MilliSecondsSyncWaitInterval / 1000 / 60));
            }
        }

        public void Start()
        {
            m_thread.Start();
        }

        public void Initialize(int sessionRunId)
        {
            m_context = RuntimeEntityModel.CreateInstance();

            m_sessionRunId = sessionRunId;
            AnalysisEngine.Initialize(sessionRunId, this);
            MigrationEngine.Initialize(sessionRunId);

            // Create a worker thread and mark it a background thread
            m_thread = new Thread(Run);
            m_thread.Name = m_threadName;
            m_thread.IsBackground = true;

            SubscribeListeners();
        }

        /// <summary>
        /// Reset the sync state to the default unless the state was persisted as PausedByConflict
        /// </summary>
        internal void InitializeSyncState()
        {
            // Reload the current state and check if it is PausedByConflict
            // The session should remain in the PausedByConflict state until all conflicts are resolved
            m_syncStateMachine.Reload();
            if (m_syncStateMachine.CurrentState != PipelineState.PausedByConflict)
            {
                m_syncStateMachine.Reset();
            }
        }

        private void Run()
        {
            TraceManager.TraceInformation(String.Format("Session worker thread [{0}] started", m_thread.Name));

            bool sessionFinishesSuccessfully = false;

            while (!sessionFinishesSuccessfully)
            {
                try
                {
                    MarkSessionRunning();
                    ProcessPipeline(WorkFlowType);
                    sessionFinishesSuccessfully = true;
                }
                catch (Exception ex)
                {
                    TraceManager.TraceError(String.Format("Session worker thread [{0}] exception: {1}",
                        m_thread.Name, ex is MissingErrorRouterException ? ex.InnerException : ex));

                    TraceManager.TraceInformation(string.Format("Restarting session in session worker thread [{0}]", m_thread.Name));
                }
            }
            
            TraceManager.TraceInformation(String.Format("Session worker thread [{0}] completed", m_thread.Name));
            // Signal SyncOrchestrator that this thread is done
            Event.Set();
        }

        private void MarkSessionRunning()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionQuery = context.RTSessionSet.Where
                    (s => s.SessionUniqueId.Equals(SessionId));
                Debug.Assert(sessionQuery.Count() == 1);

                // we want to keep StartedInStandaloneProcess session status untouched
                if (sessionQuery.First().State != (int)BusinessModelManager.SessionStateEnum.StartedInStandaloneProcess)
                {
                    sessionQuery.First().State = (int)BusinessModelManager.SessionStateEnum.Running;
                }
                context.TrySaveChanges();
            }

            // If the OrchestrationStatus for the session is PausedByConflict, it should stay in that state
            // until all conflict for the session are resolved
            if (m_syncStateMachine.CurrentState != PipelineState.PausedByConflict)
            {
                if (m_syncStateMachine.TryTransit(PipelineSyncCommand.START))
                {
                    m_syncStateMachine.CommandTransitFinished(PipelineSyncCommand.START);
                }
                else
                {
                    throw new MigrationException();
                }
            }
        }

        #region private methods
        private void SubscribeListeners()
        {
            SessionControlStop += AnalysisEngine.SessionStopEventHandler;
            SessionControlStop += MigrationEngine.SessionStopEventHandler;

            SessionControlPause += AnalysisEngine.SessionPauseEventHandler;
            SessionControlPause += MigrationEngine.SessionPauseEventHandler;

            SessionControlPauseForConflict += AnalysisEngine.SessionPauseForConflictEventHandler;
            SessionControlPauseForConflict += MigrationEngine.SessionPauseForConflictEventHandler;

            SessionControlResume += AnalysisEngine.SessionResumeEventHandler;
            SessionControlResume += MigrationEngine.SessionResumeEventHandler;
        }

        private void OneDirectionProcessPipeline(
            bool isLeftToRight,
            Guid sourceMigrationSourceId,
            Guid targetMigrationSourceId,
            bool contextSyncNeeded,
            bool bidirection)
        {
            try
            {
                m_orchPolicy.Check();
                AnalysisEngine.SourceMigrationSourceId = sourceMigrationSourceId;
                AnalysisEngine.InvokePreAnalysisAddins(sourceMigrationSourceId);

                if (!AnalysisEngine.InvokeProceedToAnalysisOnAnalysisAddins(sourceMigrationSourceId))
                {
                    // In case any of the AnalysisAddins perform cleanup in the PostAnalysis method
                    AnalysisEngine.InvokePostAnalysisAddins(sourceMigrationSourceId);
                    return;
                }

                TraceManager.TraceInformation("Pipeline flow from {0} to {1}", sourceMigrationSourceId, targetMigrationSourceId);
                if (contextSyncNeeded)
                {
                    TraceManager.TraceInformation("Generating context info tables for the migration source {0}", sourceMigrationSourceId);
                    AnalysisEngine.GenerateContextInfoTables(sourceMigrationSourceId);
                    m_orchPolicy.Check();
                }

                TraceManager.TraceInformation("Generating delta tables for the migration source {0}", sourceMigrationSourceId);
                AnalysisEngine.GenerateDeltaTables(sourceMigrationSourceId);
                m_orchPolicy.Check();

                AnalysisEngine.InvokePostDeltaComputationAddins(sourceMigrationSourceId);

                TraceManager.TraceInformation("Generating linking delta for the migration source {0}", sourceMigrationSourceId);
                LinkEngine.GenerateLinkDelta(SessionId, sourceMigrationSourceId);
                m_orchPolicy.Check();

                AnalysisEngine.InvokePostAnalysisAddins(sourceMigrationSourceId);

                // Mark the items provided by the ForceSyncItemService processed at this time
                IForceSyncItemService forceSyncItemService = AnalysisEngine[sourceMigrationSourceId].GetService(typeof(IForceSyncItemService)) as IForceSyncItemService;
                if (forceSyncItemService != null)
                {
                    forceSyncItemService.MarkCurrentItemsProcessed();
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Above: data collection at source side                                                                            //
                // Below: data migration/submission at target side                                                                  //
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                MigrationEngine.InvokePreMigrationAddins(targetMigrationSourceId);

                if (contextSyncNeeded)
                {
                    TraceManager.TraceInformation("Establishing context for the migration source {0}", targetMigrationSourceId);
                    MigrationEngine.EstablishContext(targetMigrationSourceId);
                    m_orchPolicy.Check();
                }

                if (!AnalysisEngine.DisableTargetAnalysis)
                {
                    TraceManager.TraceInformation("Generating delta tables for the migration source {0}", targetMigrationSourceId);
                    AnalysisEngine.GenerateDeltaTables(targetMigrationSourceId);
                    m_orchPolicy.Check();

                    // Mark the items provided by the ForceSyncItemService (if any) processed at this time
                    forceSyncItemService = AnalysisEngine[targetMigrationSourceId].GetService(typeof(IForceSyncItemService)) as IForceSyncItemService;
                    if (forceSyncItemService != null)
                    {
                        forceSyncItemService.MarkCurrentItemsProcessed();
                    }
                }

                TraceManager.TraceInformation("Generating migration instructions for the migration source {0}", targetMigrationSourceId);
                AnalysisEngine.GenerateMigrationInstructions(targetMigrationSourceId);
                m_orchPolicy.Check();

                TraceManager.TraceInformation("Post-processing delta table entries from the migration source {0}", targetMigrationSourceId);
                AnalysisEngine.PostProcessDeltaTableEntries(targetMigrationSourceId, bidirection);
                m_orchPolicy.Check();

                TraceManager.TraceInformation("Migrating to the migration source {0}", targetMigrationSourceId);
                MigrationEngine.Migrate(targetMigrationSourceId, m_orchPolicy);
                m_orchPolicy.Check();

                MigrationEngine.InvokePostMigrationAddins(targetMigrationSourceId);

                TraceManager.TraceInformation("Processing linking delta");
                LinkEngine.AnalyzeLinkDelta(SessionId, sourceMigrationSourceId, bidirection);
                m_orchPolicy.Check();

                TraceManager.TraceInformation("Migrating links to the migration source {0}", targetMigrationSourceId);
                LinkEngine.MigrateLinks(SessionId, targetMigrationSourceId);
                m_orchPolicy.Check();
            }
            finally
            {
                // Record a sync point in the database
                try
                {
                    RecordSyncPoint(isLeftToRight, sourceMigrationSourceId, targetMigrationSourceId);
                }
                catch (Exception ex)
                {
                    TraceManager.TraceWarning("{0}: Unable to record SyncPoint data due to exception: {1}", m_thread.Name, ex.ToString());
                }
            }
        }


        private void ProcessPipeline(WorkFlowType workFlowType)
        {
            bool bidirection = IsBidirectionalWorkFlowType(workFlowType);
            bool leftToRightContextSyncNeeded = IsLeftToRightContextSyncNeeded(workFlowType);
            bool rightToLeftContextSyncNeeded = IsRightToLeftContextSyncNeeded(workFlowType);

            // Don't start processing the pipeline if the session was previously paused by conflicts until they are resolved
            if (m_syncStateMachine.CurrentState == PipelineState.PausedByConflict)
            {
                m_orchPolicy.PauseSessionUntilConflictsResolved(this);
            }

            do
            {
                // always notifying the listeners the pipeline is (re)starting
                SessionControlResume(this, new SessionControlEventArgs());

                try
                {
                    #region ----- LEFT TO RIGHT -----
                    OneDirectionProcessPipeline(true, LeftMigrationSourceid, RightMigrationSourceid, leftToRightContextSyncNeeded, bidirection);
                    #endregion

                    TraceManager.TraceInformation("");

                    #region ----- RIGHT TO LEFT -----
                    if (bidirection)
                    {
                        OneDirectionProcessPipeline(false, RightMigrationSourceid, LeftMigrationSourceid, rightToLeftContextSyncNeeded, bidirection);
                    }
                    else
                    {
                        CleanupDeltaTable(RightMigrationSourceid);
                    }
                    #endregion

                    #region ----- The current round-trip is stopping -----
                    if (m_syncStateMachine.TryTransit(PipelineSyncCommand.STOP_CURRENT_TRIP))
                    {
                        m_syncStateMachine.CommandTransitFinished(PipelineSyncCommand.STOP_CURRENT_TRIP);
                    } 
                    #endregion
                }
                catch (SessionOrchestrationPolicy.PauseConflictedSessionException)
                {
                    // When PausedConflictedSession is requested, we continue and TryNextRoundTrip will delay
                    // until the conflicts are resolved
                    TraceManager.TraceInformation("{0}: Session paused until conflicts are resolved!", m_thread.Name);
                    continue;
                }
                catch (SessionOrchestrationPolicy.StopSessionException)
                {
                    TraceManager.TraceInformation("{0}: Session aborted!", m_thread.Name);
                    return;
                }
                catch (SessionOrchestrationPolicy.StopSingleTripException)
                {
                    // When Stop is requested, we continue and let the policy to decide 
                    // whether it should start another round trip or not
                    TraceManager.TraceInformation("{0}: Session stopped!", m_thread.Name);
                    continue;
                }
                catch (Exception)
                {
                    throw;
                }

                switch (workFlowType.Frequency)
                {
                    case Frequency.ContinuousAutomatic:
                        TraceManager.TraceInformation("{0}: Sync is done!", m_thread.Name);
                        TraceManager.TraceInformation("{0}: Waiting {1} seconds before next synchronization",
                                                      m_thread.Name,
                                                      MilliSecondsSyncWaitInterval / 1000);
                        break;
                    case Frequency.ContinuousManual:
                    case Frequency.OneTime:
                        TraceManager.TraceInformation("{0}: Migration is done!", m_thread.Name);
                        MigrationSessionCompleted();
                        break;
                    default:
                        throw new ArgumentException("m_orchPolicy.WorkFlowType.Frequency");
                }
                
            } while (m_orchPolicy.TryStartNextRoundTrip(this, MilliSecondsSyncWaitInterval));
        }

        private void CleanupDeltaTable(Guid migrationSourceid)
        {
            AnalysisEngine.ObsoleteDeltaTableEntries(migrationSourceid);
        }

        /// <summary>
        /// Marks a non-syncworkflow session to be completed
        /// and the parent session group to be completed if all its child
        /// sessions are in "completed (3)" status
        /// </summary>
        private void MigrationSessionCompleted()
        {
            // update the session state to Completed
            // 
            // NOTE: 
            // The sproc called here through EDM sets the Session *Group* to
            // be completed if all the sibling sessions of the current session (inclusively)
            // are completed.
            var updatedRTSessions = m_context.UpdateMigrationSessionStatusToCompleted(SessionId);

            if (updatedRTSessions.Count() > 0
                && WorkFlowType.Frequency == Frequency.OneTime)
            {
                // mark Session Group to be OneTimeCompleted (value: 4)

                // Note: updatedRTSessions has already been enumerated in the EDM imported function UpdateMigrationSessionStatusToCompleted
                // We have to ask the Context for a new instance of the RTSession, rather than enumerating on updatedRTSessions
                RTSession rtSession = m_context.RTSessionSet.Where(s => s.SessionUniqueId.Equals(SessionId)).First();
                rtSession.SessionGroupReference.Load();
                if (rtSession.SessionGroup.State == (int)BusinessModelManager.SessionStateEnum.Completed)
                {
                    rtSession.SessionGroup.State = (int)BusinessModelManager.SessionStateEnum.OneTimeCompleted;
                    m_context.TrySaveChanges();
                }
            }

            // update the session sync orchestration state
            if (m_syncStateMachine.TryTransit(PipelineSyncCommand.STOP))
            {
                m_syncStateMachine.CommandTransitFinished(PipelineSyncCommand.STOP);
            }

            var rtSessionRun = m_context.RTSessionRunSet.Where(r => r.Id == m_sessionRunId).FirstOrDefault();
            if (null != rtSessionRun)
            {
                rtSessionRun.State = (int)BusinessModelManager.SessionStateEnum.Completed;
                m_context.TrySaveChanges();
            }
        }

        private static bool IsLeftToRightContextSyncNeeded(WorkFlowType workFlowType)
        {
            return (workFlowType.SyncContext == SyncContext.Unidirectional
                   || workFlowType.SyncContext == SyncContext.Bidirectional);
        }

        private static bool IsRightToLeftContextSyncNeeded(WorkFlowType workFlowType)
        {
            return workFlowType.SyncContext == SyncContext.Bidirectional;
        }

        private static bool IsBidirectionalWorkFlowType(WorkFlowType workFlowType)
        {
            return workFlowType.DirectionOfFlow == DirectionOfFlow.Bidirectional;
        }

        // Insert a row into the SYNC_POINT table
        private void RecordSyncPoint(
            bool isLeftToRight,
            Guid sourceMigrationSourceId,
            Guid targetMigrationSourceId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // Get the current source high water mark
                var highWaterMarkQuery =
                            (from h in context.RTHighWaterMarkSet
                             where (h.SessionUniqueId == SessionId && h.SourceUniqueId == sourceMigrationSourceId)
                             select h).Take(1);

                if (highWaterMarkQuery.Count() > 0)
                {
                    RTHighWaterMark sourceHighWaterMark = highWaterMarkQuery.First();

                    // Get the corresponding target item
                    MigrationItemId lastMigratedTargetItem = MigrationEngine.TranslationService.GetLastMigratedItemId(targetMigrationSourceId);

                    // find last ChangeGroupId as of the sync point
                    var lastChangeGroupQuery =
                        (from g in context.RTChangeGroupSet
                         where g.SessionUniqueId.Equals(this.SessionId)
                         && (g.SourceUniqueId.Equals(sourceMigrationSourceId) || g.SourceUniqueId.Equals(targetMigrationSourceId))
                         orderby g.Id descending
                         select g.Id).Take(1);

                    long lastChangeGroupId = (lastChangeGroupQuery.Count() > 0 ? lastChangeGroupQuery.First() : 0);

                    // Use the previous sync point's values lastMigratedTargetItem the sync point if there is no value for lastMigratedTargetItem.ItemId
                    if (string.IsNullOrEmpty(lastMigratedTargetItem.ItemId))
                    {
                        IQueryable<RTSyncPoint> lastChangeSyncPointQuery =
                            (from syncPt in context.RTSyncPointSet
                             where syncPt.SessionUniqueId.Equals(this.SessionId)
                                && syncPt.SourceUniqueId.Equals(sourceMigrationSourceId)
                                && syncPt.SourceHighWaterMarkName.Equals(sourceHighWaterMark.Name)
                             orderby syncPt.Id descending
                             select syncPt).Take(1);
                        if (lastChangeSyncPointQuery.Count() > 0)
                        {
                            RTSyncPoint previousSyncPoint = lastChangeSyncPointQuery.First();
                            lastMigratedTargetItem.ItemId = previousSyncPoint.LastMigratedTargetItemId;
                            lastMigratedTargetItem.ItemVersion = previousSyncPoint.LastMigratedTargetItemVersion;
                        }
                    }

                    // Don't write the sync point if there is still no value for lastMigratedTargetItem.ItemId
                    if (!string.IsNullOrEmpty(lastMigratedTargetItem.ItemId))
                    {
                        // Populate and save the SyncPoint info
                        RTSyncPoint syncPoint =
                            RTSyncPoint.CreateRTSyncPoint(
                                0,                          // Id
                                SessionId,
                                sourceMigrationSourceId,
                                sourceHighWaterMark.Name,
                                lastMigratedTargetItem.ItemId,
                                lastMigratedTargetItem.ItemVersion
                            );

                        syncPoint.SourceHighWaterMarkValue = sourceHighWaterMark.Value;
                        syncPoint.LastChangeGroupId = lastChangeGroupId;

                        context.AddToRTSyncPointSet(syncPoint);

                        context.TrySaveChanges();

                        TraceManager.TraceInformation("Recorded sync point for migration source {0} of session {1} with Source High Water Mark '{2}' value of '{3}'",
                            sourceMigrationSourceId, SessionId, syncPoint.SourceHighWaterMarkName, syncPoint.SourceHighWaterMarkValue);
                    }
                }
            }
        }

        #endregion

        #region ISessionOrchestrator Members
        public void StopCurrentTrip()
        {
            if (m_syncStateMachine.TryTransit(PipelineSyncCommand.STOP_CURRENT_TRIP))
            {
                if (SessionControlStop != null)
                {
                    SessionControlStop(this, new SessionControlEventArgs());
                }
            }
            else
            {
                TraceManager.TraceError("StopCurrentTrip failed");
            }
        }

        public void Pause()
        {
            if (m_syncStateMachine.TryTransit(PipelineSyncCommand.PAUSE))
            {
                if (SessionControlPause != null)
                {
                    SessionControlPause(this, new SessionControlEventArgs());
                }
            }
            else
            {
                TraceManager.TraceWarning("Unable to pause session in current state: {0}", m_syncStateMachine.CurrentState);
            }
        }

        public void PauseForConflict()
        {
            if (m_syncStateMachine.TryTransit(PipelineSyncCommand.PAUSE_FOR_CONFLICT))
            {
                if (SessionControlPauseForConflict != null)
                {
                    SessionControlPauseForConflict(this, new SessionControlEventArgs());
                }
            }
            else
            {
                TraceManager.TraceError("PauseForConflict failed");
            }
        }

        public void Resume()
        {
            if (m_syncStateMachine.TryTransit(PipelineSyncCommand.RESUME))
            {
                if (SessionControlResume != null)
                {
                    SessionControlResume(this, new SessionControlEventArgs());
                }
            }
            else
            {
                TraceManager.TraceWarning("Unable to resume session in current state: {0}", m_syncStateMachine.CurrentState);
            }
        }

        public void Stop()
        {
            if (!m_syncStateMachine.TryTransit(PipelineSyncCommand.STOP))
            {
                TraceManager.TraceWarning("Unable to stop session in current state: {0}", m_syncStateMachine.CurrentState);
            }
        }
              

        #endregion
    }
}
