// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.VC;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace MigrationConsole
{
    public struct SingleSessionStartInfo
    {
        private Guid m_sessionGroupId;
        private Guid m_sessionId;

        public Guid SessionGroupId
        {
            get { return m_sessionGroupId; }
        }
        
        public Guid SessionId
        {
            get { return m_sessionId; }
        }

        public SingleSessionStartInfo(Guid sessionGroupId, Guid sessionId)
        {
            m_sessionGroupId = sessionGroupId;
            m_sessionId = sessionId;
        }

    }

    public class MigrationApp
    {
        private bool m_launchActiveGroupsFromDB;
        private string m_configFileName;
        private CommandSwitch m_commandSwitch;
        private IMigrationService m_pipeProxy;
        private bool m_startSingleSession = false;
        private const int RunningSessionPollingIntervalMillisec = 5000;

        private enum DupConfigUserResponse
        {
            Unknown = 0,
            Update = 1,
            DoNotUpdate = 2,
            CreateNew = 3,
        }

        public MigrationApp( CommandSwitch commandSwitch)
        {
            Initialize(true, null, commandSwitch);
        }

        public MigrationApp(string configFileName, CommandSwitch commandSwitch)
        {
            Initialize(false, configFileName, commandSwitch);
        }

        internal SingleSessionStartInfo SingleSessionStartInfo
        {
            get;
            set;
        }

        internal bool StartSingleSession
        {
            get
            {
                return m_startSingleSession;
            }
            set
            {
                m_startSingleSession = value;
            }
        }

        private void Initialize(
            bool launchActiveGroupsFromDB,
            string configFileName,
            CommandSwitch commandSwitch)
        {
            m_launchActiveGroupsFromDB = launchActiveGroupsFromDB;
            m_configFileName = configFileName;
            m_commandSwitch = commandSwitch;
            InitializeProxy();
        }

        private void InitializeProxy()
        {
            m_pipeProxy = new MigrationServiceClient();
        }

        public void Start()
        {
            if (m_startSingleSession)
            {
                LaunchSingleSessionFromDB();
            }
            else if (m_launchActiveGroupsFromDB)
            {
                LaunchActiveGroupsFromDB();
            }
            else
            {
                LaunchFromConfigFile();
            }            
        }

        private void LaunchSingleSessionFromDB()
        {
            BusinessModelManager manager = new BusinessModelManager();
            List<Guid> runningSessionGroups = manager.GetActiveSessionGroupUniqueIds();

            // If we are self hosting, use the IMigrationService interface 
            // to start some sessions before trying to list the active ones.
            if (!runningSessionGroups.Contains(SingleSessionStartInfo.SessionGroupId))
            {
                return;
            }

            if (m_commandSwitch.CommandSwitchSet)
            {
                Configuration configuration = manager.LoadConfiguration(SingleSessionStartInfo.SessionGroupId);
                preAuthorizeRules(configuration);
            }

            m_pipeProxy.StartSingleSessionInSessionGroup(SingleSessionStartInfo.SessionGroupId, SingleSessionStartInfo.SessionId);

            while (true)
            {
                try
                {
                    runningSessionGroups.Clear();
                    runningSessionGroups.AddRange(m_pipeProxy.GetRunningSessionGroups());

                    // uncomment the following lines for richer debugging message
                    // Console.WriteLine(String.Format("Received {0} entries", runningSessionGroups.Count));
                    //foreach (Guid guid in runningSessionGroups)
                    //{
                    //    Console.WriteLine(guid.ToString());
                    //}

                    if (runningSessionGroups.Count() == 0)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    // Ugly, but this approach lets us start the test app 
                    // before the endpoint is ready to service requests.
                    Console.WriteLine("Error: {0}", e.Message);

                    // re-initialize proxy
                    InitializeProxy();
                }

                Thread.Sleep(RunningSessionPollingIntervalMillisec);
            }
        }

        /// <summary>
        /// Pre-authorize conflict resolution rules.
        /// </summary>
        private void preAuthorizeRules(Configuration config)
        {
            VCChangeToAddOnBranchSourceNotMappedAction ignoreUnmappedPath = new VCChangeToAddOnBranchSourceNotMappedAction();

            foreach (Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource source in config.SessionGroup.MigrationSources.MigrationSource)
            {
                // Todo, currently this is for TFS source only
                if ( m_commandSwitch.IgnoreUnmappedPath &&
                    (string.Equals(source.ProviderReferenceName, "2F82C6C4-BBEE-42fb-B3D0-4799CABCF00E", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(source.ProviderReferenceName, "FEBC091F-82A2-449e-AED8-133E5896C47A", StringComparison.OrdinalIgnoreCase)))
                {

                    foreach (Microsoft.TeamFoundation.Migration.BusinessModel.Session session in config.SessionGroup.Sessions.Session)
                    {
                        if (string.Equals(session.LeftMigrationSourceUniqueId, source.InternalUniqueId) ||
                            string.Equals(session.RightMigrationSourceUniqueId, source.InternalUniqueId))
                        {
                            ConflictManager.SaveNewResolutionRule(new Guid(session.SessionUniqueId),
                                new Guid(source.InternalUniqueId),
                                new VCPathNotMappedConflictType(),
                                ignoreUnmappedPath.NewRule("$/", m_commandSwitch.IgnoreUnmappedPathReason, new Dictionary<string, string>()));

                        }
                    }
                }
            }

        }

        private void LaunchActiveGroupsFromDB()
        {
            BusinessModelManager manager = new BusinessModelManager();
            List<Guid> runningSessionGroups = manager.GetActiveSessionGroupUniqueIds();

            if (runningSessionGroups.Count() == 0)
            {
                throw new NoActiveSessionGroupException();
            }

            if (m_commandSwitch.CommandSwitchSet)
            {
                Configuration configuration;
                foreach (Guid sessionGroupId in runningSessionGroups)
                {
                    configuration = manager.LoadConfiguration(sessionGroupId);
                    preAuthorizeRules(configuration);
                }
            }

            // If we are self hosting, use the IMigrationService interface 
            // to start some sessions before trying to list the active ones.
            foreach (Guid sessionGroupId in runningSessionGroups)
            {
                m_pipeProxy.StartSessionGroup(sessionGroupId);
            }

            while (true)
            {
                try
                {
                    runningSessionGroups.Clear();
                    runningSessionGroups.AddRange(m_pipeProxy.GetRunningSessionGroups());

                    // uncomment the following lines for richer debugging message
                    // Console.WriteLine(String.Format("Received {0} entries", runningSessionGroups.Count));
                    //foreach (Guid guid in runningSessionGroups)
                    //{
                    //    Console.WriteLine(guid.ToString());
                    //}

                    if (runningSessionGroups.Count() == 0 ||
                        AllRunningSessionGroupsAreInStoppedSyncState(runningSessionGroups.ToArray()))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    // Ugly, but this approach lets us start the test app 
                    // before the endpoint is ready to service requests.
                    Console.WriteLine("Error: {0}", e.Message);

                    // re-initialize proxy
                    InitializeProxy();
                }

                Thread.Sleep(RunningSessionPollingIntervalMillisec);
            }
        }

        private bool AllRunningSessionGroupsAreInStoppedSyncState(Guid[] runningSessionGroups)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int syncStateStopped = (int)PipelineState.Stopped;

                foreach (Guid sessionGroupUniqueId in runningSessionGroups)
                {
                    var groupNotInStoppedState =
                        from sg in context.RTSessionGroupSet
                        where sg.GroupUniqueId.Equals(sessionGroupUniqueId)
                           && sg.OrchestrationStatus != syncStateStopped
                        select sg.Id;

                    if (groupNotInStoppedState.Count() > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private DupConfigUserResponse GetUserDecisionOnDuplicateConfig()
        {
            Console.WriteLine("A configuration with the same Configuration UniqueId is found in the storage. You may choose one of the following options:");
            Console.WriteLine("  To update the configuration in the storage, type (U)pdate;");
            Console.WriteLine("  To create a new session with this configuration file, type (C)reate;");
            Console.WriteLine("  To discard this configuration file and use the version in the storage, type (D)iscard.");
            Console.WriteLine();
            Console.WriteLine("Please type your choice: ");

            string userResponseStr = Console.ReadLine();

            DupConfigUserResponse userResponse = DupConfigUserResponse.Unknown;

            if (string.IsNullOrEmpty(userResponseStr))
            {
                userResponse = DupConfigUserResponse.Unknown;
            }
            if ("Update".Equals(userResponseStr, StringComparison.InvariantCultureIgnoreCase)
                || "U".Equals(userResponseStr, StringComparison.InvariantCultureIgnoreCase))
            {
                userResponse = DupConfigUserResponse.Update;
            }
            else if ("Discard".Equals(userResponseStr, StringComparison.InvariantCultureIgnoreCase)
                || "D".Equals(userResponseStr, StringComparison.InvariantCultureIgnoreCase))
            {
                userResponse = DupConfigUserResponse.DoNotUpdate;
            }
            else if ("Create".Equals(userResponseStr, StringComparison.InvariantCultureIgnoreCase)
                || "C".Equals(userResponseStr, StringComparison.InvariantCultureIgnoreCase))
            {
                userResponse = DupConfigUserResponse.CreateNew;
            }
            else
            {
                userResponse = DupConfigUserResponse.Unknown;
            }

            return userResponse;
        }

        private void LaunchFromConfigFile()
        {
            try
            {
                Configuration config = Configuration.LoadFromFile(m_configFileName);
                SessionGroupConfigurationManager configSaver = new SessionGroupConfigurationManager(config);
                try
                {
                    configSaver.TrySave();
                }
                catch (DuplicateConfigurationException)
                {
                    DupConfigUserResponse userResponse = GetUserDecisionOnDuplicateConfig();
                    while (userResponse == DupConfigUserResponse.Unknown)
                    {
                        userResponse = GetUserDecisionOnDuplicateConfig();
                    }

                    switch (userResponse)
                    {
                        case DupConfigUserResponse.Update:
                            config.UniqueId = Guid.NewGuid().ToString();
                            configSaver.TrySave();
                            break;
                        case DupConfigUserResponse.CreateNew:
                            string configFileContent = File.ReadAllText(m_configFileName);
                            configFileContent = Configuration.ReGuidConfigXml(config, configFileContent);
                            config = Configuration.LoadFromXml(configFileContent);
                            configSaver = new SessionGroupConfigurationManager(config);
                            configSaver.TrySave();
                            break;
                        case DupConfigUserResponse.DoNotUpdate:
                            break;
                        case DupConfigUserResponse.Unknown:
                        default:
                            throw new InvalidOperationException("Unknown user response.");
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                List<Guid> runningSessionGroups = new List<Guid>(1);
                runningSessionGroups.Add(config.SessionGroupUniqueId);

                if (m_commandSwitch.CommandSwitchSet)
                {
                    preAuthorizeRules(config);
                }

                // kickoff the session group
                m_pipeProxy.StartSessionGroup(config.SessionGroupUniqueId);
                
                while (true)
                {
                    try
                    {
                        runningSessionGroups.Clear();
                        runningSessionGroups.AddRange(m_pipeProxy.GetRunningSessionGroups());

                        // uncomment the following lines for richer debugging message
                        //Console.WriteLine(String.Format("Received {0} entries", runningSessionGroups.Count));
                        //foreach (Guid guid in runningSessionGroups)
                        //{
                        //    Console.WriteLine(guid.ToString());
                        //}

                        if (runningSessionGroups.Count == 0 ||
                            !runningSessionGroups.Contains(config.SessionGroupUniqueId)
                            || AllRunningSessionGroupsAreInStoppedSyncState(new Guid[] {config.SessionGroupUniqueId}))
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        // Ugly, but this approach lets us start the test app 
                        // before the endpoint is ready to service requests.
                        Console.WriteLine("Error: {0}", e.Message);

                        // re-initialize proxy
                        InitializeProxy();
                    }

                    Thread.Sleep(RunningSessionPollingIntervalMillisec);
                }
            }
            catch (System.Data.UpdateException updateEx)
            {
                if (updateEx.InnerException != null
                    && updateEx.InnerException is System.Data.SqlClient.SqlException)
                {
                    if (updateEx.InnerException.Message.Contains("chkSingleUsageOfMigrationSourceInSessions"))
                    {
                        Program.Trace(MigrationConsoleResources.ErrorMigrationSourceUsedMultipleTimes);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        internal static void MergebackStandAloneSessionInGroup(Guid s_sessionGroupUniqueId, Guid s_sessionUniqueId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int StartedInStandaloneProcessState = (int)BusinessModelManager.SessionStateEnum.StartedInStandaloneProcess;
                var sessionQuery = from s in context.RTSessionSet
                                   where s.State == StartedInStandaloneProcessState
                                      && s.SessionGroup.GroupUniqueId.Equals(s_sessionGroupUniqueId)
                                      && s.SessionUniqueId.Equals(s_sessionUniqueId)
                                   select s;

                if (sessionQuery.Count() > 0)
                {
                    sessionQuery.First().State = (int)BusinessModelManager.SessionStateEnum.Initialized;
                }

                context.TrySaveChanges();
            }

            TraceManager.TraceInformation("Session ({0}) has been merged back to parent SessionGroup ({1})",
                                          s_sessionUniqueId.ToString(),
                                          s_sessionGroupUniqueId.ToString());
        }
    }
}
