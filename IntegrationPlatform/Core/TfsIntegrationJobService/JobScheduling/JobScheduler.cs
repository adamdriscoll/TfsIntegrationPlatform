// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    internal class JobScheduler
    {
        private Dictionary<ITfsIntegrationJob, Job> m_jobAndConfig;
        private List<ISchedule> m_scheduledJobs = new List<ISchedule>();

        public JobScheduler(Dictionary<ITfsIntegrationJob, Job> jobAndConfig)
        {
            this.m_jobAndConfig = jobAndConfig;

            foreach (var jobConfig in jobAndConfig)
            {
                ISchedule schedule;
                if (ScheduleFactory.TryCreateSchedule(jobConfig.Key, jobConfig.Value, out schedule))
                {
                    m_scheduledJobs.Add(schedule);
                }
            }
        }

        internal void Start()
        {
            foreach (ISchedule schedule in m_scheduledJobs)
            {
                schedule.RunSchedule();
            }
        }

        internal void Stop()
        {
            foreach (ISchedule schedule in m_scheduledJobs)
            {
                schedule.StopRunningJob();
            }
        }
    }
}
