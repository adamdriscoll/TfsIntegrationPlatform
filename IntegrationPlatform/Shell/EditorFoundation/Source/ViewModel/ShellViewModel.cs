// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class ShellViewModel : ViewModel<ShellController, ConfigurationModel>
    {
        #region Fields
        private ExtensibilityViewModel m_extensibilityViewModel;

        private MigrationServiceManager m_migrationServiceManager;
        private static RuntimeManager m_runtimeManager;
        
        private bool m_allowOpenFromDB = true;
        private bool m_allowSaveToDB = true;

        private bool m_isRunning = false;

        private bool m_allowStart = true;
        private bool m_allowPause = false;
        private bool m_allowStop = false;
        private BackgroundWorker m_backgroundWorker = new BackgroundWorker();
        private static bool[,] s_canTransitionFrom;
        #endregion

        public bool IsAdvancedRulesEnabled
        {
            get
            {
                return Properties.Settings.Default.IsAdvancedRulesEnabled;
            }
            set
            {
                Properties.Settings.Default.IsAdvancedRulesEnabled = value;
                RaisePropertyChangedEvent("IsAdvancedRulesEnabled");
            }
        }

        private ConfigurationViewModel m_configViewModel;
        public ConfigurationViewModel ConfigViewModel
        {
            get
            {
                if (m_configViewModel == null && DataModel != null && DataModel.Configuration != null)
                {
                    try
                    {
                        m_configViewModel = new ConfigurationViewModel(this);
                    }
                    catch (Exception) // it's okay to leave m_configViewModel as null
                    {
                    }
                }
                return m_configViewModel;
            }
        }
        #region Constructors
        static ShellViewModel()
        {
            var v = Enum.GetValues(typeof(PipelineState));
            s_canTransitionFrom = new bool[v.Length, v.Length];
            s_canTransitionFrom[(int)PipelineState.Default, (int)PipelineState.Default] = true;
            s_canTransitionFrom[(int)PipelineState.Default, (int)PipelineState.Starting] = true;
            s_canTransitionFrom[(int)PipelineState.Default, (int)PipelineState.Running] = true;
            s_canTransitionFrom[(int)PipelineState.Starting, (int)PipelineState.Running] = true;
            s_canTransitionFrom[(int)PipelineState.Running, (int)PipelineState.Stopping] = true;
            s_canTransitionFrom[(int)PipelineState.Running, (int)PipelineState.Stopped] = true;
            s_canTransitionFrom[(int)PipelineState.Stopping, (int)PipelineState.Stopped] = true;
            s_canTransitionFrom[(int)PipelineState.Stopped, (int)PipelineState.Starting] = true;
            s_canTransitionFrom[(int)PipelineState.Stopped, (int)PipelineState.Running] = true;
            s_canTransitionFrom[(int)PipelineState.Running, (int)PipelineState.Pausing] = true;
            s_canTransitionFrom[(int)PipelineState.Pausing, (int)PipelineState.Paused] = true;
            s_canTransitionFrom[(int)PipelineState.Running, (int)PipelineState.Paused] = true;
            s_canTransitionFrom[(int)PipelineState.Paused, (int)PipelineState.Starting] = true;
            s_canTransitionFrom[(int)PipelineState.Paused, (int)PipelineState.Running] = true;
            s_canTransitionFrom[(int)PipelineState.Paused, (int)PipelineState.Stopping] = true;
            s_canTransitionFrom[(int)PipelineState.Paused, (int)PipelineState.Stopped] = true;
            s_canTransitionFrom[(int)PipelineState.Running, (int)PipelineState.Running] = true;
            s_canTransitionFrom[(int)PipelineState.Paused, (int)PipelineState.Paused] = true;
            s_canTransitionFrom[(int)PipelineState.Stopped, (int)PipelineState.Stopped] = true;
        }

        public ShellViewModel()
        {
            // TODO: set config directly
            m_migrationServiceManager = MigrationServiceManager.GetInstance();

            // Set up runtime view model hosting environment
            m_runtimeManager = RuntimeManager.GetInstance();
            IRefreshService refresh = (IRefreshService)m_runtimeManager.GetService(typeof(IRefreshService));
            refresh.AutoRefresh += this.AutoRefresh;

            // Avoid race by driving through top level refresh instead of relying upon background thread.
            Refresh();

            ConfigurationModel.Initialize();

            m_headlineViewModel = new HeadlineControlViewModel(Properties.Resources.ShellHeaderString, this);
            m_notificationBarVM = new NotificationBarViewModel(this);
            PushViewModel(new HomeViewModel(this));

            m_backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            m_backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_backgroundWorker_RunWorkerCompleted);
            
            this.Controller.Created += this.OnCreated;
            this.Controller.Opened += this.OnOpened;
            this.Controller.Saved += this.OnSaved;
        }
        #endregion

        void ConflictManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsBusy"))
            {
                RaisePropertyChangedEvent("IsBusy");
            }
        }

        public override bool IsBusy
        {
            get
            {
                return base.IsBusy || ConflictManager.IsBusy;
            }
        }

        public void AutoRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        private BackgroundWorker m_refreshBW;

        private void Refresh() // TODO: make async
        {
            if (DataModel != null && DataModel.Configuration != null)
            {
                if (m_refreshBW == null)
                {
                    m_refreshBW = new BackgroundWorker();
                    m_refreshBW.DoWork += new DoWorkEventHandler(m_refreshBW_DoWork);
                    m_refreshBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_refreshBW_RunWorkerCompleted);
                }

                if (!m_refreshBW.IsBusy)
                {
                    m_refreshBW.RunWorkerAsync();
                }
            }

            LastRefreshed = String.Format("{0}: {1}", Properties.Resources.LastRefreshString, DateTime.Now.ToString("hh:mmtt"));
        }

        void m_refreshBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                CurrentPipelineState = (PipelineState)e.Result;
            }
        }

        void m_refreshBW_DoWork(object sender, DoWorkEventArgs e)
        {
            IMigrationService migrationService = (IMigrationService)MigrationServiceManager.GetService(typeof(IMigrationService));
            SessionGroupInitializationStatus status = SessionGroupInitializationStatus.Unknown;
            if (DataModel != null && DataModel.Configuration != null)
            {
                status = migrationService.GetSessionGroupInitializationStatus(DataModel.Configuration.SessionGroupUniqueId);
            }
            switch (status)
            {
                case SessionGroupInitializationStatus.Initialized:
                    if (migrationService.GetRunningSessionGroups().Contains(DataModel.Configuration.SessionGroupUniqueId))
                    {
                        ISyncStateManager stateManager = SqlSyncStateManager.GetInstance();
                        PipelineState newPipelineState = stateManager.GetCurrentState(OwnerType.SessionGroup, DataModel.Configuration.SessionGroupUniqueId);
                        if (s_canTransitionFrom[(int)m_currentPipelineState, (int)newPipelineState])
                        {
                            e.Result = newPipelineState;
                        }
                        else
                        {
                            Console.WriteLine("Cannot transition from " + m_currentPipelineState + " to " + newPipelineState);
                        }
                        IsCompleted = false;
                    }
                    else
                    {
                        e.Result = PipelineState.Default;
                        using (Microsoft.TeamFoundation.Migration.EntityModel.RuntimeEntityModel context = Microsoft.TeamFoundation.Migration.EntityModel.RuntimeEntityModel.CreateInstance())
                        {
                            var query = from sg in context.RTSessionGroupSet
                                        where sg.GroupUniqueId.Equals(DataModel.Configuration.SessionGroupUniqueId)
                                        select sg.State;
                            int? state = query.FirstOrDefault();
                            if (state != null && (Microsoft.TeamFoundation.Migration.BusinessModel.BusinessModelManager.SessionStateEnum)state == BusinessModelManager.SessionStateEnum.Completed && ConflictManager != null && ConflictManager.TotalConflicts == 0)
                            {
                                IsCompleted = true;
                            }
                            else
                            {
                                IsCompleted = false;
                            }
                        }
                    }
                    break;
                case SessionGroupInitializationStatus.Initializing:
                    IsCompleted = false;
                    break;
                case SessionGroupInitializationStatus.NotInitialized:
                case SessionGroupInitializationStatus.Unknown:
                    e.Result = PipelineState.Default;
                    IsCompleted = false;
                    break;
            }
        }

        #region Properties

        private string m_lastRefreshed;
        public string LastRefreshed
        {
            get
            {
                return m_lastRefreshed;
            }
            private set
            {
                m_lastRefreshed = value;
                RaisePropertyChangedEvent("LastRefreshed");
            }
        }
        private MigrationStatusViews m_migrationView = MigrationStatusViews.Progress;
        public MigrationStatusViews ShowMigrationView
        {
            get
            {
                return m_migrationView;
            }
            set
            {
                m_migrationView = value;
                RaisePropertyChangedEvent("ShowMigrationView");
            }
        }

        private SystemState m_systemState = SystemState.NoConfiguration;
        public SystemState SystemState
        {
            get
            {
                return m_systemState;
            }
            set
            {
                m_systemState = value;
                RaisePropertyChangedEvent("SystemState");
            }
        }

        private void SetViewRelatedHeaders()
        {
            HeadlineViewModel.ShowRefreshTime = SelectedViewModel is ShellViewModel;
            NotificationBarViewModel.RefreshDefaultNotification();
        }

        private Stack<object> m_viewModelStack = new Stack<object>();
        private object m_modalViewModel;

        public void SetModalViewModel(object viewModel)
        {
            m_modalViewModel = viewModel;
            SetViewRelatedHeaders();
            RaisePropertyChangedEvent("SelectedViewModel");
        }

        public void ClearModalViewModel()
        {
            m_modalViewModel = null;
            SetViewRelatedHeaders();
            RaisePropertyChangedEvent("SelectedViewModel");
        }

        public void PushViewModel(object viewModel)
        {
            Debug.Assert(!(viewModel is ShellViewModel), "ShellViewModel incorrectly pushed onto stack.  Use ClearViewModels().");
            ClearModalViewModel();
            if (viewModel is ShellViewModel)
            {
                ClearViewModels();
            }
            else if (!SelectedViewModel.Equals(viewModel))
            {
                m_viewModelStack.Push(viewModel);
            }
            SetViewRelatedHeaders();
            RaisePropertyChangedEvent("SelectedViewModel");
        }

        public void PopViewModel(object viewModel)
        {
            if (!(viewModel is ShellViewModel) && SelectedViewModel.Equals(viewModel))
            {
                m_viewModelStack.Pop();
                SetViewRelatedHeaders();
                RaisePropertyChangedEvent("SelectedViewModel");
            }
        }

        public void ClearViewModels()
        {
            if (m_viewModelStack.Count > 0 && m_viewModelStack.Peek() is ConfigurationViewModel)
            {
                ConfigurationViewModel configViewModel = m_viewModelStack.Peek() as ConfigurationViewModel;
                if (!configViewModel.Cancel())
                {
                    return;
                }
            }

            ClearModalViewModel();
            m_viewModelStack.Clear();
            RaisePropertyChangedEvent("SelectedViewModel");
        }

        public object SelectedViewModel
        {
            get
            {
                if (m_modalViewModel != null)
                {
                    return m_modalViewModel;
                }
                else if (m_viewModelStack.Count == 0)
                {
                    return this;
                }
                else
                {
                    return m_viewModelStack.Peek();
                }
            }
        }

        private HeadlineControlViewModel m_headlineViewModel;
        public HeadlineControlViewModel HeadlineViewModel
        {
            get
            {
                return m_headlineViewModel;
            }
            set
            {
                m_headlineViewModel = value;
                RaisePropertyChangedEvent("HeadlineViewModel");
            }
        }

        private NotificationBarViewModel m_notificationBarVM;
        public NotificationBarViewModel NotificationBarViewModel
        {
            get
            {
                return m_notificationBarVM;
            }
            set
            {
                m_notificationBarVM = value;
                RaisePropertyChangedEvent("NotificationBarViewModel");
            }
        }
        /// <summary>
        /// This value determines whether a session group can be started at this time.
        /// </summary>
        private bool m_canStart;
        public bool CanStart
        {
            get
            {
                return m_canStart;
            }
            private set
            {
                if (m_canStart != value)
                {
                    m_canStart = value;
                    RaisePropertyChangedEvent("CanStart");
                }
            }
        }
        /// <summary>
        /// This view on AllowPause is wired into the actual command handler.
        /// </summary>
        private bool m_canPause;
        public bool CanPause
        {
            get
            {
                return m_canPause;
            }
            private set
            {
                if (m_canPause != value)
                {
                    m_canPause = value;
                    RaisePropertyChangedEvent("CanPause");
                }
            }
        }
        /// <summary>
        /// This property determines whether a running session group can be stopped at this time.
        /// </summary>
        private bool m_canStop;
        public bool CanStop
        {
            get
            {
                return m_canStop;
            }
            private set
            {
                if (m_canStop != value)
                {
                    m_canStop = value;
                    RaisePropertyChangedEvent("CanStop");
                }
            }
        }

        private ApplicationViewModel m_conflictManager;
        public ApplicationViewModel ConflictManager
        {
            get
            {
                return m_conflictManager;
            }
            private set
            {
                if (m_conflictManager != value)
                {
                    m_conflictManager = value;
                    m_conflictManager.PropertyChanged += new PropertyChangedEventHandler(ConflictManager_PropertyChanged);
            
                    RaisePropertyChangedEvent("ConflictManager");
                }
            }
        }
        
        // TODO: Move to controller
        public RuntimeManager RuntimeManager
        {
            get
            {
                return m_runtimeManager;
            }
        }

        // TODO: Move to controller
        public MigrationServiceManager MigrationServiceManager
        {
            get
            {
                return m_migrationServiceManager;
            }
        }

        public ExtensibilityViewModel ExtensibilityViewModel
        {
            get
            {
                if (m_extensibilityViewModel == null)
                {
                    ExtensibilityViewModel = new ExtensibilityViewModel();
                }
                return m_extensibilityViewModel;
            }
            private set
            {
                m_extensibilityViewModel = value;

                foreach (IMigrationSourceView migrationSourceView in Controller.PluginManager.GetMigrationSourceViews())
                {
                    m_extensibilityViewModel.AddMigrationSourceView(migrationSourceView);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether open of ConfigurationModel is allowed.
        /// </summary>
        public bool AllowOpenFromDB
        {
            get
            {
                return m_allowOpenFromDB;
            }
            set
            {
                if (m_allowOpenFromDB != value)
                {
                    m_allowOpenFromDB = value;
                    OnAllowOpenFromDBChanged();
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether a ConfigurationModel can be opened at this time.
        /// </summary>
        public bool CanOpenFromDB
        {
            get
            {
                return this.AllowOpenFromDB;
            }
        }

        public bool CanOpenRecent
        {
            get
            {
                return !Properties.Settings.Default.LastSessionGroupUniqueId.Equals(Guid.Empty) && !IsDataModelLoaded;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether save of ConfigurationModel is allowed.
        /// </summary>
        public bool AllowSaveToDB
        {
            get
            {
                return m_allowSaveToDB;
            }
            set
            {
                if (m_allowSaveToDB != value)
                {
                    m_allowSaveToDB = value;
                    OnAllowSaveToDBChanged();
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether a ConfigurationModel can be saved at this time.
        /// </summary>
        public bool CanSaveToDB
        {
            get
            {
                return this.AllowSaveToDB && CanSave;
            }
        }

        private bool m_isCompleted = false;
        public bool IsCompleted
        {
            get
            {
                return m_isCompleted;
            }
            set
            {
                if (m_isCompleted != value)
                {
                    m_isCompleted = value;
                    RaisePropertyChangedEvent("IsCompleted");
                    NotificationBarViewModel.RefreshDefaultNotification();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return m_isRunning;
            }
            set
            {
                if (m_isRunning != value)
                {
                    m_isRunning = value;
                    OnIsRunningChanged();
                }
            }
        }

        /// <summary>
        /// Determines whether the UI elements allowing a start command to be
        /// issued should be lit up.
        /// </summary>
        public bool AllowStart
        {
            get
            {
                return m_allowStart;
            }
            set
            {
                if (m_allowStart != value)
                {
                    m_allowStart = value;
                    OnAllowStartChanged();
                }
            }
        }

        private PipelineState m_currentPipelineState;
        public PipelineState CurrentPipelineState
        {
            get
            {
                if (m_currentPipelineState == PipelineState.Default)
                {
                    return PipelineState.Stopped;
                }
                return m_currentPipelineState;
            }
            set
            {
                m_currentPipelineState = value;
                RaisePropertyChangedEvent("CurrentPipelineState");
                if (!IsCompleted && NotificationBarViewModel.ShowNotifications)
                {
                    NotificationBarViewModel.RefreshDefaultNotification();
                }

                switch (CurrentPipelineState)
                {
                    case PipelineState.Pausing:
                    case PipelineState.Starting:
                    case PipelineState.Stopping:
                    case PipelineState.StoppingSingleTrip:
                        CanStart = false;
                        CanStop = false;
                        CanPause = false;
                        break;
                    case PipelineState.Default:
                    case PipelineState.StoppedSingleTrip:
                    case PipelineState.Stopped:
                        CanStart = true;
                        CanStop = false;
                        CanPause = false;
                        RuntimeManager.DisableAutoRefresh();
                        break;
                    case PipelineState.Running:
                        CanStart = false;
                        CanStop = true;
                        CanPause = true;
                        break;
                    case PipelineState.Paused:
                        CanStart = true;
                        CanStop = true;
                        CanPause = false;
                        RuntimeManager.DisableAutoRefresh();
                        break;
                    case PipelineState.PausedByConflict:
                        // The conflicts need to be resolved before starting or stopping
                        CanStart = false;
                        CanStop = false;
                        CanPause = false;
                        break;
                }
            }
        }
        
        /// <summary>
        /// Tracks whether the sync orchestrator is running and can be paused.
        /// </summary>
        public bool AllowPause
        {
            get
            {
                return m_allowPause;
            }
            set
            {
                if (m_allowPause != value)
                {
                    m_allowPause = value;
                    OnAllowPauseChanged();
                }
            }
        }

        
        /// <summary>
        /// Tracks whether the sync orchestrator is running and can be stopped.
        /// </summary>
        public bool AllowStop
        {
            get
            {
                return m_allowStop;
            }
            set
            {
                if (m_allowStop != value)
                {
                    m_allowStop = value;
                    OnAllowStopChanged();
                }
            }
        }

        public SessionGroupStatus SessionGroupStatus { get; private set; }

        public bool IsConfigurationPersisted
        {
            get
            {
                if (DataModel != null)
                {
                    BusinessModelManager businessModelManager = new BusinessModelManager();
                    return businessModelManager.IsConfigurationPersisted(DataModel.Configuration);
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsDirty
        {
            get
            {
                return (HasUnsavedChanges || !IsConfigurationPersisted) && !HasErrors;
            }
        }

        public bool HasErrors { get; set; }

        public override string Title
        {
            get
            {
                if (IsDataModelLoaded)
                {
                    return DataModel.Configuration.FriendlyName;
                }
                else
                {
                    return base.Title;
                }
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Open an existing Configuration from the underlying migration tools
        /// DB synchronously.
        /// </summary>
        /// <param name="sessionGroupUniqueId"></param>
        public virtual bool OpenFromDB(Guid sessionGroupUniqueId)
        {
            if (this.CanOpenFromDB)
            {
                return this.Controller.Open(sessionGroupUniqueId);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Save a Configuration from to the DB.
        /// </summary>
        /// <param name="sessionGroupUniqueId"></param>
        public virtual bool SaveToDB(Guid sessionGroupUniqueId, bool saveAsNew)
        {
            if (this.CanSaveToDB)
            {
                return this.Controller.Save(sessionGroupUniqueId, saveAsNew);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Starts running the SessionGroup defined by the current DataModel.
        /// </summary>
        public virtual void Start()
        {
            if (DataModel != null && DataModel.Configuration != null && !m_backgroundWorker.IsBusy)
            {
                CanStart = false;
                CanStop = false;
                CanPause = false;
                SystemState = SystemState.MigrationProgress;

                
                Guid sessionGroupUniqueId = DataModel.Configuration.SessionGroupUniqueId;
                if (CurrentPipelineState == PipelineState.Paused) // is paused, resume TODO: State=Paused,Running,Stopped
                {
                    m_currentPipelineState = PipelineState.Starting;
                    NotificationBarViewModel.SetState(NotificationState.Info, DataModel.Configuration.SessionGroup.FriendlyName,
                        Properties.Resources.StartingString);
                    m_backgroundWorker.RunWorkerAsync(new BackgroundWorkerArgs(sessionGroupUniqueId, BackgroundWorkerTask.Resume));
                }
                else
                {
                    m_currentPipelineState = PipelineState.Starting;
                    NotificationBarViewModel.SetState(NotificationState.Info, DataModel.Configuration.SessionGroup.FriendlyName, 
                        Properties.Resources.StartingString);
                    m_backgroundWorker.RunWorkerAsync(new BackgroundWorkerArgs(sessionGroupUniqueId, BackgroundWorkerTask.Start));
                }
                RaisePropertyChangedEvent("CurrentPipelineState");

                RuntimeManager.EnableAutoRefresh();
            }
        }

        enum BackgroundWorkerTask
        {
            Start,
            Resume,
            Stop,
            Pause
        }

        private class BackgroundWorkerArgs
        {
            public Guid SessionGroupUniqueId { get; private set; }
            public BackgroundWorkerTask Task { get; private set; }
            public BackgroundWorkerArgs(Guid sessionGroupUniqueId, BackgroundWorkerTask task)
            {
                SessionGroupUniqueId = sessionGroupUniqueId;
                Task = task;
            }
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorkerArgs args = (BackgroundWorkerArgs)e.Argument;
            IMigrationService migrationService = (IMigrationService)MigrationServiceManager.GetService(typeof(IMigrationService));
            switch(args.Task)
            {
                case BackgroundWorkerTask.Start:
                    migrationService.StartSessionGroup(args.SessionGroupUniqueId);
                    e.Result = args;
                    break;
                case BackgroundWorkerTask.Resume:
                    migrationService.ResumeSessionGroup(args.SessionGroupUniqueId);
                    break;
                case BackgroundWorkerTask.Stop:
                    migrationService.StopSessionGroup(args.SessionGroupUniqueId);
                    break;
                case BackgroundWorkerTask.Pause:
                    migrationService.PauseSessionGroup(args.SessionGroupUniqueId);
                    break;
            }
        }

        void m_backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorkerArgs args = (BackgroundWorkerArgs)e.Result;
            if (args != null && args.Task == BackgroundWorkerTask.Start)
            {
                //this.ConflictManager.SetSessionGroupUniqueId(args.SessionGroupUniqueId, true);
            }
        }

        /// <summary>
        /// Stops running the SessionGroup defined by the current DataModel.
        /// </summary>
        public virtual void Stop()
        {
            if (DataModel != null && DataModel.Configuration != null)
            {
                CanStart = false;
                CanStop = false;
                CanPause = false;
                SystemState = SystemState.MigrationStopped;
                m_currentPipelineState = PipelineState.Stopping;
                RaisePropertyChangedEvent("CurrentPipelineState");
                NotificationBarViewModel.SetState(NotificationState.Info, DataModel.Configuration.SessionGroup.FriendlyName,
                    Properties.Resources.StoppingString);

                Guid sessionGroupUniqueId = DataModel.Configuration.SessionGroupUniqueId;
                m_backgroundWorker.RunWorkerAsync(new BackgroundWorkerArgs(sessionGroupUniqueId, BackgroundWorkerTask.Stop));

                RuntimeManager.EnableAutoRefresh();
            }
        }

        /// <summary>
        /// Pauses the SessionGroup defined by the current DataModel.
        /// </summary>
        public virtual void Pause()
        {
            if (DataModel != null && DataModel.Configuration != null)
            {
                CanStart = false;
                CanStop = false;
                CanPause = false;
                m_currentPipelineState = PipelineState.Pausing;
                RaisePropertyChangedEvent("CurrentPipelineState");
                NotificationBarViewModel.SetState(NotificationState.Info, DataModel.Configuration.SessionGroup.FriendlyName,
                    Properties.Resources.PausingString);


                Guid sessionGroupUniqueId = DataModel.Configuration.SessionGroupUniqueId;
                m_backgroundWorker.RunWorkerAsync(new BackgroundWorkerArgs(sessionGroupUniqueId, BackgroundWorkerTask.Pause));

                RuntimeManager.EnableAutoRefresh();
            }
        }
        #endregion

        public void SetSessionGroupUniqueId(Guid sessionGroupUniqueId)
        {
            SessionGroupStatus = new SessionGroupStatus(sessionGroupUniqueId);
            ConflictManager = new ApplicationViewModel(this);
            ConflictManager.SetSessionGroupUniqueId(sessionGroupUniqueId, true);
            this.RuntimeManager.SetSessionGroupUniqueId(sessionGroupUniqueId, ConflictManager);
            Properties.Settings.Default.LastSessionGroupUniqueId = sessionGroupUniqueId;
            
            if (!GlobalConfiguration.UseWindowsService)
            {
                ISyncStateManager stateManager = SqlSyncStateManager.GetInstance();
                stateManager.TryResetSessionGroupStates(sessionGroupUniqueId);
                RuntimeManager.DisableAutoRefresh();
            }
            else
            {
                RuntimeManager.EnableAutoRefresh();
            }
            
            Refresh();
        }

        public void RefreshConfigViewModel()
        {
            m_configViewModel = null;
            RaisePropertyChangedEvent("ConfigViewModel");
        }

        private void OnCreated(object sender, EventArgs eventArgs)
        {
            RefreshConfigViewModel();
            RaisePropertyChangedEvent("IsConfigurationPersisted");
            ShellController controller = sender as ShellController;
            if (!IsConfigurationPersisted)
            {
                ViewConfigurationCommand.Open(this, null);
            }
        }

        private void OnOpened(object sender, OpenedEventArgs eventArgs)
        {
            try
            {
                RefreshConfigViewModel();
                RaisePropertyChangedEvent("IsConfigurationPersisted");
                if (eventArgs.Error == null)
                {
                    if (Utilities.IsGuid(eventArgs.FilePath))
                    {
                        // opened from DB
                        Guid sessionGroupUniqueId = new Guid(eventArgs.FilePath);
                        SetSessionGroupUniqueId(this.DataModel.Configuration.SessionGroupUniqueId);
                    }
                    else
                    {
                        // opened from file
                        SessionGroupStatus = new SessionGroupStatus(Guid.Empty);
                    }

                    RaisePropertyChangedEvent("Title");
                    if (!IsConfigurationPersisted)
                    {
                        ViewConfigurationCommand.Open(this, null);
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.HandleException(e);
            }
        }

        private void OnSaved(object sender, SavedEventArgs eventArgs)
        {
            RefreshConfigViewModel();
            RaisePropertyChangedEvent("IsConfigurationPersisted");
            if (eventArgs.Error == null)
            {
                if (Utilities.IsGuid(eventArgs.FilePath))
                {
                    // saved to DB
                    Guid sessionGroupUniqueId = new Guid(eventArgs.FilePath);
                    SetSessionGroupUniqueId(this.DataModel.Configuration.SessionGroupUniqueId);
                }
                else
                {
                    // saved to file
                }
            }
        }

        public void LoadConflictTypes()
        {
            ExtensibilityViewModel = new ExtensibilityViewModel();

            // load corresponding shell adapters and decorate extensibility view model, e.g. connect dialogs, conflict types (assert if user controls not paired) 
            foreach (MigrationSource migrationSource in DataModel.Configuration.SessionGroup.MigrationSources.MigrationSource)
            {
                Guid sourceId = new Guid(migrationSource.InternalUniqueId);
                Guid providerId = new Guid(migrationSource.ProviderReferenceName);

                Guid shellAdapterIdentifier;
                ProviderHandler providerHandler = ConfigViewModel.AllProviders.FirstOrDefault(x => x.ProviderId.Equals(providerId));
                if (providerHandler != null)
                {
                    shellAdapterIdentifier = providerHandler.ProviderDescriptionAttribute.ShellAdapterIdentifier;
                }
                else
                {
                    shellAdapterIdentifier = providerId;
                }

                IEnumerable<IConflictTypeView> shellAdapterConflictTypes = PluginManager.GetConflictTypes(shellAdapterIdentifier) ?? new List<IConflictTypeView>();
                IEnumerable<ConflictType> providerConflictTypes = ConflictManager.Sync.GetConflictTypes(sourceId);
                IEnumerable<ConflictType> sessionGroupConflictTypes = ConflictManager.Sync.GetConflictTypes(sourceId);
                
                foreach (ConflictType conflictType in providerConflictTypes)
                {
                    IConflictTypeView conflictTypeView = shellAdapterConflictTypes.FirstOrDefault(x => x.Guid.Equals(conflictType.ReferenceName));
                    if (conflictTypeView != null)
                    {
                        ExtensibilityViewModel.AddConflictTypeView(conflictTypeView, sourceId);
                    }
                    else
                    {
                        Debug.Fail(string.Format("UserControl not found for conflict type: {0}", conflictType.FriendlyName));
                    }
                }

                foreach (ConflictType conflictType in sessionGroupConflictTypes)
                {
                    IConflictTypeView conflictTypeView = shellAdapterConflictTypes.FirstOrDefault(x => x.Guid.Equals(conflictType.ReferenceName));
                    if (conflictTypeView != null)
                    {
                        ExtensibilityViewModel.AddConflictTypeView(conflictTypeView, Microsoft.TeamFoundation.Migration.Toolkit.Constants.FrameworkSourceId);
                    }
                    else
                    {
                        Debug.Fail(string.Format("UserControl not found for conflict type: {0}", conflictType.FriendlyName));
                    }
                }
            }
        }

        #region Protected Methods
        /// <summary>
        /// State of underlying properties determine whether open of a ConfigurationModel is allowed.
        /// </summary>
        private void OnAllowOpenFromDBChanged()
        {
            RaiseAllowOpenFromDBChanged();
            RaiseCanOpenFromDBChanged();
        }

        /// <summary>
        /// State of underlying properties determine whether save of a ConfigurationModel is allowed.
        /// </summary>
        private void OnAllowSaveToDBChanged()
        {
            RaiseAllowSaveToDBChanged();
            RaiseCanSaveToDBChanged();
        }

        /// <summary>
        /// Called when session run state changes.
        /// </summary>
        protected void OnIsRunningChanged()
        {
            RaiseIsRunningChangedEvent();
        }

        /// <summary>
        /// Called when allow start changes.
        /// </summary>
        protected void OnAllowStartChanged()
        {
            RaiseAllowStartChangedEvent();
            RaiseCanStartChangedEvent();
        }

        /// <summary>
        /// Called when allow pause changes.
        /// </summary>
        protected void OnAllowPauseChanged()
        {
            RaiseAllowPauseChangedEvent();
            RaiseCanPauseChangedEvent();
        }

        /// <summary>
        /// Called when allow pause changes.
        /// </summary>
        protected void OnAllowStopChanged()
        {
            RaiseAllowStopChangedEvent();
            RaiseCanStopChangedEvent();
        }
        #endregion

        #region Private Methods
        private void RaiseAllowOpenFromDBChanged()
        {
            this.RaisePropertyChangedEvent("AllowOpenFromDB");
        }

        private void RaiseCanOpenFromDBChanged()
        {
            this.RaisePropertyChangedEvent("CanOpenFromDB");
        }

        private void RaiseAllowSaveToDBChanged()
        {
            this.RaisePropertyChangedEvent("AllowSaveToDB");
        }

        private void RaiseCanSaveToDBChanged()
        {
            this.RaisePropertyChangedEvent("CanSaveToDB");
        }

        private void RaiseIsRunningChangedEvent()
        {
            RaisePropertyChangedEvent("IsRunning");
        }

        private void RaiseAllowStartChangedEvent()
        {
            RaisePropertyChangedEvent("AllowStart");
        }

        private void RaiseCanStartChangedEvent()
        {
            RaisePropertyChangedEvent("CanStart");
        }

        private void RaiseAllowPauseChangedEvent()
        {
            RaisePropertyChangedEvent("AllowPause");
        }

        private void RaiseCanPauseChangedEvent()
        {
            RaisePropertyChangedEvent("CanPause");
        }

        private void RaiseAllowStopChangedEvent()
        {
            RaisePropertyChangedEvent("AllowStop");
        }

        private void RaiseCanStopChangedEvent()
        {
            RaisePropertyChangedEvent("CanStop");
        }
        #endregion
    }
    public enum SystemState
    {
        NoConfiguration,
        EditConfiguration,
        ConfigurationSaved,
        MigrationProgress,
        MigrationStopped,
        MigrationCompleted
    }
    public enum MigrationStatusViews
    {
        Configuration,
        Conflicts,
        Progress,
    }
}
