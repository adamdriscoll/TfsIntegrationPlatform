// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Data.EntityModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.Toolkit.WIT;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class SyncOrchestrator
    {
        public enum ConflictsSyncOrchOptions
        {
            StopConflictedSessionCurrentTrip,
            StopConflictedSession,
            StopAllSessionsCurrentTrip,
            Continue,
        }

        // If any running session threads are still alive after the following timeout expires, 
        // SyncOrchestrator will abort any alive worker threads forcefully
        // By default it waits 10 minutes for the session worker threads to die
        private const int MinutesSessionStopWaitTimeout = 10;
        
        private Dictionary<Guid, BM.MigrationSource> m_migrationSources = new Dictionary<Guid, BM.MigrationSource>();
        private int m_sessoinGroupRunId;

        private ConflictManager m_conflictManager;
        private List<ConflictManager> m_conflictManagers = new List<ConflictManager>();

        // array of session worker threads
        private Dictionary<Guid, SessionWorker> m_sessionWorkers;

        // todo: get setting from configuration
        private ConflictsSyncOrchOptions m_conflictSyncOption = ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip;

        // If it's synchronization work flow (WorkFlowTypeEnum.OneDirectionalSynchronization 
        // or WorkFlowTypeEnum.BiDirectionalSynchronization), wait for the following miliseconds 
        // after completing one migration. 
        // By default it waits 30 seconds before next synchronization. 
        private const int DefaultSyncInternalInSeconds = 30;
        private int m_syncIntervalInSeconds;
        private long m_syncDurationInMilliSeconds;

        private bool m_toolkitVersionChanged = false;
        private int m_toolkitProviderInternalId = 0;
        private LinkEngine m_linkEngine;

        private Thread m_syncCommandPollingThread;
        private SyncStateMachine m_sessionGroupStateMachine;
        private ISyncCommandQueue m_syncCommandQueue;
        private PipelineSyncCommand? m_processingCommand = null;
        private RuntimeEntityModel m_sessionGroupStatePollingContext = RuntimeEntityModel.CreateInstance();

        private UserIdentityLookupService m_userIdLookupService;
        private ErrorManager m_errorManager;
        private ICredentialManagementService m_credentialManagementService;

        #region properties
        private BM.Configuration Configuration { get; set; }
        private Dictionary<Guid, ProviderHandler> ProviderHandlers { get; set; }
        private AddinManagementService AddinManagementService { get; set; }
        private Dictionary<Guid, BM.MigrationSource> MigrationSources { get { return m_migrationSources; } }
        private List<ManualResetEvent> m_events = null;
        private ManualResetEvent[] SessionWorkerEvents
        {
            get
            {
                Debug.Assert(m_sessionWorkers != null);
                Debug.Assert(m_sessionWorkers.Count() > 0);

                if (m_events == null)
                {
                    m_events= new List<ManualResetEvent>();
                    foreach (SessionWorker worker in m_sessionWorkers.Values)
                    {
                        m_events.Add(worker.Event);
                    }
                }
                return m_events.ToArray();
            }
        }
        #endregion

        #region constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <exception cref="DBSchemaValidationException" />
        /// <exception cref="InitializationException" />
        public SyncOrchestrator(BM.Configuration config)
        {
            ValidateDBSchemaVersion();

            this.Configuration = config;

            // Load migration sources
            foreach (var source in config.SessionGroup.MigrationSources.MigrationSource)
            {
                MigrationSources.Add(new Guid(source.InternalUniqueId), source);
            }

            SessionsElement sessions = Configuration.SessionGroup.Sessions;
            m_sessionWorkers = new Dictionary<Guid, SessionWorker>(sessions.Session.Count);

            // Load Adapters and Add-Ins
            ProviderManager providerManager = new ProviderManager(Configuration);
            ProviderHandlers = providerManager.LoadProvider(new DirectoryInfo(Constants.PluginsFolderName));
            TraceManager.TraceInformation("{0} Adapter instance(s) loaded", ProviderHandlers.Count);
            AddinManagementService = providerManager.AddinManagementService;
            TraceManager.TraceInformation("{0} Add-Ins loaded", AddinManagementService.Count);

            // Validate and Initialize Add-Ins
            var addinElemCollection = config.Addins.Addin;
            AddinManagementService.ValidateRegisteredAddins(addinElemCollection);
            foreach (IAddin addin in AddinManagementService.RegisteredAddins)
            {
                try
                {
                    addin.Initialize(config);
                }
                catch (Exception e)
                {
                    TraceManager.TraceVerbose(string.Format("Addin '{0}' failed to initialize. Exception details:", addin.ReferenceName.ToString()));
                    TraceManager.TraceVerbose(e.ToString());
                }
            }

            // Register the migration source specific Addins with the AddinManagementService
            AddinManagementService.RegisterMigrationSourceAddins(MigrationSources);

            // Toolkit is also considered a provider, we try saving it to db as well
            ProviderDescriptionAttribute toolkitDescAttribute = new ProviderDescriptionAttribute(Constants.FrameworkSourceId.ToString(), 
                                                                                                 Constants.FrameworkName, 
                                                                                                 Constants.FrameWorkVersion);
            m_toolkitVersionChanged = ProviderHandler.TrySaveProvider(toolkitDescAttribute, out m_toolkitProviderInternalId);

            // Load optional Sync-interval setting
            m_syncIntervalInSeconds = (Configuration.SessionGroup.SyncIntervalInSeconds <= 0) 
                                      ? DefaultSyncInternalInSeconds
                                      : Configuration.SessionGroup.SyncIntervalInSeconds;

            m_syncDurationInMilliSeconds = TimeoutMinutesToMillisecs(Configuration.SessionGroup.SyncDurationInMinutes);

            m_syncCommandPollingThread = new Thread(PollSyncCommand);
            m_syncCommandPollingThread.IsBackground = true;
            var syncStateManager = SqlSyncStateManager.GetInstance();
            m_sessionGroupStateMachine = new SyncStateMachine(PipelineState.Default,
                                                              new SyncStateTransitionAlgorithm(),
                                                              OwnerType.SessionGroup,
                                                              config.SessionGroupUniqueId,
                                                              syncStateManager);
            m_syncCommandQueue = syncStateManager;
            m_userIdLookupService = new UserIdentityLookupService(config, AddinManagementService);

            m_errorManager = ErrorManager.CreateSingletonInstance(config.SessionGroupUniqueId, config.SessionGroup.ErrorManagement, this);

            m_credentialManagementService = new CredentialManagementService(config);
        }

        #endregion

        #region public methods
        public void Start()
        {
            Start(m_syncDurationInMilliSeconds);
        }

        public void Start(int minutesTimeout)
        {
            m_syncDurationInMilliSeconds = TimeoutMinutesToMillisecs(minutesTimeout);
            Start(m_syncDurationInMilliSeconds);
        }

        public void BlockUntilAllSessionFinishes()
        {
            // Wait for all session worker threads to complete
            WaitHandle.WaitAll(SessionWorkerEvents);
        }

        public void ConstructPipelines()
        {
            try
            {
                InitializeGlobalConflictManager(Configuration);
                CreateSessionGroupRunEntry(Configuration);

                m_linkEngine = CreateLinkEngine(Configuration);
                m_linkEngine.ErrorManager = m_errorManager;

                // Process pipeline for each session
                SessionsElement sessions = Configuration.SessionGroup.Sessions;
                int index = 0;
                foreach (Session session in sessions.Session)
                {
                    FillSessionConfigInfo(session);
                    SessionWorker workerThread = ConstructSessionPipeline(session, index, m_linkEngine);
                    index++;
                }


                if (m_toolkitVersionChanged)
                {
                    m_conflictManager.ValidateAndSaveProviderConflictRegistration(m_toolkitProviderInternalId);
                }
            }
            catch (Exception e)
            {
                m_errorManager.TryHandleException(new InitializationException(e.Message ?? string.Empty, e), m_conflictManager);
                throw;
            }
        }

        public void InitializePipelines()
        {
            try
            {
                // Process pipeline for each session
                SessionsElement sessions = Configuration.SessionGroup.Sessions;
                foreach (Session session in sessions.Session)
                {
                    Guid sessionUniqueId = new Guid(session.SessionUniqueId);
                    SessionWorker sessionWorker = m_sessionWorkers[sessionUniqueId];
                    InitializeSessionPipeline(sessionWorker, session, m_linkEngine);
                }

                m_errorManager.RegisterErrorsInConfigurationFile();
            }
            catch (Exception e)
            {
                m_errorManager.TryHandleException(new InitializationException(e.Message ?? string.Empty, e), m_conflictManager);
                throw;
            }
        }

        internal void CleanUpSyncOrchStatus()
        {
            SetSyncStatesToDefault();
            ObsoleteUnprocessedSyncCommand();
        }

        /// <summary>
        /// This method cleans up the sync command queue, such that we do not process
        /// commands that were enqued by previous session (which aborted or was killed).
        /// </summary>
        private void ObsoleteUnprocessedSyncCommand()
        {
            ISyncCommandQueue syncStateManager = SqlSyncStateManager.GetInstance();
            syncStateManager.ClearUpUnprocessedCommand(this.Configuration.SessionGroupUniqueId);
        }

        /// <summary>
        /// This method sets all the session (group) sync state machine to the default state.
        /// </summary>
        private void SetSyncStatesToDefault()
        {
            m_sessionGroupStateMachine.Reset();

            foreach (var sessionWorker in m_sessionWorkers.Values)
            {
                sessionWorker.ResetSyncState();
            }
        }

        private LinkEngine CreateLinkEngine(Configuration configuration)
        {
            return new LinkEngine(new Guid(configuration.SessionGroup.SessionGroupGUID), m_conflictManager, configuration.SessionGroup.Linking);
        }

        public ReadOnlyCollection<ConflictManager> GetConflictManagers(Guid scopeId, Guid sourceId)
        {
            List<ConflictManager> ret = new List<ConflictManager>();

            if (m_conflictManager != null && scopeId.Equals(m_conflictManager.ScopeId))
            {
                ret.Add(m_conflictManager);
            }
            else
            {
                foreach (ConflictManager cm in m_conflictManagers)
                {
                    if (cm.ScopeId.Equals(scopeId) && cm.SourceId.Equals(sourceId))
                    {
                        ret.Add(cm);
                    }
                }
            }

            return ret.AsReadOnly();
        }

        /// <summary>
        /// Get a list of all ConflictTypes registered with a migration source.
        /// </summary>
        /// <param name="sourceId">MigrationSource UniqueId</param>
        /// <returns></returns>
        public ReadOnlyCollection<ConflictType> GetConflictTypes(Guid sourceId)
        {
            Dictionary<Guid, ConflictType> conflictTypes = new Dictionary<Guid, ConflictType>();
            
            foreach (ConflictManager cm in m_conflictManagers)
            {
                if (cm.SourceId.Equals(sourceId))
                {
                    foreach (ConflictType conflictType in cm.RegisteredConflictTypes.Values)
                    {
                        conflictTypes[conflictType.ReferenceName] = conflictType;
                    }
                }
            }
            foreach (ConflictType conflictType in m_conflictManager.GetSourceSpecificConflictTypes(sourceId))
            {
                conflictTypes[conflictType.ReferenceName] = conflictType;
            }

            return conflictTypes.Values.ToList().AsReadOnly();
        }

        public bool IsAlive
        {
            get
            {
                bool retVal = false;
                foreach (var sessionWorker in m_sessionWorkers.Values)
                {
                    if (sessionWorker.Thread.IsAlive)
                    {
                        retVal = true;
                        break;
                    }
                }

                return retVal;
            }
        }

        #endregion

        #region private methods

        void PollSyncCommand()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);

                    if (!this.IsAlive)
                    {
                        // session group is no longer running, stop polling
                        if (m_sessionGroupStateMachine.TryTransit(PipelineSyncCommand.FINISH))
                        {
                            m_sessionGroupStateMachine.CommandTransitFinished(PipelineSyncCommand.FINISH);
                        }

                        break;
                    }

                    if (IsSessionInIntermittentState())
                    {
                        // do nothing if any child session is in intermittent state
                        continue;
                    }
                    else if (m_processingCommand.HasValue)
                    {
                        ProcessedCommand();
                    }

                    PipelineSyncCommand? activeCommand = m_syncCommandQueue.GetNextActiveCommand(this.Configuration.SessionGroupUniqueId);
                    if (!activeCommand.HasValue)
                    {
                        // do nothing if there is no active command to process
                        continue;
                    }

                    m_processingCommand = activeCommand;

                    switch (m_processingCommand.Value)
                    {
                        case PipelineSyncCommand.DEFAULT:
                            return;
                        case PipelineSyncCommand.FINISH:
                            Stop();
                            break;
                        case PipelineSyncCommand.PAUSE:
                            Pause();
                            break;
                        case PipelineSyncCommand.RESUME:
                            Resume();
                            break;
                        case PipelineSyncCommand.START:
                            // start should be invoked through the public interface of this class
                            ProcessedCommand();
                            break;
                        case PipelineSyncCommand.START_NEW_TRIP:
                            ProcessedCommand(); // this should never happen
                            break;
                        case PipelineSyncCommand.STOP:
                            Stop();
                            break;
                        case PipelineSyncCommand.STOP_CURRENT_TRIP:
                            ProcessedCommand(); // this should never happen
                            break;
                    }
                }
                catch (Exception ex)
                {
                    TraceManager.TraceException(ex);
                }
            }
        }

        private bool IsSessionInIntermittentState()
        {
            foreach (SessionWorker sessionWorker in m_sessionWorkers.Values)
            {
                if (SyncStateMachine.IsIntermittentState(sessionWorker.CurrentState))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSessionGroupRunning()
        {
            int runningStateVal = (int)BM.BusinessModelManager.SessionStateEnum.Running;
            var runningGroupIdQuery =
                from g in m_sessionGroupStatePollingContext.RTSessionGroupSet
                where g.State == runningStateVal && g.GroupUniqueId.Equals(this.Configuration.SessionGroupUniqueId)
                select g.Id;

            return runningGroupIdQuery.Count() > 0;
        }

        private bool IsAllChildSessionsInStoppedSyncState()
        {
            int syncStateStopped = (int)PipelineState.Stopped;
            var nonStoppedSessionIdQuery =
                from s in m_sessionGroupStatePollingContext.RTSessionSet
                where s.OrchestrationStatus != syncStateStopped
                   && s.SessionGroup.GroupUniqueId.Equals(this.Configuration.SessionGroupUniqueId)
                select s.Id;

            return nonStoppedSessionIdQuery.Count() == 0;
        }

        private void ProcessedCommand()
        {
            if (m_processingCommand.HasValue)
            {
                m_syncCommandQueue.MarkCommandProcessed(this.Configuration.SessionGroupUniqueId, m_processingCommand.Value);

                switch (m_processingCommand.Value)
                {
                    case PipelineSyncCommand.FINISH:
                    case PipelineSyncCommand.PAUSE:
                    case PipelineSyncCommand.RESUME:
                    case PipelineSyncCommand.START:
                    case PipelineSyncCommand.START_NEW_TRIP:
                    case PipelineSyncCommand.STOP:
                    case PipelineSyncCommand.STOP_CURRENT_TRIP:
                        m_sessionGroupStateMachine.CommandTransitFinished(m_processingCommand.Value);
                        break;
                    case PipelineSyncCommand.DEFAULT:
                    default:
                        break;
                }

                m_processingCommand = null;
            }
        }
        
        private void Start(long millisecsTimeout)
        {
            MarkSessionGroupRunning();

            if (millisecsTimeout != (long)Timeout.Infinite)
            {
                TraceManager.TraceInformation("Migration timeout value is set to {0} minutes", millisecsTimeout);
                TimerCallback timerCallback = new TimerCallback(StopSessionWorkers);
                Timer timer = new Timer(timerCallback, null, millisecsTimeout, Timeout.Infinite);
            }

            // Kickoff session workers
            foreach (SessionWorker sessionWorker in m_sessionWorkers.Values)
            {
                sessionWorker.Start();
            }

            // Kickoff sync orchestration command polling thread
            m_syncCommandPollingThread.Start();
        }

        internal void Stop()
        {
            if (m_sessionGroupStateMachine.TryTransit(PipelineSyncCommand.STOP))
            {
                foreach (SessionWorker worker in m_sessionWorkers.Values)
                {
                    Debug.Assert(worker != null, "worker == null");
                    Debug.Assert(worker.Thread != null, "worker.Thread == null");

                    if (worker.Thread.IsAlive)
                    {
                        TraceManager.TraceInformation("Session stop request to [{0}] ", worker.Thread.Name);
                        worker.Stop();
                    }
                }
            }
        }


        private void Pause()
        {
            if (m_sessionGroupStateMachine.TryTransit(PipelineSyncCommand.PAUSE))
            {
                foreach (SessionWorker worker in m_sessionWorkers.Values)
                {
                    Debug.Assert(worker != null, "worker == null");
                    Debug.Assert(worker.Thread != null, "worker.Thread == null");

                    if (worker.Thread.IsAlive)
                    {
                        TraceManager.TraceInformation("Session pause request to [{0}] ", worker.Thread.Name);
                        worker.Pause();
                    }
                }
            }
        }

        private void Resume()
        {
            if (m_sessionGroupStateMachine.TryTransit(PipelineSyncCommand.RESUME))
            {
                foreach (SessionWorker worker in m_sessionWorkers.Values)
                {
                    Debug.Assert(worker != null, "worker == null");
                    Debug.Assert(worker.Thread != null, "worker.Thread == null");

                    if (worker.Thread.IsAlive)
                    {
                        TraceManager.TraceInformation("Session resume request to [{0}] ", worker.Thread.Name);
                        worker.Resume();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="DBSchemaValidationException" />
        /// <exception cref="InitializationException" />
        private void ValidateDBSchemaVersion()
        {
            if (string.IsNullOrEmpty(GlobalConfiguration.TfsMigrationDbConnectionString))
            {
                throw new InitializationException(MigrationToolkitResources.SQLMissingConnectionString);
            }

            // throws MigrationException when the ReferenceName of the TFS Migration DB
            // does not match the one declared by this version of the platform
            SqlUtil.ValidateDBSchemaVersion(GlobalConfiguration.TfsMigrationDbConnectionString);
        }

        private long TimeoutMinutesToMillisecs(
            int minutesTimeout)
        {
            if (minutesTimeout <= 0)
            {
                return Timeout.Infinite;
            }

            return minutesTimeout * 60000;
        }

        private void MarkSessionGroupRunning()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {                  
                var sessionGroupQuery = context.RTSessionGroupSet.Where
                    (sg => sg.GroupUniqueId.Equals(this.Configuration.SessionGroupUniqueId));
                Debug.Assert(sessionGroupQuery.Count() == 1);
                sessionGroupQuery.First().State = (int)BusinessModelManager.SessionStateEnum.Running;
                sessionGroupQuery.First().OrchestrationStatus = (int)PipelineState.Running;
                context.TrySaveChanges();
            }

            // trigger the session group state machine to transit to "START" status
            if (m_sessionGroupStateMachine.TryTransit(PipelineSyncCommand.START))
            {
                m_sessionGroupStateMachine.CommandTransitFinished(PipelineSyncCommand.START);
            }
            else
            {
                throw new MigrationException();
            }
        }

        private void StopSessionWorkers(object state)
        {
            TraceManager.TraceInformation("Migration timeout expired");

            if (m_sessionGroupStateMachine.TryTransit(PipelineSyncCommand.STOP))
            {
                Stop();

                try
                {
                    // Wait for session worker threads to die gracefully
                    Thread.Sleep(MinutesSessionStopWaitTimeout * 60000);
                    TraceManager.TraceInformation("SyncOrchestrator waited {0} minutes for session workers to die", MinutesSessionStopWaitTimeout);

                    // Kill any alive threads forcefully
                    foreach (SessionWorker worker in m_sessionWorkers.Values)
                    {
                        if (worker.Thread.IsAlive)
                        {
                            TraceManager.TraceInformation("Killing thread [{0}] forcefully ", worker.Thread.Name);
                            worker.Thread.Abort();
                        }
                    }

                    m_sessionGroupStateMachine.CommandTransitFinished(PipelineSyncCommand.STOP);
                }
                catch (Exception e)
                {
                    TraceManager.TraceInformation("Failed to kill thread(s) {0}", e.Message);
                    TraceManager.TraceException(e);
                }
            }
        }

        private void CreateSessionGroupRunEntry(BM.Configuration configuration)
        {
            Guid configUniqueId = new Guid(configuration.UniqueId);

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionGroupConfig = context.RTSessionGroupConfigSet.Where
                    (c => c.UniqueId.Equals(configUniqueId)).First();
                Debug.Assert(null != sessionGroupConfig, "Error: cannot find the session group configuration");

                RTSessionGroupRun groupRun = RTSessionGroupRun.CreateRTSessionGroupRun(0, DateTime.Now);
                groupRun.Config = sessionGroupConfig;
                context.AddToRTSessionGroupRunSet(groupRun);

                RTConflictCollection conflictCollection = RTConflictCollection.CreateRTConflictCollection(0);
                groupRun.ConflictCollection = conflictCollection;

                context.TrySaveChanges();

                m_conflictManager.ConflictCollectionInternalId = groupRun.ConflictCollection.Id;
                m_sessoinGroupRunId = groupRun.Id;            
            }
        }

        private void InitializeGlobalConflictManager(BM.Configuration configuration)
        {              
            m_conflictManager = new ConflictManager(Constants.FrameworkSourceId);
            m_conflictManagers.Add(m_conflictManager);
            m_conflictManager.ScopeId = configuration.SessionGroupUniqueId;
            m_conflictManager.ConflictUnresolvedEvent += new ConflictManager.ConflictUnresolvedEventHandler(ConflictUnresolvedEventHandler);
        }

        void ConflictUnresolvedEventHandler(object sender, ConflictUnresolvedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Unresolved conflict: ");
            sb.AppendLine(string.Format(e.IsGlobalConflict ? "  Session Group: {0}" : "  Session: {0}", e.ScopeId.ToString()));
            if (!e.IsGlobalConflict)
            {
                sb.AppendLine(string.Format("  Source: {0}", e.SourceId.ToString()));
            }
            sb.Append("  Message: ");
            sb.AppendLine(e.Message);
            sb.Append("  Conflict Type: ");
            sb.AppendLine(e.UnresolvedConflict.ConflictType.FriendlyName);
            sb.Append("  Conflict Type Reference Name: ");
            sb.AppendLine(e.UnresolvedConflict.ConflictType.ReferenceName.ToString());
            sb.Append("  Conflict Details: ");
            sb.AppendLine(e.UnresolvedConflict.ConflictDetails);
            sb.AppendLine();
            TraceManager.TraceInformation(sb.ToString());

            Thread conflictedThread = e.ConflictedThread;;

            ConflictsSyncOrchOptions conflictUnresolvedSyncOrchOption = m_conflictSyncOption;

            if (e.SyncOrchestrationOption.HasValue)
            {
                conflictUnresolvedSyncOrchOption = e.SyncOrchestrationOption.Value;
            }

            switch (conflictUnresolvedSyncOrchOption)
            {
                case ConflictsSyncOrchOptions.Continue:
                    break;
                case ConflictsSyncOrchOptions.StopAllSessionsCurrentTrip:
                    TraceManager.TraceInformation("Stopping current trip for all sessions");
                    foreach (SessionWorker worker in m_sessionWorkers.Values)
                    {
                        TraceManager.TraceInformation("Stopping current trip for session: {0}", worker.SessionId.ToString());
                        worker.StopCurrentTrip();
                    }
                    break;
                case ConflictsSyncOrchOptions.StopConflictedSessionCurrentTrip:
                    foreach (SessionWorker worker in m_sessionWorkers.Values)
                    {
                        if (worker.Thread == conflictedThread)
                        {
                            TraceManager.TraceInformation("Stopping current trip for session: {0}", worker.SessionId.ToString());
                            worker.StopCurrentTrip();
                            break;
                        }
                    }
                    break;
                case ConflictsSyncOrchOptions.StopConflictedSession:
                    foreach (SessionWorker worker in m_sessionWorkers.Values)
                    {
                        if (worker.Thread == conflictedThread)
                        {
                            // NOTE: avoid using the dangerous thread abort for now
                            // force the conflicted thread to stop
                            // at where it is at now (in conflict manager)
                            //worker.Thread.Abort();
                            //break;
                            TraceManager.TraceInformation("Stopping conflicted session: {0}", worker.SessionId.ToString());
                            worker.Stop();
                            break;
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }      
  
        private void FillSessionConfigInfo(BM.Session session)
        {
            Guid leftSourceId = new Guid(session.LeftMigrationSourceUniqueId);
            BM.MigrationSource leftSource = Configuration.GetMigrationSource(leftSourceId);
            if (null == leftSource)
            {
                throw new InvalidOperationException("Missing migration source");
            }

            Guid rightSourceId = new Guid(session.RightMigrationSourceUniqueId);
            BM.MigrationSource rightSource = Configuration.GetMigrationSource(rightSourceId);
            if (null == rightSource)
            {
                throw new InvalidOperationException("Missing migration source");
            }

            if (!session.MigrationSources.ContainsKey(leftSourceId))
            {
                session.MigrationSources.Add(leftSourceId, leftSource);
            }
            if (!session.MigrationSources.ContainsKey(rightSourceId))
            {
                session.MigrationSources.Add(rightSourceId, rightSource);
            }
        }


        private void InitializeSessionPipeline(
            SessionWorker sessionWorker, 
            BM.Session config, 
            LinkEngine linkEngine)
        {
            // initialize the session worker
            // 1. creating the session worker thread
            // 2. finishing the heavy initialization, e.g. server connection, etc.
            int sessionRunId = CreateSessionRunEntry(config);
            sessionWorker.Initialize(sessionRunId);
        }

        // Creates a session worker thread with the specified session configuration
        private SessionWorker ConstructSessionPipeline(BM.Session config, int sessionIndex, LinkEngine linkEngine)
        {
            Guid leftMigrationSourceId = new Guid(config.LeftMigrationSourceUniqueId);
            Guid rightMigrationSourceId = new Guid(config.RightMigrationSourceUniqueId);

            // Find providers for this session
            ProviderHandler leftProviderHandler;
            ProviderHandler rightProviderHandler;
            if (!ProviderHandlers.TryGetValue(leftMigrationSourceId, out leftProviderHandler))
            {
                throw new InitializationException(MigrationToolkitResources.ErrorProviderNotFound, config.MigrationSources[leftMigrationSourceId].FriendlyName);
            }

            if (!ProviderHandlers.TryGetValue(rightMigrationSourceId, out rightProviderHandler))
            {
                throw new InitializationException(MigrationToolkitResources.ErrorProviderNotFound, config.MigrationSources[rightMigrationSourceId].FriendlyName);
            }

            // TODO: remove these session type specific logic from the pipeline construction
            ITranslationService translationService = null;
            IConflictAnalysisService conflictAnalysisService = null;
            switch (config.SessionType)
            {
                case SessionTypeEnum.VersionControl:
                    translationService = new VCTranslationService(
                                            config,
                                            leftMigrationSourceId,
                                            rightMigrationSourceId,
                                            leftProviderHandler.Provider,
                                            rightProviderHandler.Provider,
                                            m_userIdLookupService);
                    conflictAnalysisService = new VCBasicConflictAnalysisService();
                    break;
                case SessionTypeEnum.WorkItemTracking:
                    translationService = new WITTranslationService(config, m_userIdLookupService);
                    conflictAnalysisService = new WITBasicConflictAnalysisService();
                    break;
                default:
                    Debug.Assert(false, "Unsupported Session Type");
                    break;
            }

            // Create engines
            AnalysisEngine analysisEngine = CreateAnalysisEngine(config, 
                                                                 leftProviderHandler, rightProviderHandler, 
                                                                 translationService, conflictAnalysisService);
            if (analysisEngine == null)
            {
                throw new InitializationException(String.Format("Loading analysis engine failed for the session {0}", config.SessionUniqueId));
            }
            MigrationEngine migrationEngine = CreateMigrationEngine(config, 
                                                                    leftProviderHandler, rightProviderHandler, 
                                                                    translationService);
            if (migrationEngine == null)
            {
                throw new InitializationException(String.Format("Loading migration engine failed for the session {0}", config.SessionUniqueId));
            }

            // register link service container to link engine
            var sessionId = new Guid(config.SessionUniqueId);
            ServiceContainer leftContainer = CreateLinkServiceContainer(config, linkEngine, sessionId, leftMigrationSourceId, leftProviderHandler, translationService);
            leftContainer.AddService(typeof(IAddinManagementService), AddinManagementService);
            leftContainer.AddService(typeof(ICredentialManagementService), m_credentialManagementService);
            leftContainer.AddService(typeof(ErrorManager), linkEngine.ErrorManager);
            ServiceContainer rightContainer = CreateLinkServiceContainer(config, linkEngine, sessionId, rightMigrationSourceId, rightProviderHandler, translationService);
            rightContainer.AddService(typeof(IAddinManagementService), AddinManagementService);
            rightContainer.AddService(typeof(ICredentialManagementService), m_credentialManagementService);
            rightContainer.AddService(typeof(ErrorManager), linkEngine.ErrorManager);

            var linkServiceContainers = new Dictionary<Guid, ServiceContainer>(2);
            linkServiceContainers.Add(leftMigrationSourceId, leftContainer);
            linkServiceContainers.Add(rightMigrationSourceId, rightContainer);
            linkEngine.AddSessionServiceContainers(sessionId, linkServiceContainers, config.SessionType);

            var leftLinkProvider = leftContainer.GetService(typeof (ILinkProvider)) as ILinkProvider;
            var rightLinkProvider = rightContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
            if (null != leftLinkProvider && null != rightLinkProvider)
            {
                leftLinkProvider.SupportedChangeActionsOther = rightLinkProvider.SupportedChangeActions;
                rightLinkProvider.SupportedChangeActionsOther = leftLinkProvider.SupportedChangeActions;
            }

            // initialize all the link providers that have been registered to the link service for this session
            if (linkEngine.AllSessionServiceContainerPairs.ContainsKey(sessionId))
            {
                var sessionServiceContainer = linkEngine.AllSessionServiceContainerPairs[sessionId];

                Debug.Assert(sessionServiceContainer.Count == 2, "sessionServiceContainer.Count != 2");

                ILinkProvider[] linkProviders = new ILinkProvider[2];
                for (int i = 0; i < 2; ++i)
                {
                    var serviceContainer = sessionServiceContainer.Values.ElementAt(i);
                    linkProviders[i] = serviceContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
                    if (null != linkProviders[i])
                    {
                        linkProviders[i].Initialize(serviceContainer);
                        linkProviders[i].RegisterSupportedLinkTypes();
                    }
                }

                if (linkProviders[0] != null && linkProviders[1] != null)
                {
                    linkProviders[0].SupportedLinkTypeReferenceNamesOther = linkProviders[1].SupportedLinkTypes.Keys;
                    linkProviders[1].SupportedLinkTypeReferenceNamesOther = linkProviders[0].SupportedLinkTypes.Keys;
                }
            }

            // Creates a session worker thread
            string sessionName = config.SessionType.ToString();
            SessionWorker newSessionWorker = new SessionWorker(
                sessionId,
                new ManualResetEvent(false),
                analysisEngine,
                migrationEngine,
                linkEngine,
                leftMigrationSourceId,
                rightMigrationSourceId,
                Configuration.SessionGroup.WorkFlowType,
                m_syncIntervalInSeconds,
                sessionName);

            Debug.Assert(!m_sessionWorkers.ContainsKey(sessionId));
            m_sessionWorkers.Add(sessionId, newSessionWorker);
            return m_sessionWorkers[sessionId];
        }

        private ServiceContainer CreateLinkServiceContainer(
            BM.Session sessionConfig, 
            LinkEngine linkEngine, 
            Guid sessionId, 
            Guid sourceId, 
            ProviderHandler handler,
            ITranslationService translationService)
        {
            var linkProvider = handler.Provider.GetService(typeof (ILinkProvider)) as ILinkProvider;

            var serviceContainer = new ServiceContainer();

            serviceContainer.AddService(typeof(ITranslationService), translationService);

            serviceContainer.AddService(typeof(ConfigurationService), new ConfigurationService(Configuration, sessionConfig, sourceId));

            // register link-specific services and give the link provider a chance to initialize itself
            if (null != linkProvider)
            {
                serviceContainer.AddService(typeof (ILinkProvider), linkProvider);
                LinkService linkService = new LinkService(linkEngine, sessionId, sourceId);
                serviceContainer.AddService(typeof(LinkService), linkService);
                serviceContainer.AddService(typeof(ILinkTranslationService), linkService);
                serviceContainer.AddService(typeof (ConflictManager), linkEngine.ConflictManager);

                // moving linkProvider.Initialize(...) to Phase2 InitializeSessionPipeline
                // linkProvider.Initialize(serviceContainer);
                linkProvider.RegisterSupportedLinkOperations();
                linkProvider.RegisterConflictTypes(linkEngine.ConflictManager, sourceId);
            }

            return serviceContainer;
        }

        private int CreateSessionRunEntry(BM.Session config)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Guid configUniqueId = new Guid(Configuration.UniqueId);
                var sessionGroupConfig = context.RTSessionGroupConfigSet.Where
                    (c => c.UniqueId.Equals(configUniqueId)).First();
                Debug.Assert(null != sessionGroupConfig, "Error: cannot find the session group configuration");

                Guid sessionUniqueId = new Guid(config.SessionUniqueId);
                RTSessionConfig sessionConfig = 
                    (from sc in context.RTSessionConfigSet
                    where sc.SessionGroupConfig.UniqueId.Equals(configUniqueId) &&  sc.SessionUniqueId.Equals(sessionUniqueId)
                    select sc).First<RTSessionConfig>();
                Debug.Assert(null != sessionConfig, "Error: cannot find the session configuration");

                RTSessionRun sessionRun = RTSessionRun.CreateRTSessionRun(0, false);
                context.AddToRTSessionRunSet(sessionRun);

                RTConflictCollection perSessionRunConflicts = RTConflictCollection.CreateRTConflictCollection(0);
                sessionRun.ConflictCollection = perSessionRunConflicts;

                sessionRun.Config = sessionConfig;
                sessionRun.SessionGroupRun =
                    (from sgr in context.RTSessionGroupRunSet
                     where sgr.Id == m_sessoinGroupRunId
                     select sgr).First<RTSessionGroupRun>();
                sessionRun.State = 1;

                context.TrySaveChanges();
                return sessionRun.Id;
            }
        }

        private void ValidateAndSaveConflictRegistration(
            ServiceContainer serviceContainer,
            ProviderHandler providerHandler,
            bool validateToolkitRegInfo)
        {
            ConflictManager conflictManager = serviceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
            if (providerHandler.VersionChanged)
            {
                conflictManager.ValidateAndSaveProviderConflictRegistration(providerHandler.InternalId);
            }
            if (validateToolkitRegInfo)
            {
                conflictManager.ValidateAndSaveProviderConflictRegistration(m_toolkitProviderInternalId);
            }
        }

        private bool needToDisableTargetAnalysis(BM.Session config)
        {
            foreach (BM.VC.Setting setting in config.VCCustomSetting.Settings.Setting)
            {
                if (string.Equals(
                    setting.SettingKey, MigrationToolkitResources.VCSetting_DisableTargetAnalysis, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private AnalysisEngine CreateAnalysisEngine(
            BM.Session sessionConfig, 
            ProviderHandler leftProviderHandler, 
            ProviderHandler rightProviderHandler, 
            ITranslationService translationService,
            IConflictAnalysisService conflictAnalysisService)
        {
            AnalysisEngine analysisEngine = null;

            Guid leftMigrationSourceId = new Guid(sessionConfig.LeftMigrationSourceUniqueId);
            Guid rightMigrationSourceId = new Guid(sessionConfig.RightMigrationSourceUniqueId);

            IAnalysisProvider leftAnalysisProvider = (IAnalysisProvider)leftProviderHandler.Provider.GetService(typeof(IAnalysisProvider));
            IAnalysisProvider rightAnalysisProvider = (IAnalysisProvider)rightProviderHandler.Provider.GetService(typeof(IAnalysisProvider));

            if (leftAnalysisProvider == null)
            {
                throw new InitializationException(String.Format("AnalysisProvider {0} not found",
                    MigrationSources[leftMigrationSourceId].ProviderReferenceName));
            }

            if (rightAnalysisProvider == null)
            {
                throw new InitializationException(String.Format("AnalysisProvider {0} not found",
                    MigrationSources[rightMigrationSourceId].ProviderReferenceName));
            }

            var runtimeSession = new RuntimeSession(sessionConfig, Configuration);
            runtimeSession.RegisterAddinManagementService(AddinManagementService);
            analysisEngine = new AnalysisEngine(runtimeSession, Configuration, AddinManagementService);

            analysisEngine.ErrorManager = m_errorManager;
            analysisEngine.DisableTargetAnalysis = needToDisableTargetAnalysis(sessionConfig);

            analysisEngine.RegisterAnalysisProvider(leftMigrationSourceId, sessionConfig, leftAnalysisProvider, leftProviderHandler.Provider);
            ValidateAndSaveConflictRegistration(analysisEngine[leftMigrationSourceId], leftProviderHandler, m_toolkitVersionChanged);

            analysisEngine.RegisterAnalysisProvider(rightMigrationSourceId, sessionConfig, rightAnalysisProvider, rightProviderHandler.Provider);
            ValidateAndSaveConflictRegistration(analysisEngine[rightMigrationSourceId], rightProviderHandler, m_toolkitVersionChanged);

            analysisEngine.TranslationService = translationService;
            analysisEngine.BasicConflictAnalysisService = conflictAnalysisService;

            var serviceContainerPairCpy = new Dictionary<Guid, ServiceContainer>(2);
            serviceContainerPairCpy.Add(leftMigrationSourceId, analysisEngine[leftMigrationSourceId]);
            serviceContainerPairCpy.Add(rightMigrationSourceId, analysisEngine[rightMigrationSourceId]);
            analysisEngine.DeltaTableMaintenanceService = new BasicDeltaTableMaintenanceService(serviceContainerPairCpy);

            analysisEngine.InitializeHandlers(
                leftMigrationSourceId, 
                rightAnalysisProvider.SupportedChangeActions.Keys, 
                rightAnalysisProvider.SupportedContentTypes);

            analysisEngine.InitializeHandlers(
                rightMigrationSourceId,
                rightAnalysisProvider.SupportedChangeActions.Keys,
                rightAnalysisProvider.SupportedContentTypes);

            Guid sessionUniqueId = new Guid(sessionConfig.SessionUniqueId);

            Guid leftSourceId = new Guid(sessionConfig.LeftMigrationSourceUniqueId);
            RegisterSessionUnresolvedConflictEvent(analysisEngine[leftSourceId], sessionUniqueId, leftSourceId);
            analysisEngine[leftSourceId].AddService(typeof(ErrorManager), m_errorManager);
            analysisEngine[leftSourceId].AddService(typeof(ICredentialManagementService), m_credentialManagementService);

            Guid rightSourceId = new Guid(sessionConfig.RightMigrationSourceUniqueId);
            RegisterSessionUnresolvedConflictEvent(analysisEngine[rightSourceId], sessionUniqueId, rightSourceId);
            analysisEngine[rightSourceId].AddService(typeof(ErrorManager), m_errorManager);
            analysisEngine[rightSourceId].AddService(typeof(ICredentialManagementService), m_credentialManagementService);

            if (sessionConfig.SessionType == BM.SessionTypeEnum.WorkItemTracking)
            {
                analysisEngine.StopMigrationEngineOnBasicConflict = false;
            }
            else
            {
                analysisEngine.StopMigrationEngineOnBasicConflict = true;
            }

            return analysisEngine;
        }

        private void RegisterSessionUnresolvedConflictEvent(
            ServiceContainer serviceContainer, 
            Guid sessionUniqueId,
            Guid sourceId)
        {
            Debug.Assert(serviceContainer != null, string.Format(
                "Service container for migration source {0} is not constructed in analysis engine for session {1}",
                sourceId.ToString(), sessionUniqueId.ToString()));
            if (serviceContainer != null)
            {
                ConflictManager conflictManager = serviceContainer.GetService(typeof(ConflictManager)) as ConflictManager;
                if (conflictManager != null)
                {
                    conflictManager.ConflictUnresolvedEvent += ConflictUnresolvedEventHandler;
                    if (!m_conflictManagers.Contains(conflictManager))
                    {
                        m_conflictManagers.Add(conflictManager);
                    }
                }
            }
        }

        private MigrationEngine CreateMigrationEngine(
            BM.Session sessionConfig, 
            ProviderHandler leftProviderHandler, 
            ProviderHandler rightProviderHandler,
            ITranslationService translationService)
        {
            MigrationEngine migrationEngine = null;

            Guid leftMigrationSourceId = new Guid(sessionConfig.LeftMigrationSourceUniqueId);
            Guid rightMigrationSourceId = new Guid(sessionConfig.RightMigrationSourceUniqueId);

            IMigrationProvider leftMigrationProvider = (IMigrationProvider)leftProviderHandler.Provider.GetService(typeof(IMigrationProvider));
            IMigrationProvider rightMigrationProvider = (IMigrationProvider)rightProviderHandler.Provider.GetService(typeof(IMigrationProvider));

            if (leftMigrationProvider == null)
            {
                throw new InitializationException(String.Format("MigrationProvider {0} not found",
                    MigrationSources[leftMigrationSourceId].ProviderReferenceName));
            }

            if (rightMigrationProvider == null)
            {
                throw new InitializationException(String.Format("MigrationProvider {0} not found",
                    MigrationSources[rightMigrationSourceId].ProviderReferenceName));
            }

            var runtimeSession = new RuntimeSession(sessionConfig, Configuration);
            runtimeSession.RegisterAddinManagementService(AddinManagementService);
            migrationEngine = new MigrationEngine(runtimeSession, Configuration, AddinManagementService);
            migrationEngine.RegisterMigrationProvider(leftMigrationSourceId, leftMigrationProvider);
            migrationEngine.RegisterMigrationProvider(rightMigrationSourceId, rightMigrationProvider);

            migrationEngine.ErrorManager = this.m_errorManager;

            if (sessionConfig.SessionType == BM.SessionTypeEnum.WorkItemTracking)
            {
                migrationEngine.StopMigrationEngineOnBasicConflict = false;
            }
            else
            {
                migrationEngine.StopMigrationEngineOnBasicConflict = true;
            }

            Guid sessionUniqueId = new Guid(sessionConfig.SessionUniqueId);

            Guid leftSourceId = new Guid(sessionConfig.LeftMigrationSourceUniqueId);
            RegisterSessionUnresolvedConflictEvent(migrationEngine[leftSourceId], sessionUniqueId, leftSourceId);
            migrationEngine[leftSourceId].AddService(typeof(ErrorManager), m_errorManager);
            migrationEngine[leftSourceId].AddService(typeof(ICredentialManagementService), m_credentialManagementService);

            Guid rightSourceId = new Guid(sessionConfig.RightMigrationSourceUniqueId);
            RegisterSessionUnresolvedConflictEvent(migrationEngine[rightSourceId], sessionUniqueId, rightSourceId);
            migrationEngine[rightSourceId].AddService(typeof(ErrorManager), m_errorManager);
            migrationEngine[rightSourceId].AddService(typeof(ICredentialManagementService), m_credentialManagementService);

            migrationEngine.TranslationService = translationService;

            return migrationEngine;
        }

        #endregion
    }
}
