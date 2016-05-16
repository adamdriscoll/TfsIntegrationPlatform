// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    abstract class ScheduleBase : ISchedule
    {
        protected ITfsIntegrationJob m_job;
        protected Job m_configuration;
        protected bool m_stopRequested = false;
        protected object m_stopRequestedLock = new object();

        public ScheduleBase(ITfsIntegrationJob job, Job configuration)
        {
            this.m_job = job;
            this.m_configuration = configuration;
        }

        public abstract void RunSchedule();

        public virtual void StopRunningJob()
        {
            m_job.Stop();

            StopRequested = true;
        }
        
        public virtual string JobName
        {
            get { return m_job.FriendlyName; }
        }

        /// <summary>
        /// Gets/sets the StopRequested flag
        /// </summary>
        protected bool StopRequested
        {
            get
            {
                lock (m_stopRequestedLock)
                {
                    return m_stopRequested;
                }
            }
            set
            {
                lock (m_stopRequestedLock)
                {
                    m_stopRequested = value;
                }
            }
        }
    }
}
