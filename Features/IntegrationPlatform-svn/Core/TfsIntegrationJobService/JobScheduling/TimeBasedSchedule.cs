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
    class TimeBasedSchedule : ScheduleBase
    {
        Thread m_jobSchedulingThread;
        private long[] m_jobKickOffTimeOfDayTicksRange = new long[2];
        private const int PollInterval = 500; // in millisec's
        private DateTime m_lastJobStartTime = DateTime.MinValue;
        
        public TimeBasedSchedule(ITfsIntegrationJob job, Job configuration)
            : base(job, configuration)
        {
            DateTime configuredDateTime = DateTime.Parse(configuration.Trigger.Setting);
            CalcKickOffTimeRange(configuredDateTime, m_jobKickOffTimeOfDayTicksRange);
        }

        public override void RunSchedule()
        {
            m_jobSchedulingThread = new Thread(ScheduleByTime);
            m_jobSchedulingThread.Name = m_job.FriendlyName;
            m_jobSchedulingThread.IsBackground = true;
            m_jobSchedulingThread.Start();
        }

        private void CalcKickOffTimeRange(DateTime configuredDateTime, long[] jobKickOffTimeOfDayTicksRange)
        {
            // kick-off time search window is [configured_time, configured_time + 5 sec)
            jobKickOffTimeOfDayTicksRange[0] = configuredDateTime.TimeOfDay.Ticks;
            jobKickOffTimeOfDayTicksRange[1] = jobKickOffTimeOfDayTicksRange[0] + TimeSpan.TicksPerSecond * 5;
        }
        
        private void ScheduleByTime()
        {
            m_job.Initialize(m_configuration);
            while (!StopRequested)
            {
                if (TimeToKickOffJob)
                {
                    try
                    {
                        TraceManager.TraceInformation(string.Format("Starting job: {0} ...\n", JobName));
                        m_lastJobStartTime = DateTime.Now;

                        Thread jobThread = new Thread(m_job.Run);
                        jobThread.Name = m_job.FriendlyName;
                        jobThread.IsBackground = true;
                        jobThread.Start();
                    }
                    catch (Exception e)
                    {
                        TraceManager.TraceError(e.ToString());
                    }
                }

                Thread.Sleep(PollInterval);
            }
        }

        private bool TimeToKickOffJob
        {
            get
            {
                long timeOfDayNowTicks = DateTime.Now.TimeOfDay.Ticks;
                return CanKickOffJob(timeOfDayNowTicks);
            }
        }

        private bool CanKickOffJob(long timeOfDayTicks)
        {
            if (!IsTimeInRange(timeOfDayTicks))
            {
                return false;
            }

            if (m_lastJobStartTime.AddHours(23).CompareTo(DateTime.Now) < 0) // last job starting time was 23 hours ago 
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsTimeInRange(long timeOfDayTicks)
        {
            return (timeOfDayTicks >= m_jobKickOffTimeOfDayTicksRange[0] && timeOfDayTicks < m_jobKickOffTimeOfDayTicksRange[1]);
        }
    }
}
