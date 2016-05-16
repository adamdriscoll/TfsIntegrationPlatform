// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// A session instance for migration.
    /// </summary>
    [XmlType("Session")]
    public class RuntimeSession // ToDo implement IEventLogger 
    {
        Dictionary<Guid, ServiceContainer> m_serviceContainers;
        Dictionary<Guid, ChangeGroupManager> m_changeGroupManagers; // ToDo syncorchestor need to initialize changegroupmanager, populate targetIds
        //ToDo get sourceId collection from syncorchestor
        Session m_session;
        Configuration m_globalConfiguration;
        int m_internalSessionRunId;

        internal Session Configuration
        {
            get 
            { 
                return m_session; 
            }
        }

        internal Dictionary<Guid, ServiceContainer> ServiceContainers
        {
            get
            {
                return m_serviceContainers;
            }
        }

        internal Dictionary<Guid, ChangeGroupManager> ChangeGroupManagers
        {
            get
            {
                return m_changeGroupManagers;
            }
        }

        /// <summary>
        /// Constructor of the session object
        /// </summary>
        public RuntimeSession(Session session, Configuration globalConfiguration)
        {
            m_session = session;
            m_globalConfiguration = globalConfiguration;

            m_serviceContainers = new Dictionary<Guid, ServiceContainer>(2);
            m_serviceContainers.Add(new Guid(m_session.LeftMigrationSourceUniqueId), new ServiceContainer());
            m_serviceContainers.Add(new Guid(m_session.RightMigrationSourceUniqueId), new ServiceContainer());

            CreateServices();
        }

        internal void RegisterAddinManagementService(IAddinManagementService service)
        {
            foreach (var container in m_serviceContainers.Values)
            {
                container.AddService(typeof(IAddinManagementService), service);
            }
        }

        /// <summary>
        /// Initialize method of the session object
        /// </summary>
        internal void Initialize(int sessionRunId)
        {
            m_internalSessionRunId = sessionRunId;  
          
            // fully initialize the change group managers
            foreach (var serviceContainer in m_serviceContainers.Values)
            {
                ChangeGroupService changeGroupService = serviceContainer.GetService(typeof(ChangeGroupService)) as ChangeGroupService;
                changeGroupService.Initialize(sessionRunId);
            }
        }

        internal int InternalSessionRunId
        {
            get
            {
                return m_internalSessionRunId;
            }
        }

        /// <summary>
        /// Initialize configuration services and add it to service container
        /// </summary>
        private void CreateServices()
        {
            Debug.Assert(m_serviceContainers.Count == 2);
            ChangeGroupManager changeGroupManager = null;
            foreach (KeyValuePair<Guid, ServiceContainer> container in m_serviceContainers)
            {
                ConfigurationService configurationService = new ConfigurationService(m_globalConfiguration, Configuration, container.Key);
                container.Value.AddService(typeof(ConfigurationService), configurationService);

                var sqlChangeGroupManager = new SqlChangeGroupManager(m_session, container.Key);
                ChangeGroupService changeGroupService= new ChangeGroupService(sqlChangeGroupManager);
                sqlChangeGroupManager.ChangeGroupService = changeGroupService;
                container.Value.AddService(typeof(ChangeGroupService), changeGroupService);

                if (null == changeGroupManager)
                {
                    changeGroupManager = sqlChangeGroupManager;
                }
                else
                {
                    changeGroupManager.OtherSideChangeGroupManager = sqlChangeGroupManager;
                    sqlChangeGroupManager.OtherSideChangeGroupManager = changeGroupManager;
                }
            }
        }

        /// <summary>
        /// Throw exception if the current session is aborted
        /// </summary>
        public virtual void ThrowIfAborted()
        {
            throw new NotImplementedException();
            /*if (IsAborted)
            {
                throw new MigrationAbortedException();
            }
             * */
        }

        /*bool m_isAborted;
        bool m_isRunning;
        bool m_isComplete;
        bool m_isStopRequested;
        bool m_isAbortRequested;
        ManualResetEvent m_abortEvent;                  // Thread stop event
        ManualResetEvent m_stopEvent;                   // Thread abort event
        ManualResetEvent m_completeCurrentSyncEvent;   // Current Synchronization complete event
        WaitHandle[] m_completeSessionEvents;
        WaitHandle[] m_completeCurrentSyncEvents;
        RunningMode m_runningMode = RunningMode.NotSet;
        int m_sleepTime = -1;
        object m_stateLocker = new object();
        List<IMigrationSessionEventSink> m_cleanup = new List<IMigrationSessionEventSink>();
        string m_id = string.Empty;
        string m_providerID;
        Thread m_serviceThread;

        TfsVersionControlTargetEndpoint m_target;
        IVersionControlEndpoint m_source;

        /// <summary>
        /// Constructor.
        /// </summary>
        public VersionControlSession()
        {
            LogHelper = new LogHelper(this);
        }

        /// <summary>
        /// Gets the session ID.
        /// </summary>
        [XmlAttribute("id")]
        public string Id
        {
            get
            {
                return m_id;
            }
            set
            {
                m_id = value;
            }
        }

        /// <summary>
        /// Gets the provider ID.
        /// </summary>
        [XmlAttribute("provider")]
        public string ProviderId
        {
            get
            {
                return m_providerID;
            }
            set
            {
                m_providerID = value;
            }
        }


        /// <summary>
        /// Gets the TFS server end point.
        /// </summary>
        public virtual TfsVersionControlTargetEndpoint Target
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
        /// Gets the source system end point.
        /// </summary>
        public virtual IVersionControlEndpoint Source
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

        #region IMigrationSession events
        /// <summary>
        /// Fired when the session is about to start.  This is a cancelable event.
        /// </summary>
        public event EventHandler<MigrationSessionEventArgs> SessionStart;

        /// <summary>
        /// Fired after a session is aborted.  This is a non-cancelable event.
        /// </summary>
        public event EventHandler<MigrationSessionEventArgs> SessionAborted;

        /// <summary>
        /// Fired after the session completes.  This is a non-cancelable event.
        /// </summary>
        public event EventHandler<MigrationSessionEventArgs> SessionComplete;

        /// <summary>
        /// Fired when an error occurs within a session.  This is a non-cancelable event.
        /// </summary>
        public event EventHandler<MigrationSessionEventArgs> SessionError;

        /// <summary>
        /// Custom logging event.
        /// </summary>
        public event EventHandler<CustomMigrationEventArgs> CustomEvent;
        #endregion

        #region IVersionControlSession events
        /// <summary>
        /// Fired before analysis begins.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalysisStarting;

        /// <summary>
        /// Fired when analysis encounters an error.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalysisError;

        /// <summary>
        /// Fired when analysis encounters an warning condition
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalysisWarning;

        /// <summary>
        /// Fired when an analysis session is aborted.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalysisAborted;

        /// <summary>
        /// Fired when an analysis session is completed.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalysisComplete;

        /// <summary>
        /// Fired when an individual change analysis is about to begin.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalyzingChangeStarting;

        /// <summary>
        /// Fired when an individual change analysis completes
        /// </summary>
        public event EventHandler<VersionControlEventArgs> AnalyzingChangeComplete;

        /// <summary>
        /// Fired when migration is about to begin
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationStarting;

        /// <summary>
        /// Fired when a migration session encounters an error
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationError;

        /// <summary>
        /// Fired when a migration session is aborted.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationAborted;

        /// <summary>
        /// Fired when a migration session completes.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationComplete;

        /// <summary>
        /// Fired when an individual migration action is about to begin
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigratingChangeStarting;

        /// <summary>
        /// Fired when an individual migration action completes
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigratingChangeComplete;

        /// <summary>
        /// Fired when an item download is about to being.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationDownloadStarted;

        /// <summary>
        /// Fired when an item download has completed.
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationDownloadCompleted;

        /// <summary>
        /// Fired when a migration warning occurs
        /// </summary>
        public event EventHandler<VersionControlEventArgs> MigrationWarning;

        #endregion

        #region Session events
        private void FireEvent(EventHandler<MigrationSessionEventArgs> eventToFire, MigrationSessionEventArgs eventArgs)
        {
            if (eventToFire != null)
            {
                eventToFire(this, eventArgs);
            }
        }

        /// <summary>
        /// Fires the session error event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnSessionError(MigrationSessionEventArgs eventArgs)
        {
            FireEvent(SessionError, eventArgs);
        }

        /// <summary>
        /// Fires the session start event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnSessionStart(MigrationSessionEventArgs eventArgs)
        {
            FireEvent(SessionStart, eventArgs);
        }

        /// <summary>
        /// Set session state, cleanup and  fires the session aborted event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnSessionAborted(MigrationSessionEventArgs eventArgs)
        {
            lock (m_stateLocker)
            {
                m_isAborted = true;
                m_isRunning = false;
                m_isComplete = false;
            }
            CleanupSession();
            FireEvent(SessionAborted, eventArgs);
        }

        /// <summary>
        /// Set session state, cleanup and fres the session complete event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnSessionComplete(MigrationSessionEventArgs eventArgs)
        {
            lock (m_stateLocker)
            {
                m_isAborted = false;
                m_isRunning = false;
                m_isComplete = true;
            }
            CleanupSession();
            FireEvent(SessionComplete, eventArgs);
        }
        #endregion

        #region Version control event methods
        private void FireVCEvent(EventHandler<VersionControlEventArgs> eventToFire, VersionControlEventArgs eventArgs)
        {
            if (eventToFire != null)
            {
                TraceManager.TraceInformation(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", m_id, eventArgs.Description));

                if (eventArgs.Exception != null)
                {
                    TraceManager.TraceException(eventArgs.Exception);
                }
            }

            if (eventToFire != null)
            {
                eventToFire(this, eventArgs);
            }
        }

        /// <summary>
        /// Fires the before analysis event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnBeforeAnalysis(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalysisStarting, eventArgs);
        }

        /// <summary>
        /// Fires the analysis error event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnAnalysisError(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalysisError, eventArgs);
        }

        /// <summary>
        /// Fires the analysis warning event.
        /// </summary>
        /// <param name="eventArgs">warning event args</param>
        public virtual void OnAnalysisWarning(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalysisWarning, eventArgs);
        }

        /// <summary>
        /// Fires the analysis aborted event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnAnalysisAborted(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalysisAborted, eventArgs);
        }

        /// <summary>
        /// Fires the analysis complete event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnAnalysisComplete(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalysisComplete, eventArgs);
        }

        /// <summary>
        /// Fires the individual change analysis pre event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnAnalyzingChangeStarting(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalyzingChangeStarting, eventArgs);
        }

        /// <summary>
        /// Fires the individual change analysis post event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnAnalyzingChangeComplete(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(AnalyzingChangeComplete, eventArgs);
        }

        /// <summary>
        /// Fires the migration pre event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnBeforeMigration(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationStarting, eventArgs);
        }

        /// <summary>
        /// Fires the migration error event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnMigrationError(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationError, eventArgs);
        }

        /// <summary>
        /// Fires the migration aborted event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnMigrationAborted(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationAborted, eventArgs);
        }

        /// <summary>
        /// Fires the migration complete event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnMigrationComplete(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationComplete, eventArgs);
        }

        /// <summary>
        /// Fires the individual change migration pre event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnMigratingChangeStarting(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigratingChangeStarting, eventArgs);
        }

        /// <summary>
        /// Fires the individual change migration post event.
        /// </summary>
        /// <param name="eventArgs">The event arguments</param>
        public virtual void OnMigratingChangeComplete(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigratingChangeComplete, eventArgs);
        }

        /// <summary>
        /// Fires the download started event
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        public void OnDownloadStarting(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationDownloadStarted, eventArgs);
        }

        /// <summary>
        /// Fires the download completed event
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        public void OnDownloadCompleted(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationDownloadCompleted, eventArgs);
        }

        /// <summary>
        /// Fires the migration warning event
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        public void OnMigrationWarning(VersionControlEventArgs eventArgs)
        {
            FireVCEvent(MigrationWarning, eventArgs);
        }

        #endregion

        /// <summary>
        /// Called to Complete a session
        /// </summary>
        protected virtual void Complete()
        {
            if (RunningMode == RunningMode.Service)
            {
                m_completeCurrentSyncEvent.Set();
            }
            else
            {
                OnSessionComplete(new MigrationSessionEventArgs(MigrationToolkitResources.SessionComplete, 0, 0));
            }   
        }

        /// <summary>
        /// Called to stop the current session
        /// </summary>
        public virtual void Stop()
        {
            bool needToStop = false;
            lock (m_stateLocker)
            {
                if (!m_isAborted && m_isRunning)
                {
                    needToStop = true;
                    m_isStopRequested = true;
                }
            }
            if (needToStop && (RunningMode == RunningMode.Service))
            {
                m_stopEvent.Set();
                m_serviceThread.Join();
            }     
        }

        /// <summary>
        /// Start a session in service mode
        /// </summary>
        public virtual void Start()
        {
            bool alreadyStarted = true;
            lock (m_stateLocker)
            {
                if (!m_isRunning)
                {
                    alreadyStarted = false;
                    m_isAbortRequested = false;
                    m_isStopRequested = false;
                    m_isAborted = false;
                    m_isComplete = false;
                    m_isRunning = true;
                    RunningMode = RunningMode.Service;
                }
            }
            if (!alreadyStarted)
            {
                OnSessionStart(new MigrationSessionEventArgs(MigrationToolkitResources.SessionStarting, 0, 0));
                InitializeSession();
                InitializeSyncEvents();
                m_serviceThread = new Thread(ThreadProc);
                m_serviceThread.Start();
            }
        }

        /// <summary>
        /// Thread that runs a vc session in service mode
        /// </summary>
        /// <returns></returns>
        protected void ThreadProc()
        {
            Thread.CurrentThread.Name = "VC session service thread";
            while (!(IsAbortRequested || IsStopRequested))
            {
                m_completeCurrentSyncEvent.Reset();
                DoSynchronizeFull();
                WaitHandle.WaitAny(m_completeCurrentSyncEvents, -1, false); // wait until the current synchronization completes or session is aborted.
                WaitHandle.WaitAny(m_completeSessionEvents, SleepTime * 1000, false); // wait for the duration of sleep time unless session is stopped or aborted.
            }
            m_completeCurrentSyncEvent.Reset();
            m_stopEvent.Reset();
            m_abortEvent.Reset();
            if (IsAbortRequested)
            {
                OnSessionAborted(new MigrationSessionEventArgs(MigrationToolkitResources.SessionAborted, 0, 0));
            }
            else
            {
                OnSessionComplete(new MigrationSessionEventArgs(MigrationToolkitResources.SessionComplete, 0, 0));
            }
        }


        /// <summary>
        /// Initialization methods for sync service
        /// </summary>
        protected void InitializeSyncEvents()
        {
            if (m_abortEvent == null)
            {
                m_abortEvent = new ManualResetEvent(false);
            }
            if (m_stopEvent == null)
            {
                m_stopEvent = new ManualResetEvent(false);
            }
            if (m_completeCurrentSyncEvent == null)
            {
                m_completeCurrentSyncEvent = new ManualResetEvent(false);
            }
            m_completeCurrentSyncEvent.Reset();
            m_abortEvent.Reset();
            m_stopEvent.Reset();

            m_completeCurrentSyncEvents = new WaitHandle[] { m_completeCurrentSyncEvent, m_abortEvent };
            m_completeSessionEvents = new WaitHandle[] { m_stopEvent, m_abortEvent };
        }

        /// <summary>
        /// Initialize method for session
        /// </summary>
        protected virtual void InitializeSession()
        {
            subscribeTfsListeners();
        }

        /// <summary>
        /// Cleanup method for session
        /// </summary>
        protected virtual void CleanupSession()
        {
            unSubscribeTfsListeners();
        }

        #region Tfs events listener
        private void unSubscribeTfsListeners()
        {
            if (Target != null)
            {

                TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(MigrationConfiguration.TfsServers[Target.Server].Server);
                VersionControlServer tfsClient = (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));

                tfsClient.CommitCheckin -= new CommitCheckinEventHandler(CommitCheckin);
                tfsClient.Conflict -= new ConflictEventHandler(Conflict);
                tfsClient.Getting -= new GettingEventHandler(Getting);
                tfsClient.NewPendingChange -= new PendingChangeEventHandler(NewPendingChange);
                tfsClient.NonFatalError -= new ExceptionEventHandler(NonFatalError);
                tfsClient.UndonePendingChange -= new PendingChangeEventHandler(UndonePendingChange);
            }

        }

        private void subscribeTfsListeners()
        {
            if (Target != null)
            {
                TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(MigrationConfiguration.TfsServers[Target.Server].Server);
                VersionControlServer tfsClient = (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));

                tfsClient.CommitCheckin += new CommitCheckinEventHandler(CommitCheckin);
                tfsClient.Conflict += new ConflictEventHandler(Conflict);
                tfsClient.Getting += new GettingEventHandler(Getting);
                tfsClient.NewPendingChange += new PendingChangeEventHandler(NewPendingChange);
                tfsClient.NonFatalError += new ExceptionEventHandler(NonFatalError);
                tfsClient.UndonePendingChange += new PendingChangeEventHandler(UndonePendingChange);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void UndonePendingChange(object sender, PendingChangeEventArgs e)
        {
            TraceManager.WriteLine(TraceManager.Engine, "Undoing {0}", e.PendingChange.ServerItem);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void NonFatalError(object sender, ExceptionEventArgs e)
        {
            // Exceptions are always rethrown
            if (e.Exception != null)
            {
                throw e.Exception;
            }

            // For starters, log the message
            TraceManager.TraceError(e.Failure.Message);

            if (e.Failure.Severity == SeverityType.Error)
            {
                TfsUtil.FailWithError(e.Failure.Message);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void CommitCheckin(object sender, CommitCheckinEventArgs e)
        {
            TraceManager.TraceInformation("Commited TFS change: {0}", e.ChangesetId);

            foreach (PendingChange p in e.UndoneChanges)
            {
                TraceManager.TraceInformation("Undone Change on Checkin: {0} ({1}):",
                    p.ServerItem, p.ChangeType);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void Conflict(object sender, ConflictEventArgs e)
        {
            TfsUtil.FailWithError(e.Message);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void NewPendingChange(object sender, PendingChangeEventArgs e)
        {
            TraceManager.WriteLine(TraceManager.Engine,
                "Pending change {0} for {1}", e.PendingChange.ChangeTypeName, e.PendingChange.ServerItem);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected virtual void Getting(object sender, GettingEventArgs e)
        {
            string error;
            string message = e.GetMessage(null, out error);
            TraceManager.WriteLine(TraceManager.Engine, message);
            if (error != null)
            {
                TraceManager.TraceError(error);
            }
        }

        #endregion // Tfs events listener

        /// <summary>
        /// Called to Abort a session
        /// </summary>
        public virtual void Abort()
        {
            bool needToAbort = false;
            lock (m_stateLocker)
            {
                if (!m_isAborted && m_isRunning)
                {
                    needToAbort = true;
                    m_isRunning = false;
                    m_isAbortRequested = true;
                }
            }
            if (needToAbort)
            {
                if (RunningMode == RunningMode.Service)
                {
                    m_abortEvent.Set();
                    m_serviceThread.Join();
                }
                else
                {
                    OnSessionAborted(new MigrationSessionEventArgs(MigrationToolkitResources.SessionAborted, 0, 0));
                }
            }
        }
        
        /// <summary>
        /// True if the session is running, false otherwise.
        /// </summary>
        public virtual bool IsRunning
        {
            get
            {
                lock(m_stateLocker)
                {
                    return m_isRunning;
                }
            }
        }

        /// <summary>
        /// True if the session is complete, false otherwise.
        /// </summary>
        public virtual bool IsComplete
        {
            get
            {
                lock (m_stateLocker)
                {
                    return m_isComplete;
                }
            }
        }

        /// <summary>
        /// True if the stop request has been received, false otherwise.
        /// </summary>
        public virtual bool IsStopRequested
        {
            get
            {
                lock (m_stateLocker)
                {
                    return m_isStopRequested;
                }
            }
        }

        /// <summary>
        /// True if the abort request has been received, false otherwise.
        /// </summary>
        public virtual bool IsAbortRequested
        {
            get
            {
                lock (m_stateLocker)
                {
                    return m_isAbortRequested;
                }
            }
        }

        /// <summary>
        /// True if the session is aborted, false otherwise.
        /// </summary>
        public virtual bool IsAborted
        {
            get
            {
                lock(m_stateLocker)
                {
                    return m_isAborted;
                }
            }
        }

        RunningMode RunningMode
        {
            get
            {
                return m_runningMode;
            }
            set
            {
                m_runningMode = value;
            }
        }
        /// <summary>
        /// Returns number of seconds to sleep between passes.
        /// </summary>
        protected int SleepTime 
        {
            get 
            {
                if (m_sleepTime == -1)
                {
                    m_sleepTime = GetValue<int>("SleepTime", 600);
                    if (m_sleepTime < 60)
                    {
                        Trace.TraceWarning("The sleep time value is too short. Reset it to 60 seconds.");
                        m_sleepTime = 60;
                    }
                }
                return m_sleepTime; 
            } 
            set 
            {
                if (value < 60)
                {
                    Trace.TraceWarning("The sleep time value is too short. Reset it to 60 seconds.");
                    m_sleepTime = 60;
                }
                else
                {
                    m_sleepTime = value;
                }
            } 
        }

        /// <summary>
        /// Registers the session with the migration event sink.
        /// </summary>
        /// <param name="sink"></param>
        public virtual void RegisterEventSink(IMigrationSessionEventSink sink)
        {
            if (sink != null)
            {
                sink.RegisterSession(this);
                m_cleanup.Add(sink);
            }
        }

        /// <summary>
        /// Represents session in a string form.
        /// </summary>
        /// <returns>String representation of the session</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "VC Session: {0}",              // Do not localize!
                m_id);
        }

        /// <summary>
        /// Called to start a one time one way synchronization
        /// </summary>
        /// <param name="primarySystem"></param>
        public virtual void Synchronize(SystemType primarySystem)
        {
            bool alreadyStarted= true;
            if (primarySystem == SystemType.Other)
            {
                RunningMode = RunningMode.SourceToTfs;
            }
            else
            {
                RunningMode = RunningMode.TfsToSource;
            }
            lock (m_stateLocker)
            {
                if (!m_isRunning)
                {
                    alreadyStarted = false;
                    m_isAborted = false;
                    m_isComplete = false;
                    m_isRunning = true;
                }
            }
            if (!alreadyStarted)
            {
                InitializeSession();
                DoSynchronize(primarySystem);
            }
            else
            {
                OnSessionStart(new MigrationSessionEventArgs(MigrationToolkitResources.SessionAlreadyStarted, 0, 0));
            }
        }

        /// <summary>
        /// Called to start a one time two way synchronization
        /// </summary>
        public virtual void SynchronizeFull()
        {
            bool alreadyStarted = true;
            RunningMode = RunningMode.FullSync;
            lock (m_stateLocker)
            {
                if (!m_isRunning)
                {
                    alreadyStarted = false;
                    m_isAborted = false;
                    m_isComplete = false;
                    m_isRunning = true;
                }
            }
            if (!alreadyStarted)
            {
                OnSessionStart(new MigrationSessionEventArgs(MigrationToolkitResources.SessionStarting, 0, 0));
                InitializeSession();
                DoSynchronizeFull();
            }
            else
            {
                OnSessionStart(new MigrationSessionEventArgs(MigrationToolkitResources.SessionAlreadyStarted, 0, 0));
            }
        }

        protected virtual void DoSynchronizeFull()
        {
            // Inherited session should inplement synchronization code here. 
            Complete();
        }

        protected virtual void DoSynchronize(SystemType primarySystem)
        {
            // Inherited session should inplement synchronization code here. 
            Complete();
        }

        /// <summary>
        /// Gets the helper object for logging custom events.
        /// </summary>
        public LogHelper LogHelper { get; internal set; }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // dispose managed resources
                    foreach (IDisposable toDispose in m_cleanup)
                    {
                        toDispose.Dispose();
                    }
                    if (m_abortEvent != null)
                    {
                        m_abortEvent.Close();
                    }
                    if (m_completeCurrentSyncEvent != null)
                    {
                        m_completeCurrentSyncEvent.Close();
                    }
                    if (m_stopEvent != null)
                    {
                        m_stopEvent.Close();
                    }
                }

                // release unmanged resources
            }

            m_disposed = true;
        }

        /// <summary>
        /// Dispose function
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        bool m_disposed;
    }

    enum RunningMode
    {
        Service, 
        FullSync,
        SourceToTfs,
        TfsToSource,
        NotSet
    */
    }
}
