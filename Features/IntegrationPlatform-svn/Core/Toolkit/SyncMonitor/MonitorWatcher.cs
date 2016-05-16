// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.SyncMonitor
{
    internal class MonitorWatcher
    {
        private bool m_useTraceManager;
        private bool m_verbose;
        private int m_pollIntervalSeconds = -1;

        private Thread m_WorkerThread = null;
        private bool m_stopped;
        private ManualResetEvent m_stoppedEvent;

        internal MonitorWatcher(bool useTraceManager, bool verbose) : this (useTraceManager, verbose, -1)
        {
        }

        internal MonitorWatcher(bool useTraceManager, bool verbose, int pollIntervalSeconds)
        {
            m_verbose = verbose;
            m_useTraceManager = useTraceManager;
            m_pollIntervalSeconds = pollIntervalSeconds;
            m_stopped = true;
            m_stoppedEvent = new ManualResetEvent(false);
        }

        internal void Start()
        {
            if (this.m_WorkerThread != null && this.m_WorkerThread.IsAlive)
            {
                // The worker thread is still running from the last call to Start, so don't start it again
                LogVerbose("MonitorWatcher.Start() returning without starting new thread; existing thread already running");
                return;
            }

            m_stopped = false;
            this.m_WorkerThread = new Thread(this.Worker);
            this.m_WorkerThread.IsBackground = true;
            this.m_WorkerThread.Start();
            this.m_WorkerThread.Name = MigrationToolkitResources.SyncMonitorThreadName;

            LogVerbose(String.Format(CultureInfo.InvariantCulture, "MonitorWatcher: Successfully started {0}", MigrationToolkitResources.SyncMonitorThreadName));
        }

        internal void Stop()
        {
            if (!m_stopped)
            {
                m_stopped = true;
                m_stoppedEvent.Set();

                if (this.m_WorkerThread != null)
                {
                    LogVerbose(String.Format(CultureInfo.InvariantCulture, "Waiting for {0} to stop ...", MigrationToolkitResources.SyncMonitorThreadName));

                    if (!this.m_WorkerThread.Join(30000))
                    {
                        LogWarning(String.Format(CultureInfo.InvariantCulture, "Timed out waiting for {0} to stop", MigrationToolkitResources.SyncMonitorThreadName));
                    }
                    this.m_WorkerThread = null;
                }
            }
        }

        private void Worker()
        {
            Dictionary<string, Endpoint> previousActiveEndpoints = null;
            while (!m_stopped)
            {
                try
                {
                    Dictionary<string, Endpoint> activeEndpoints;
                    using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                    {
                        // Monitor all currently active endpoints plus any other Endpoints that were active during the 
                        // previous monitoring cycle so that the final data for those Endpoints is recorded.
                        activeEndpoints = GetActiveEndpoints(context);

                        // Poll each active endpoint
                        foreach (Endpoint endpoint in activeEndpoints.Values)
                        {
                            if (m_stopped)
                            {
                                break;
                            }
                            try
                            {
                                endpoint.Poll();
                            }
                            catch (Exception ex)
                            {
                                LogError(String.Format("An unexpected error occurred polling migration source '{0}': {1}",
                                    endpoint.FriendlyName, ex.ToString()));
                            }
                        }

                        /* TODO: Currently, including this caode causes an ObjectDisposedException on call
                            * to RuntimeEntityModel.TrySaveChanges().   Ask if this is expected and if there's a way around it.
                            * Perhaps LogSuccessfulCompletion could save row to POLL_LATENCY bypassing RuntimeEntityModel.
                        if (previousActiveEndpoints != null)
                        {
                            foreach (Endpoint previousActiveEndpoint in previousActiveEndpoints.Values)
                            {
                                if (!activeEndpoints.ContainsKey(previousActiveEndpoint.UniqueId))
                                {
                                    previousActiveEndpoint.LogSuccessfulCompletion();
                                }
                            }
                        }
                            */

                        previousActiveEndpoints = activeEndpoints;
                    }
                }
                catch (ThreadAbortException ex)
                {
                    LogWarning(String.Format(CultureInfo.InvariantCulture,
                        "Thread abort exception received. Assuming normal shutdown:\n{0}", ex.Message));
                    return;
                }
                catch (Exception ex)
                {
                    LogError(String.Format("An unexpected error occured whilst monitoring connector: {0}", ex.ToString()));
                }

                if (m_pollIntervalSeconds > 0)
                {
                    m_stoppedEvent.WaitOne(1000 * m_pollIntervalSeconds);
                }
                else
                {
                    // When m_pollIntervalSeconds is -1, the caller of MonitorWatcher is performing the schedule, so we just run once then exit the thread
                    break;
                }
            }
        }

         /// <summary>
        /// Get a list of migration sources and their high water marks from the database
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Endpoint> GetActiveEndpoints(RuntimeEntityModel context)
        {
            // TODO: Once a session completes, we should report one more row in LATENCY_DATA that shows that there is no
            // longer any backlog.    This means that we need to save the list of endPoints from the previous call to EndPoints
            // and include them as well as the ones that are currently active.

            int sessionsBeingMonitored = 0;
            Dictionary<string, Endpoint> endpoints = new Dictionary<string, Endpoint>();

            // Find all the sessions that are not in the Completed State
            var sessionQuery =
                (from rtSession in context.RTSessionSet
                 where rtSession.State != (int)BusinessModelManager.SessionStateEnum.Completed
                 select rtSession);

            foreach (RTSession rtSession in sessionQuery)
            {
                // For a uni-directional migration or sync, we only want to monitor the source side
                // so we need to determine the WorkflowType
                rtSession.SessionGroupReference.Load();
                Debug.Assert(rtSession.SessionGroup != null);
                bool isUniDirectional = IsSessionUniDirectional(context, rtSession.SessionGroup);

                rtSession.LeftMigrationSourceReference.Load();
                Debug.Assert(rtSession.LeftMigrationSource != null);
                rtSession.RightMigrationSourceReference.Load();
                Debug.Assert(rtSession.RightMigrationSource != null);

                bool sessionEndpointAdded = false;
                bool isRightMigrationSource = false;
                RTMigrationSource[] migrationSources;


                if (isUniDirectional)
                {
                    migrationSources = new RTMigrationSource[1] { rtSession.LeftMigrationSource};
                }
                else
                {
                    migrationSources = new RTMigrationSource[2] { rtSession.LeftMigrationSource, rtSession.RightMigrationSource };                        
                }
                
                foreach (RTMigrationSource migrationSource in migrationSources)
                {
                    try
                    {
                        if (migrationSource == null)
                        {
                            LogError(String.Format(CultureInfo.InvariantCulture,
                                "Session {0} found with a null value for the {1} migration source (ignoring)",
                                rtSession.SessionUniqueId, isRightMigrationSource ? "right" : "left"));
                            continue;
                        }
                        LogVerbose(String.Format(CultureInfo.InvariantCulture,
                            "Found active migration source '{0}' for session with Id: {1}",
                            migrationSource.FriendlyName, rtSession.SessionUniqueId));

                        RTMigrationSource peerMigrationSource = isRightMigrationSource ?
                            rtSession.LeftMigrationSource : rtSession.RightMigrationSource;
                        Endpoint endpoint = new Endpoint(this, context, rtSession, migrationSource, isRightMigrationSource, peerMigrationSource);
                        if (endpoints.ContainsKey(endpoint.UniqueId))
                        {
                            LogWarning("Skipping endpoint with same Id as endpoint previously found: " + endpoint.UniqueId);
                            continue;
                        }
                        endpoints.Add(endpoint.UniqueId, endpoint);

                        if (!sessionEndpointAdded)
                        {
                            sessionsBeingMonitored++;
                            sessionEndpointAdded = true;
                        }

                        LogVerbose(String.Format(CultureInfo.InvariantCulture,
                            "Successfully initialized monitoring for migration source '{0}'",
                            migrationSource.FriendlyName));
                    }
                    catch (NotImplementedException)
                    {
                        // It's OK for an adapter to not implement the ISyncMonitorProvider interface
                        LogInfo(String.Format(CultureInfo.InvariantCulture,
                            "Skipping monitoring for endpoint '{0}' because the adapter does not implement the ISyncMonitorProvider interface",
                            migrationSource.FriendlyName));
                    }
                    catch (Exception ex)
                    {
                        LogWarning(String.Format(CultureInfo.InvariantCulture,
                            "Unable to setup monitoring for endpoint '{0}': {1}",
                            migrationSource.FriendlyName, ex.ToString()));
                    }
                    // Will be true 2nd time through foreach for the session.RightMigrationSource
                    isRightMigrationSource = true;
                }
            }

            if (endpoints.Count > 0)
            {
                LogInfo(String.Format(CultureInfo.InvariantCulture,
                    MigrationToolkitResources.SyncMonitorCollectingBacklogData,
                    endpoints.Count, sessionsBeingMonitored));
            }
            else
            {
                LogVerbose(MigrationToolkitResources.SyncMonitorNoActiveSessions);
            }

            return endpoints;
        }

        private bool IsSessionUniDirectional(RuntimeEntityModel context, RTSessionGroup rtSessionGroup)
        {            
            var sessionGroupConfigQuery = 
                    (from sg in context.RTSessionGroupConfigSet
                    where sg.SessionGroup.GroupUniqueId.Equals(rtSessionGroup.GroupUniqueId)
                    orderby sg.Id descending
                    select sg).First();

            WorkFlowType workFlowType = new WorkFlowType(sessionGroupConfigQuery.WorkFlowType);
            return workFlowType.DirectionOfFlow == DirectionOfFlow.Unidirectional;
        }

        internal void LogError(string message)
        {
            string syncMonitorMessage = String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.SyncMonitorError, message);
            if (m_useTraceManager)
            {
                TraceManager.TraceError(syncMonitorMessage);
            }
            else
            {
                Console.WriteLine(syncMonitorMessage);
            }
        }

        internal void LogWarning(string message)
        {
            string syncMonitorMessage = String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.SyncMonitorWarning, message);
            if (m_useTraceManager)
            {
                TraceManager.TraceWarning(syncMonitorMessage);
            }
            else
            {
                Console.WriteLine(syncMonitorMessage);
            }
        }

        internal void LogInfo(string message)
        {
            string syncMonitorMessage = String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.SyncMonitorInfo, message);
            if (m_useTraceManager)
            {
                TraceManager.TraceInformation(syncMonitorMessage);
            }
            else
            {
                Console.WriteLine(syncMonitorMessage);
            }
        }

        internal void LogVerbose(string message)
        {
            if (m_verbose)
            {
                LogInfo(message);
            }
        }
    }
}
