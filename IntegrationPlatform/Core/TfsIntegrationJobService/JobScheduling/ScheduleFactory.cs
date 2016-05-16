// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    class ScheduleFactory
    {
        public static bool TryCreateSchedule(ITfsIntegrationJob job, Job configuration, out ISchedule createdSchedule)
        {
            createdSchedule = null;
            bool retVal = false;

            if (configuration.Enabled)
            {
                try
                {
                    switch (configuration.Trigger.Option)
                    {
                        case TriggerOption.IntervalBased:
                            TraceManager.TraceInformation(string.Format(
                                "Created interval-based job schedule for job '{0}'", job.FriendlyName));
                            createdSchedule = new IntervalBasedSchedule(job, configuration);
                            retVal = true;
                            break;
                        case TriggerOption.TimeBased:
                            TraceManager.TraceInformation(string.Format(
                                "Created time-based job schedule for job '{0}'.", job.FriendlyName));
                            createdSchedule = new TimeBasedSchedule(job, configuration);
                            retVal = true;
                            break;
                        default:
                            TraceManager.TraceError(string.Format(
                                "'{0}' is not a supported job trigger type.", configuration.Trigger.Option.ToString()));
                            break;
                    }
                }
                catch (Exception e)
                {
                    TraceManager.TraceError(e.ToString());
                }
            }

            return retVal;
        }
    }
}
