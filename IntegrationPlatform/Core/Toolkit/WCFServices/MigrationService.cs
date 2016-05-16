// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    // Sticking with the default value of UseSynchronizationContext will lock up the UI thread when self-hosted in a smart client
    [System.ServiceModel.ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext=false)]
    public class MigrationService : IMigrationService
    {
        private static MigrationService s_instance;
        private Dictionary<Guid, SyncOrchestrator> m_runningSessionGroups = new Dictionary<Guid, SyncOrchestrator>();
        private object m_runningSessionGroupsLock = new object();

        private Dictionary<Guid, SessionGroupInitializationStatus> m_sessionGroupInitializationStatus =
            new Dictionary<Guid, SessionGroupInitializationStatus>();

        public static MigrationService GetInstance()
        {
            if (s_instance == null)
            {
                TraceManager.TraceInformation("MigrationService: GetInstance: Creating new MigrationService instance");
                s_instance = new MigrationService();
            }
            return s_instance;
        }

        private MigrationService()
        {
        }

        public void ShutDownService()
        {
            try
            {
                Dictionary<Guid, SyncOrchestrator>.KeyCollection runningSessionGroupsGuids = null;
                lock (m_runningSessionGroupsLock)
                {
                    runningSessionGroupsGuids = m_runningSessionGroups.Keys;
                }

                foreach (Guid sessionGroupUniqueId in runningSessionGroupsGuids)
                {
                    StopSessionGroup(sessionGroupUniqueId);
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
            }
        }

        public void StartSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                lock (m_runningSessionGroupsLock)
                {
                    TraceManager.TraceInformation("StartSessionGroup: Enter with sessionGroupUniqueId: {0}", sessionGroupUniqueId);

                    PollLivenessAndUpdateHostedSessionGroups();

                    // Create a SyncOrchestrator if we don't already have one servicing the referenced SessionGroup
                    if (!m_runningSessionGroups.ContainsKey(sessionGroupUniqueId))
                    {
                        // mark session group init status to be Starting
                        if (!m_sessionGroupInitializationStatus.ContainsKey(sessionGroupUniqueId))
                        {
                            m_sessionGroupInitializationStatus.Add(sessionGroupUniqueId, SessionGroupInitializationStatus.Initializing);
                        }
                        else
                        {
                            m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.Initializing;
                        }

                        if (IsSessionGroupObsoleteOrDeleted(sessionGroupUniqueId))
                        {
                            TraceManager.TraceError(MigrationToolkitResources.ErrorRestartDeletedSessionGroup,
                                                    sessionGroupUniqueId.ToString());
                            m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;

                            return;
                        }
                        else if (IsSessionGroupOneTimeCompleted(sessionGroupUniqueId))
                        {
                            TraceManager.TraceError(MigrationToolkitResources.ErrorRestartOneTimeCompleteSessionGroup,
                                                    sessionGroupUniqueId.ToString());
                            m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;

                            return;
                        }

                        TraceManager.TraceInformation("StartSessionGroup: Creating new SyncOrchestrator");
                        BusinessModelManager businessModelManager = new BusinessModelManager();

                        Configuration configuration = businessModelManager.LoadConfiguration(sessionGroupUniqueId);

                        // exclude sessions that are started in standalone process
                        Guid[] standaloneSessionsInGroup = new Guid[0];
                        using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                        {
                            int StartedInStandaloneProcessState = (int)BusinessModelManager.SessionStateEnum.StartedInStandaloneProcess;
                            var sessionQuery = from s in context.RTSessionSet
                                               where s.State == StartedInStandaloneProcessState
                                                  && s.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                                               select s.SessionUniqueId;

                            if (sessionQuery.Count() > 0)
                            {
                                standaloneSessionsInGroup = sessionQuery.ToArray();
                            }
                        }
                        ExcludeSessionFromGroupInConfig(configuration, standaloneSessionsInGroup);

                        SyncOrchestrator syncOrchestrator = new SyncOrchestrator(configuration);

                        // phase1 light-weight initialization
                        syncOrchestrator.ConstructPipelines();

                        // phase2 heavy-weight initialization
                        syncOrchestrator.InitializePipelines();

                        // clean-up the sync orchestration status and command queue, in case
                        // previous session process aborted
                        syncOrchestrator.CleanUpSyncOrchStatus();

                        TraceManager.TraceInformation("StartSessionGroup: Starting SyncOrchestrator; now {0} running sessions", m_runningSessionGroups.Count);
                        syncOrchestrator.Start();

                        m_runningSessionGroups.Add(sessionGroupUniqueId, syncOrchestrator);
                        m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.Initialized;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
                m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;
            }
        }

        public void StartSingleSessionInSessionGroup(Guid sessionGroupUniqueId, Guid sessionUniqueId)
        {
            try
            {
                lock (m_runningSessionGroupsLock)
                {
                    PollLivenessAndUpdateHostedSessionGroups();

                    // mark session group init status to be Starting
                    if (!m_sessionGroupInitializationStatus.ContainsKey(sessionGroupUniqueId))
                    {
                        m_sessionGroupInitializationStatus.Add(sessionGroupUniqueId, SessionGroupInitializationStatus.Initializing);
                    }
                    else
                    {
                        m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.Initializing;
                    }

                    if (IsSessionGroupObsoleteOrDeleted(sessionGroupUniqueId))
                    {
                        TraceManager.TraceError(MigrationToolkitResources.ErrorRestartDeletedSessionGroup,
                                                sessionGroupUniqueId.ToString());
                        m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;

                        return;
                    }
                    else if (IsSessionGroupOneTimeCompleted(sessionGroupUniqueId))
                    {
                        TraceManager.TraceError(MigrationToolkitResources.ErrorRestartOneTimeCompleteSessionGroup,
                                                sessionGroupUniqueId.ToString());
                        m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;
                        return;
                    }

                    TraceManager.TraceInformation("StartSingleSessionInSessionGroup: Enter with sessionGroupUniqueId: {0}, sessionUniqueId: {1}",
                                                  sessionGroupUniqueId.ToString(),
                                                  sessionUniqueId.ToString());

                    using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                    {
                        var sessionQuery = from s in context.RTSessionSet
                                           where s.SessionUniqueId.Equals(sessionUniqueId)
                                              && s.SessionGroup.GroupUniqueId.Equals(sessionGroupUniqueId)
                                           select s;
                        if (sessionQuery.Count() == 0)
                        {
                            return;
                        }

                        sessionQuery.First().State = (int)BusinessModelManager.SessionStateEnum.StartedInStandaloneProcess;
                        context.TrySaveChanges();
                    }

                    // Create a SyncOrchestrator if we don't already have one servicing the referenced SessionGroup
                    if (!m_runningSessionGroups.ContainsKey(sessionGroupUniqueId))
                    {
                        TraceManager.TraceInformation("StartSessionGroup: Creating new SyncOrchestrator");
                        BusinessModelManager businessModelManager = new BusinessModelManager();

                        Configuration configuration = businessModelManager.LoadConfiguration(sessionGroupUniqueId);

                        List<Guid> sessionIdsToExclude = new List<Guid>();
                        foreach (var session in configuration.SessionGroup.Sessions.Session)
                        {
                            var sessionId = new Guid(session.SessionUniqueId);
                            if (sessionId.Equals(sessionUniqueId))
                            {
                                continue;
                            }

                            sessionIdsToExclude.Add(sessionId);
                        }


                        // extract only the session we need
                        ExcludeSessionFromGroupInConfig(configuration, sessionIdsToExclude.ToArray());

                        SyncOrchestrator syncOrchestrator = new SyncOrchestrator(configuration);

                        // phase1 light-weight initialization
                        syncOrchestrator.ConstructPipelines();

                        // phase2 heavy-weight initialization
                        syncOrchestrator.InitializePipelines();

                        syncOrchestrator.CleanUpSyncOrchStatus();

                        TraceManager.TraceInformation("StartSessionGroup: Starting SyncOrchestrator; now {0} running sessions", m_runningSessionGroups.Count);
                        syncOrchestrator.Start();

                        m_runningSessionGroups.Add(sessionGroupUniqueId, syncOrchestrator);
                        m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.Initialized;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
                m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;
            }
        }

        private bool IsSessionGroupOneTimeCompleted(Guid sessionGroupUniqueId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionGroupQuery = from sg in context.RTSessionGroupSet
                                   where sg.GroupUniqueId.Equals(sessionGroupUniqueId)
                                   select sg;

                if (sessionGroupQuery.Count() != 1)
                {
                    throw new MigrationException("{0} rows are found for Session Group {1}",
                        sessionGroupQuery.Count().ToString(), sessionGroupUniqueId.ToString());
                }

                return sessionGroupQuery.First().State == (int)BusinessModelManager.SessionStateEnum.OneTimeCompleted;
            }
        }

        private bool IsSessionGroupObsoleteOrDeleted(Guid sessionGroupUniqueId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionGroupQuery = from sg in context.RTSessionGroupSet
                                        where sg.GroupUniqueId.Equals(sessionGroupUniqueId)
                                        select sg;

                if (sessionGroupQuery.Count() == 0)
                {
                    return true;
                }
                else if (sessionGroupQuery.Count() != 1)
                {
                    throw new MigrationException("{0} rows are found for Session Group {1}",
                        sessionGroupQuery.Count().ToString(), sessionGroupUniqueId.ToString());
                }

                return sessionGroupQuery.First().State == (int)BusinessModelManager.SessionStateEnum.MarkedForDeletion;
            }
        }

        private void ExcludeSessionFromGroupInConfig(Configuration configuration, Guid[] sessionIdsToExclude)
        {
            if (sessionIdsToExclude.Length == 0)
            {
                return;
            }

            // extract only the session we need
            List<Session> sessionsToExlude = new List<Session>();
            foreach (var bzSession in configuration.SessionGroup.Sessions.Session)
            {
                Guid sessionId = new Guid(bzSession.SessionUniqueId);

                foreach (Guid id in sessionIdsToExclude)
                {
                    if (sessionId.Equals(id))
                    {
                        sessionsToExlude.Add(bzSession);
                        break;
                    }
                }
            }

            foreach (var bzSession in sessionsToExlude)
            {
                configuration.SessionGroup.Sessions.Session.Remove(bzSession);
            }
        }

        public void StopSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                lock (m_runningSessionGroupsLock)
                {
                    SqlSyncStateManager stateManager = SqlSyncStateManager.GetInstance();
                    stateManager.AddCommand(sessionGroupUniqueId, PipelineSyncCommand.STOP);
                    m_runningSessionGroups.Remove(sessionGroupUniqueId);
                    if (m_sessionGroupInitializationStatus.ContainsKey(sessionGroupUniqueId))
                    {
                        m_sessionGroupInitializationStatus[sessionGroupUniqueId] = SessionGroupInitializationStatus.NotInitialized;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
            }
        }

        public void PauseSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                SqlSyncStateManager stateManager = SqlSyncStateManager.GetInstance();
                stateManager.AddCommand(sessionGroupUniqueId, PipelineSyncCommand.PAUSE);
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
            }
        }

        public void ResumeSessionGroup(Guid sessionGroupUniqueId)
        {
            try
            {
                SqlSyncStateManager stateManager = SqlSyncStateManager.GetInstance();
                stateManager.AddCommand(sessionGroupUniqueId, PipelineSyncCommand.RESUME);
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
			}
        }
        
        public List<Guid> GetRunningSessionGroups()
        {
            try
            {
                lock (m_runningSessionGroupsLock)
                {
                    PollLivenessAndUpdateHostedSessionGroups();
                    return this.m_runningSessionGroups.Keys.ToList();
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
                return new List<Guid>();
            }
        }

        public SessionGroupInitializationStatus GetSessionGroupInitializationStatus(Guid sessionGroupUniqueId)
        {
            try
            {
                if (m_sessionGroupInitializationStatus.ContainsKey(sessionGroupUniqueId))
                {
                    return m_sessionGroupInitializationStatus[sessionGroupUniqueId];
                }
                else
                {
                    return SessionGroupInitializationStatus.Unknown;
                }
            }
            catch (Exception e)
            {
                TraceManager.TraceException(e);
                return SessionGroupInitializationStatus.Unknown;
            } 
        }

        private void PollLivenessAndUpdateHostedSessionGroups()
        {               
            List<Guid> deadSessionGroupUniqueIds = new List<Guid>();
            foreach (Guid sessionGroupUniqueId in m_runningSessionGroups.Keys)
            {
                if (!m_runningSessionGroups[sessionGroupUniqueId].IsAlive)
                {
                    deadSessionGroupUniqueIds.Add(sessionGroupUniqueId);
                }
            }

            foreach (Guid uniqueId in deadSessionGroupUniqueIds)
            {
                m_runningSessionGroups.Remove(uniqueId);
            }
        }
    }
}
