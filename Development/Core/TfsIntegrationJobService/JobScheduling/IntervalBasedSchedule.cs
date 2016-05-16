// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    class IntervalBasedSchedule : ScheduleBase
    {
        private Thread m_jobThread;
        private int m_intervalMilliSeconds;

        public IntervalBasedSchedule(ITfsIntegrationJob job, Job configuration)
            : base(job, configuration)
        {
            try
            {
                this.m_intervalMilliSeconds = checked(int.Parse(configuration.Trigger.Setting) * 1000);
            }
            catch (OverflowException e)
            {
                TraceManager.TraceError(e.ToString());
                TraceManager.TraceError(string.Format(
                    "Setting default interval time to {0} seconds", (int.MaxValue / 1000).ToString()));
                this.m_intervalMilliSeconds = int.MaxValue;
            }
        }

        public override void RunSchedule()
        {
            m_jobThread = new Thread(ScheduleByInterval);
            m_jobThread.Name = m_job.FriendlyName;
            m_jobThread.IsBackground = true;
            m_jobThread.Start();
        }

        public override void StopRunningJob()
        {
            base.StopRunningJob();
        }

        private void ScheduleByInterval()
        {
            m_job.Initialize(m_configuration);

            while (!StopRequested)
            {
                try
                {
                    TraceManager.TraceInformation(string.Format("Starting job: {0} ...\n", JobName));
                    m_job.Run();
                }
                catch (Exception e)
                {
                    TraceManager.TraceError(e.ToString());
                }

                TraceManager.TraceVerbose(string.Format("{0}: Waiting for '{1}' seconds", JobName, (m_intervalMilliSeconds / 1000).ToString()));
                Thread.Sleep(m_intervalMilliSeconds);
            }
        }        
    }
}
