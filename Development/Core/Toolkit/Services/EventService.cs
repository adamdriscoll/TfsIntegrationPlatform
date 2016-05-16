// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class EventService : IServiceProvider
    {
        [Flags]
        public enum EventTrackingMode
        {
            NotDefined = 0,
            Counter = 1,
            ElapseTime = Counter << 1,
            CounterAndElapseTime = Counter | ElapseTime,
        }

        private class EventTrackingCache
        {
            public string FriendlyName { get; set; }
            public DateTime? StartTime { get; set; }        
            public long? Counter { get; set; }
            public EventTrackingMode Mode { get; set; }

            public EventTrackingCache(string friendlyName, DateTime? startTime, long? counter, EventTrackingMode mode)
            {
                FriendlyName = friendlyName;
                StartTime = startTime;
                Counter = counter;
                Mode = mode;
            }
        }

        private object m_eventTrackCacheLock = new object();
        private Dictionary<Guid, EventTrackingCache> m_eventTrackCache = new Dictionary<Guid, EventTrackingCache>();

        Guid m_sourceId;
        RuntimeSession m_session;

        public EventService(RuntimeSession session, Guid sourceId)
        {
            m_session = session;
            m_sourceId = sourceId;
        }

        /// <summary>
        /// Provides method to get the service of current object.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(EventService)))
            {
                return this;
            }
            return null;
        }

        public void OnMigrationWarning(VersionControlEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        public void StartTrackingEvent(Guid eventReferenceName, string eventFriendlyName, EventTrackingMode trackingMode)
        {
            DateTime? startTime = null;
            long? counter = null;
            switch (trackingMode)
            {
                case EventTrackingMode.Counter:
                    counter = 0;
                    break;
                case EventTrackingMode.ElapseTime:
                    startTime = DateTime.Now;
                    break;
                case EventTrackingMode.CounterAndElapseTime:
                    counter = 0;
                    startTime = DateTime.Now;
                    break;
                case EventTrackingMode.NotDefined:
                default:
                    return;
            }

            lock (m_eventTrackCacheLock)
            {
                if (m_eventTrackCache.ContainsKey(eventReferenceName))
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot start tracking the same event \"{0}\" twice.", eventReferenceName));
                }

                m_eventTrackCache.Add(eventReferenceName, new EventTrackingCache(eventFriendlyName, startTime, counter, trackingMode));
            }
        }

        public void IncrementCachedEventCounter(Guid eventReferenceName)
        {
            lock (m_eventTrackCacheLock)
            {
                Debug.Assert(m_eventTrackCache.ContainsKey(eventReferenceName),
                    string.Format("Event \"{0}\" is not being tracked.", eventReferenceName));

                EventTrackingCache cache = m_eventTrackCache[eventReferenceName];
                if ((cache.Mode & EventTrackingMode.Counter) != EventTrackingMode.Counter
                    || !cache.Counter.HasValue)
                {
                    return;
                }

                cache.Counter++;
            }
        }

        public void FinishTrackingEvent(Guid eventReferenceName)
        {
            lock (m_eventTrackCacheLock)
            {
                if (!m_eventTrackCache.ContainsKey(eventReferenceName))
                {
                    return;
                }

                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    RTSessionRun rtSessionRun = context.RTSessionRunSet.Where
                        (s => s.Id == m_session.InternalSessionRunId).First<RTSessionRun>();
                    Debug.Assert(rtSessionRun != null,
                        string.Format("Cannot find session run with Id ({0}) to track event", m_session.InternalSessionRunId));

                    EventTrackingCache cache = m_eventTrackCache[eventReferenceName];
                    RTGeneralPerformanceData perfData =
                        (from p in context.RTGeneralPerformanceDataSet
                         where p.RuntimeSessionGroupRun.Id == rtSessionRun.SessionGroupRun.Id
                         && p.SessionUniqueId.Equals(rtSessionRun.Config.SessionUniqueId)
                         && p.SourceUniqueId.Equals(m_sourceId)
                         && p.CriterionReferenceName.Equals(eventReferenceName)
                         select p).First<RTGeneralPerformanceData>();

                    if (null == perfData)
                    {
                        perfData = RTGeneralPerformanceData.CreateRTGeneralPerformanceData(
                            0, rtSessionRun.Config.SessionUniqueId, m_sourceId, eventReferenceName, cache.FriendlyName);
                        perfData.RuntimeSessionGroupRun = rtSessionRun.SessionGroupRun;
                    }
                    
                    perfData.PerfCounter = cache.Counter;
                    perfData.PerfStartTime = cache.StartTime;
                    perfData.PerfFinishTime = DateTime.Now;
                    
                    context.TrySaveChanges();
                }

                m_eventTrackCache.Remove(eventReferenceName);
            }
        }
    }
}