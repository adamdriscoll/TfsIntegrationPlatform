// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.SyncMonitor
{
    internal class Endpoint
    {
        private MonitorWatcher m_monitorWatcher;

        private RuntimeEntityModel Context { get; set; }

        private Session Session { get; set; }

        private RTMigrationSource RTMigrationSource { get; set; }

        private RTMigrationSource PeerRTMigrationSource { get; set; }

        private Configuration Config { get; set; }

        private ISyncMonitorProvider SyncMonitorProvider { get; set; }

        private bool IsRightMigrationSource { get; set; }

        private List<string> m_filterStrings = new List<string>();

        public string UniqueId
        {
            get { return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", Session.SessionUniqueId, RTMigrationSource.Id); }
        }

        public string FriendlyName
        {
            get { return RTMigrationSource.FriendlyName; }
        }

        public Endpoint()
        {
        }

        public Endpoint(
            MonitorWatcher watcher, 
            RuntimeEntityModel context, 
            RTSession rtSession, 
            RTMigrationSource rtMigrationSource, 
            bool isRightMigrationSource,
            RTMigrationSource peerMigrationSource)
        {
            m_monitorWatcher = watcher;

            this.Context = context;
            this.RTMigrationSource = rtMigrationSource;
            this.IsRightMigrationSource = isRightMigrationSource;
            this.PeerRTMigrationSource = peerMigrationSource;

            BusinessModelManager businessModelManager = new BusinessModelManager();

            if (rtSession.SessionGroup == null)
            {
                rtSession.SessionGroupReference.Load();
            }

            Config = businessModelManager.LoadConfiguration(rtSession.SessionGroup.GroupUniqueId);
            if (Config == null)
            {
                throw new ApplicationException(
                    String.Format(CultureInfo.InvariantCulture, MigrationToolkitResources.SessionGroupNotFound,
                    rtSession.SessionGroup.GroupUniqueId.ToString()));
            }

            // TODO: Modify ProdviderManager to take a constructor that does not require a Config and that just loads
            // all providers in the Plugins directory, then move this code up to the MonitorWatcher constructor and pass the
            // providerHandlers down as another argument to this constructor
            ProviderManager providerManager = new ProviderManager(Config);
            Dictionary<Guid, ProviderHandler> providerHandlers = providerManager.LoadProvider(new DirectoryInfo(Constants.PluginsFolderName));

            ProviderHandler providerHandler;
            if (!providerHandlers.TryGetValue(this.RTMigrationSource.UniqueId, out providerHandler))
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, 
                    MigrationToolkitResources.ProviderHandlerNotLoadedForMigrationSouce, 
                    this.RTMigrationSource.FriendlyName));
            }

            Debug.Assert(providerHandler.Provider != null);
            SyncMonitorProvider = providerHandler.Provider.GetService(typeof(ISyncMonitorProvider)) as ISyncMonitorProvider;
            if (SyncMonitorProvider == null)
            {
                throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, 
                    MigrationToolkitResources.ProviderDoesNotImplementISyncMonitor,
                    providerHandler.ProviderName));
            }

            // Find the Session object corresponding to the RTSession
            if (Config.SessionGroup != null && Config.SessionGroup.Sessions != null)
            {
                foreach (var aSession in Config.SessionGroup.Sessions.Session)
                {
                    if (string.Equals(aSession.SessionUniqueId, rtSession.SessionUniqueId.ToString(), StringComparison.Ordinal))
                    {
                        Session = aSession;
                        break;
                    }
                }
            }
            if (Session == null)
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture,
                    MigrationToolkitResources.SessionNotFoundForMigrationSource,
                    rtSession.SessionGroup.GroupUniqueId.ToString(), RTMigrationSource.FriendlyName));
            }

            Guid migrationSourceGuid = new Guid(isRightMigrationSource ? Session.RightMigrationSourceUniqueId : Session.LeftMigrationSourceUniqueId);
            Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrationSource = Config.GetMigrationSource(migrationSourceGuid);
            Session.MigrationSources.Add(migrationSourceGuid, migrationSource);

            var serviceContainer = new ServiceContainer();
            serviceContainer.AddService(typeof(ITranslationService), new SyncMonitorTranslationService(Session));
            // We pass null for the global Configuration to the ConfigurationService constructor because its not handy and not needed in this context
            serviceContainer.AddService(typeof(ConfigurationService), new ConfigurationService(null, Session, migrationSourceGuid));
            SyncMonitorProvider.InitializeServices(serviceContainer);
            SyncMonitorProvider.InitializeClient(migrationSource);

            int filterPairIndex = IsRightMigrationSource ? 1 : 0;
            foreach (var filterPair in Session.Filters.FilterPair)
            {
                if (!filterPair.Neglect)
                {
                    m_filterStrings.Add(VCTranslationService.TrimTrailingPathSeparator(filterPair.FilterItem[filterPairIndex].FilterString));
                }
            }
        }

        public virtual void Poll()
        {
            m_monitorWatcher.LogVerbose(String.Format(CultureInfo.InvariantCulture,
                MigrationToolkitResources.PollingEndpoint, RTMigrationSource.FriendlyName));

            Stopwatch stopWatch = Stopwatch.StartNew();
            string lastMigratedChangeName = GetLastMigratedChangeName();
            stopWatch.Stop();
            m_monitorWatcher.LogVerbose(String.Format(MigrationToolkitResources.TimeForGetLastMigratedChangeName, stopWatch.Elapsed.TotalMilliseconds));

            if (string.IsNullOrEmpty(lastMigratedChangeName))
            {
                m_monitorWatcher.LogVerbose(String.Format(CultureInfo.InvariantCulture,
                    MigrationToolkitResources.GetLastMigratedChangeNameReturnedEmpty,
                    RTMigrationSource.FriendlyName));
            }
            else
            {
                m_monitorWatcher.LogVerbose(String.Format(MigrationToolkitResources.LastMigratedItemName, lastMigratedChangeName));

                DateTime pollTimeUtc = DateTime.UtcNow;
                Stopwatch stopWatch2 = Stopwatch.StartNew();
                ChangeSummary changeSummary = SyncMonitorProvider.GetSummaryOfChangesSince(lastMigratedChangeName, m_filterStrings);
                stopWatch2.Stop();
                m_monitorWatcher.LogVerbose(String.Format(MigrationToolkitResources.TimeForGetSummaryOfChangesSince, stopWatch2.Elapsed.TotalMilliseconds));

                TimeSpan latencyTimeSpan = pollTimeUtc - changeSummary.FirstChangeModifiedTimeUtc;
                if (latencyTimeSpan.TotalSeconds < 0)
                {
                    latencyTimeSpan = new TimeSpan(0);
                }

                if (changeSummary.ChangeCount > 0 && latencyTimeSpan.TotalSeconds > (double)Int32.MaxValue)
                {
                    // The Latency value is to big to be stored as a 32 bit int and must be wrong; return without storing a LatencyPoll row
                    m_monitorWatcher.LogWarning(String.Format(CultureInfo.InvariantCulture,
                        MigrationToolkitResources.SyncMonitorNotStoringInvalidLatencyPollData, RTMigrationSource.FriendlyName, lastMigratedChangeName));
                    return;
                }

                RTLatencyPoll latencyPoll =
                    RTLatencyPoll.CreateRTLatencyPoll(
                        0,
                        pollTimeUtc,
                        (changeSummary.ChangeCount == 0) ? DateTime.UtcNow : changeSummary.FirstChangeModifiedTimeUtc,
                        (changeSummary.ChangeCount == 0) ? 0 : Convert.ToInt32(latencyTimeSpan.TotalSeconds),
                        changeSummary.ChangeCount);

                latencyPoll.LastMigratedChange = lastMigratedChangeName;

                // latencyPoll.MigrationSourceReference.Attach(this.RTMigrationSource);
                latencyPoll.MigrationSource = this.RTMigrationSource;

                Context.AddToRTLatencyPollSet(latencyPoll);

                Context.TrySaveChanges();

                m_monitorWatcher.LogVerbose(String.Format(CultureInfo.InvariantCulture,
                    MigrationToolkitResources.SuccessfullyPolledEndpoint,
                    RTMigrationSource.FriendlyName, latencyPoll.Latency, latencyPoll.BacklogCount));
            }
        }

        public virtual void LogSuccessfulCompletion()
        {
            RTLatencyPoll latencyPoll =
                RTLatencyPoll.CreateRTLatencyPoll(
                    0,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    0,
                    0);

            // latencyPoll.MigrationSourceReference.Attach(this.RTMigrationSource);
            latencyPoll.MigrationSource = this.RTMigrationSource;

            Context.AddToRTLatencyPollSet(latencyPoll);

            Context.TrySaveChanges();
        }

        private string GetLastMigratedChangeName()
        {
            string lastMigratedChangeName = null;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int changeGroupStatusComplete = (int)ChangeStatus.Complete;
                var changeGroupQuery =
                    (from cg in context.RTChangeGroupSet
                     where cg.SourceSideMigrationSource.Id == this.PeerRTMigrationSource.Id &&
                           cg.SessionUniqueId == Session.SessionUniqueIdGuid &&
                           cg.Status == changeGroupStatusComplete &&
                           cg.Name != "Context Information"
                     orderby cg.Id descending
                     select cg.Name).Take(1);
                if (changeGroupQuery.Count() > 0)
                {
                    lastMigratedChangeName = changeGroupQuery.First();
                }
                else
                {
                    m_monitorWatcher.LogVerbose(String.Format(CultureInfo.InvariantCulture,
                        MigrationToolkitResources.GetLastMigratedChangeNameFoundNoRows, this.FriendlyName));
                }
            }

            return lastMigratedChangeName;
        }
    }
}
