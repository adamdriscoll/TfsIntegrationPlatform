// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    public class TfsIntegrationJobService : ServiceBase
    {
        private List<ITfsIntegrationJob> m_loadedJobs = new List<ITfsIntegrationJob>();
        private JobScheduler m_jobScheduler;

        public TfsIntegrationJobService()
        {
            TraceManager.TraceInformation("Initializing TfsIntegrationJobService ...");

            this.ServiceName = Constants.TfsIntegrationJobServiceName;
            this.EventLog.Log = Constants.TfsServiceEventLogName;
            // EventLog.Source is the ServiceName of the service by default

            this.CanHandlePowerEvent = true;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;

            TraceManager.TraceInformation("Finished initializing TfsIntegrationJobService ...");
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Uncomment to debug startup of service
                // Thread.Sleep(40000);

                TraceManager.TraceInformation("Starting TfsIntegrationJobService ...");
                this.EventLog.WriteEntry(String.Format("{0} starting...", this.ServiceName), EventLogEntryType.Information, 0, 0);

                // todo: use Jobs folder
                string jobsFolder = Microsoft.TeamFoundation.Migration.Toolkit.Constants.PluginsFolderName;
                IEnumerable<ProviderHandler> providers =
                    Microsoft.TeamFoundation.Migration.Toolkit.Utility.LoadProvider(new DirectoryInfo(jobsFolder));

                Dictionary<ITfsIntegrationJob, Job> jobAndConfig = new Dictionary<ITfsIntegrationJob, Job>();
                foreach (ProviderHandler handler in providers)
                {
                    try
                    {
                        IProvider provider = handler.Provider;
                        ITfsIntegrationJob integrationJob = provider.GetService(typeof(ITfsIntegrationJob)) as ITfsIntegrationJob;
                        if (null != integrationJob)
                        {
                            m_loadedJobs.Add(integrationJob);
                            if (TfsIntegrationJobsConfiguration.ConfiguredJobs.ContainsKey(integrationJob.ReferenceName))
                            {
                                if (TfsIntegrationJobsConfiguration.ConfiguredJobs[integrationJob.ReferenceName].Enabled)
                                {
                                    jobAndConfig[integrationJob] = TfsIntegrationJobsConfiguration.ConfiguredJobs[integrationJob.ReferenceName];
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TraceManager.TraceError(e.ToString());

                        string failureMessage = string.Format(
                            "A failure occurred while trying to load the TfsIntegrationJob in: {0}.{1}  Exception: {2}",
                            handler.ProviderName, Environment.NewLine, e.Message);
                        this.EventLog.WriteEntry(failureMessage, EventLogEntryType.Error, 0, 0);
                    }
                }

                m_jobScheduler = new JobScheduler(jobAndConfig);
                m_jobScheduler.Start();
            }
            catch (Exception e)
            {
                TraceManager.TraceError(e.ToString());
                this.EventLog.WriteEntry(
                    String.Format("{0} error starting: {1}", this.ServiceName, e.Message), 
                    EventLogEntryType.Error, 0, 0);
            }
        }

        protected override void OnStop()
        {
            try
            {
                m_jobScheduler.Stop();

                base.OnStop();

                this.EventLog.WriteEntry(String.Format("{0} stopped.", this.ServiceName), EventLogEntryType.Information, 0, 0);
            }
            catch (Exception e)
            {
                TraceManager.TraceError(e.ToString());
                this.EventLog.WriteEntry(
                    String.Format("{0} stopped with an exception: {1}", this.ServiceName, e.Message), 
                    EventLogEntryType.Error, 0, 0);
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                m_jobScheduler.Stop();

                base.OnShutdown();
            }
            catch (Exception e)
            {
                TraceManager.TraceError(e.ToString());
                this.EventLog.WriteEntry(
                    String.Format("{0} stopped with an exception: {1}", this.ServiceName, e.Message), 
                    EventLogEntryType.Error, 0, 0);
            }
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }
    }
}
