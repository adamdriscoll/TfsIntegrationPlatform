// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Threading;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.SyncMonitor;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationService
{
    class TfsIntegrationService : ServiceBase
    {
        private ServiceHost m_runtimeTraceHost;
        private ServiceHost m_migrationServiceHost;
        private MigrationServiceClient m_migrationServiceProxy;
        private TraceWriterBase m_traceWriter;

        private readonly bool m_hostWCFInWindowsService;

        public TfsIntegrationService()
        {
            ChangeWorkingDirToExeLocation();

            this.ServiceName = Constants.TfsIntegrationServiceName;
            this.EventLog.Log = Constants.TfsServiceEventLogName;
            // EventLog.Source is the ServiceName of the service by default

            this.CanHandlePowerEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            m_migrationServiceHost = new CustomConfigServiceHost(MigrationService.GetInstance());
            m_runtimeTraceHost = new CustomConfigServiceHost(typeof(RuntimeTrace));

            m_hostWCFInWindowsService = GlobalConfiguration.UseWindowsService;
        }

        private void ChangeWorkingDirToExeLocation()
        {
            Environment.CurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main()
        {
            ServiceBase.Run(new TfsIntegrationService());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.EventLog.WriteEntry(String.Format("{0} starting...", this.ServiceName), EventLogEntryType.Information, 0, 0);

                if (m_hostWCFInWindowsService)
                {
                    m_migrationServiceHost.Open();
                    m_runtimeTraceHost.Open();

                    m_migrationServiceProxy = new MigrationServiceClient();
                    m_traceWriter = new FileTraceWriter();

                    m_traceWriter.StartListening();

                    RestartActiveSessionGroups();

                    this.EventLog.WriteEntry(String.Format("{0} is running", this.ServiceName), EventLogEntryType.Information, 0, 0);
                } 
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(String.Format("{0} error starting: {1}", this.ServiceName, e.Message), EventLogEntryType.Error, 0, 0);
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (m_hostWCFInWindowsService)
                {
                    this.RequestAdditionalTime(30000); // request additional 30 sec (the default session interval time) for stopping

                    this.EventLog.WriteEntry(String.Format("{0} stopping...", this.ServiceName), EventLogEntryType.Information, 0, 0);

                    StopRunningSessionGroups();

                    // Shut down WCF endpoints
                    m_runtimeTraceHost.Close();
                    m_migrationServiceHost.Close();
                }

                base.OnStop();

                this.EventLog.WriteEntry(String.Format("{0} stopped", this.ServiceName), EventLogEntryType.Information, 0, 0);

                if (m_hostWCFInWindowsService)
                {
                    if (null != m_traceWriter)
                    {
                        m_traceWriter.StopListening();
                        m_traceWriter.TracerThread.Join();
                    }
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(String.Format("{0} stopped with an exception: {1}", this.ServiceName, e.ToString()), EventLogEntryType.Information, 0, 0);
            }
        }

        protected override void OnPause()
        {
            if (m_hostWCFInWindowsService)
            {
                PauseRunningSessionGroups();
            }
            base.OnPause();
        }

        protected override void OnContinue()
        {
            if (m_hostWCFInWindowsService)
            {
                ResumeRunningSessionGroups();
            }
            base.OnContinue();
        }

        protected override void OnShutdown()
        {
            try
            {
                if (m_hostWCFInWindowsService)
                {
                    this.RequestAdditionalTime(30000); // request additional 30 sec (the default session interval time) for stopping

                    this.EventLog.WriteEntry(String.Format("{0} stopping in response to system ShutDown event...", this.ServiceName), EventLogEntryType.Information, 0, 0);

                    StopRunningSessionGroups();

                    // Shut down WCF endpoints
                    m_runtimeTraceHost.Close();
                    m_migrationServiceHost.Close();
                }

                base.OnShutdown();

                this.EventLog.WriteEntry(String.Format("{0} stopped", this.ServiceName), EventLogEntryType.Information, 0, 0);

                if (m_hostWCFInWindowsService)
                {
                    if (null != m_traceWriter)
                    {
                        m_traceWriter.StopListening();
                        m_traceWriter.TracerThread.Join();
                    }
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry(String.Format("{0} stopped with an exception: {1}", this.ServiceName, e.ToString()), EventLogEntryType.Information, 0, 0);
            }
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        // This is where Windows Service behavior over TFSMigrationDb lives... look for work to do when waking up
        private void RestartActiveSessionGroups()
        {
            TraceManager.TraceInformation("RestartActiveSessionGroups: Enter");

            BusinessModelManager businessModelManager = new BusinessModelManager();

            List<Guid> activeSessionGroupUniqueIds = businessModelManager.GetActiveSessionGroupUniqueIds();

            TraceManager.TraceInformation("RestartActiveSessionGroups: BusinessModelManager returned # sessions: {0}", activeSessionGroupUniqueIds.Count);

            foreach (Guid activeSessionGroupUniqueId in activeSessionGroupUniqueIds)
            {
                m_migrationServiceProxy.StartSessionGroup(activeSessionGroupUniqueId);
            }
        }

        private void StopRunningSessionGroups()
        {
            List<Guid> activeSessionGroupUniqueIds = m_migrationServiceProxy.GetRunningSessionGroups();

            foreach (Guid activeSessionGroupUniqueId in activeSessionGroupUniqueIds)
            {
                m_migrationServiceProxy.StopSessionGroup(activeSessionGroupUniqueId);
            }
        }

        private void PauseRunningSessionGroups()
        {
            List<Guid> activeSessionGroupUniqueIds = m_migrationServiceProxy.GetRunningSessionGroups();

            foreach (Guid activeSessionGroupUniqueId in activeSessionGroupUniqueIds)
            {
                m_migrationServiceProxy.PauseSessionGroup(activeSessionGroupUniqueId);
            }
        }

        private void ResumeRunningSessionGroups()
        {
            List<Guid> activeSessionGroupUniqueIds = m_migrationServiceProxy.GetRunningSessionGroups();

            foreach (Guid activeSessionGroupUniqueId in activeSessionGroupUniqueIds)
            {
                m_migrationServiceProxy.ResumeSessionGroup(activeSessionGroupUniqueId);
            }
        }
    }
}
