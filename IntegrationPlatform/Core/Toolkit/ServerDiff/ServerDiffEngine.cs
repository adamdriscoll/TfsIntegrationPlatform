// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.Toolkit.WIT;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff
{
    internal class ServerDiffEngine
    {
        public bool NoContentComparison { get; set; }
        public bool Verbose { get; set; }
        public bool UseTraceManager { get; set; }
        public bool StoreResultsInDB { get; set; }
        public Session Session { get; set; }
        public Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource LeftMigrationSource { get; set; }
        public Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource RightMigrationSource { get; set; }
        public Guid LeftDiffProviderGuid { get; set; }
        public Guid RightDiffProviderGuid { get; set; }
        public IProvider LeftProvider { get; set; }
        public IProvider RightProvider { get; set; }
        public IDiffProvider SourceDiffProvider { get; set; }
        public IDiffProvider TargetDiffProvider { get; set; }
        public ServiceContainer DiffServiceContainer { get; private set; }

        private IDiffComparer DiffComparer { get; set; }

        private BusinessModelManager m_businessModelManager;

        private Dictionary<Guid, ILinkProvider> m_linkProvidersByMigrationSourceId = new Dictionary<Guid, ILinkProvider>();

        private BusinessModelManager BusinessModelManager
        {
            get
            {
                if (m_businessModelManager == null)
                {
                    m_businessModelManager = new BusinessModelManager();
                }
                return m_businessModelManager;
            }

        }

        internal AddinManagementService AddinManagementService { get; set; }

        public Configuration Config { get; set; }

        private Guid SessionGuidArgument { get; set; }

        private List<string> m_serverDiffResultDetails = new List<string>();

        /// <summary>
        /// VCServerDiff will verify the first VC session in the session group identified by the sessionGroupId
        /// </summary>
        /// <param name="sessionGuid">The Guid of the Session group on which to perform the diff operation</param>
        /// <param name="noContentComparison">If true, don't compare the contents of each items, just the existence on both sides</param>
        /// <param name="verbose">Whether or not to include verbose messages in any tracing that is performed</param>
        public ServerDiffEngine(Guid sessionGuid, bool noContentComparison, bool verbose, SessionTypeEnum sessionType) :
            this(sessionGuid, noContentComparison, verbose, sessionType, false, false)
        {
        }

        /// <summary>
        /// VCServerDiff will verify the first VC session in the session group identified by the sessionGroupId
        /// </summary>
        /// <param name="sessionGuid">The Guid of the Session on which to perform the diff operation</param>
        /// <param name="noContentComparison">If true, don't compare the contents of each items, just the existence on both sides</param>
        /// <param name="verbose">Whether or not to include verbose messages in any tracing that is performed</param>
        /// <param name="useTraceManager">Whether or not to write messages using the TraceManager class</param>
        /// <param name="storeResultsInDB">Whether or not to store the results of the Diff operation in the Tfs_IntegrationPlatform database</param>        
        public ServerDiffEngine(
            Guid sessionGuid, 
            bool noContentComparison, 
            bool verbose, 
            SessionTypeEnum sessionType, 
            bool useTraceManager,
            bool storeResultsInDB)
        {
            NoContentComparison = noContentComparison;
            Verbose = verbose;
            UseTraceManager = useTraceManager;
            StoreResultsInDB = storeResultsInDB;

            SessionGuidArgument = sessionGuid;
            if (sessionGuid.Equals(Guid.Empty))
            {
                Session = FindMostRecentSessionOfType(sessionType);
                if (Session == null)
                {
                    throw new MigrationSessionNotFoundException(
                        String.Format(CultureInfo.InvariantCulture, ServerDiffResources.SessionWithTypeNotFound, sessionType));
                }
            }
            else
            {
                Session = FindSession(sessionGuid, sessionType);
                if (Session == null)
                {
                    throw new MigrationSessionNotFoundException(
                        String.Format(CultureInfo.InvariantCulture, ServerDiffResources.SessionWithIdAndTypeNotFound, sessionType, sessionGuid.ToString()));
                }
            }

            ProviderManager providerManager = new ProviderManager(Config);
            Dictionary<Guid, ProviderHandler> providerHandlers = providerManager.LoadProvider(new DirectoryInfo(Constants.PluginsFolderName));
            AddinManagementService = providerManager.AddinManagementService;
            TraceManager.TraceInformation("{0} Add-Ins loaded", AddinManagementService.Count);

            LeftDiffProviderGuid = new Guid(Session.LeftMigrationSourceUniqueId);
            LeftMigrationSource = Config.GetMigrationSource(LeftDiffProviderGuid);
            if (!providerHandlers.ContainsKey(LeftDiffProviderGuid))
            {
                throw new ApplicationException(
                    String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ServerDiffProviderNotLoaded, LeftMigrationSource.FriendlyName));                
            }
            LeftProvider = providerHandlers[LeftDiffProviderGuid].Provider;

            RightDiffProviderGuid = new Guid(Session.RightMigrationSourceUniqueId);
            RightMigrationSource = Config.GetMigrationSource(RightDiffProviderGuid);
            if (!providerHandlers.ContainsKey(RightDiffProviderGuid))
            {
                throw new ApplicationException(
                    String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ServerDiffProviderNotLoaded, RightMigrationSource.FriendlyName));
            }
            else
            {
                RightProvider = providerHandlers[RightDiffProviderGuid].Provider;
            }

            Session.MigrationSources.Add(LeftDiffProviderGuid, LeftMigrationSource);
            Session.MigrationSources.Add(RightDiffProviderGuid, RightMigrationSource);

            LogInfo(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.MigrationSourceName, LeftMigrationSource.FriendlyName));
            LogInfo(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.MigrationTargetName, RightMigrationSource.FriendlyName));

            SourceDiffProvider = GetDiffProviderForMigrationSource(LeftMigrationSource, providerHandlers);
            TargetDiffProvider = GetDiffProviderForMigrationSource(RightMigrationSource, providerHandlers);
            if (SourceDiffProvider is IWITDiffProvider)
            {
                ILinkProvider sourceLinkProvider = SourceDiffProvider.GetService(typeof(ILinkProvider)) as ILinkProvider;
                ILinkProvider targetLinkProvider = TargetDiffProvider.GetService(typeof(ILinkProvider)) as ILinkProvider;
                if (sourceLinkProvider != null && targetLinkProvider != null)
                {
                    sourceLinkProvider.SupportedLinkTypeReferenceNamesOther = targetLinkProvider.SupportedLinkTypes.Keys;
                    targetLinkProvider.SupportedLinkTypeReferenceNamesOther = sourceLinkProvider.SupportedLinkTypes.Keys;
                }
            }
        }

        #region public methods
        public bool VerifyContentsMatch(string leftQualifier, string rightQualifier)
        {
            bool allContentsMatch = false;
            
            Debug.Assert(DiffComparer != null,
                "ServerDiffEngine: An IDiffComparer must be registered before VerifyContentsMatch is called");

            DateTime diffStartTime = DateTime.Now;
            Stopwatch timeToVerify = Stopwatch.StartNew();
            try
            {
                allContentsMatch = DiffComparer.VerifyContentsMatch(leftQualifier, rightQualifier);
            }
            catch (Exception e)
            {
                m_serverDiffResultDetails.Add(e.ToString());
            }
            finally
            {
                timeToVerify.Stop();
            }

            if (StoreResultsInDB)
            {
                int durationOfDiffInSecs = (int)Math.Round(timeToVerify.Elapsed.TotalSeconds);
                try
                {
                    InsertServerDiffResultInDB(diffStartTime, allContentsMatch, durationOfDiffInSecs, leftQualifier, rightQualifier);
                }
                catch (Exception ex)
                {
                    LogError(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ExceptionStoringResultsInDB, ex.ToString()));
                }
            }

            return allContentsMatch;
        }

        public bool LinkTypeSupportedByOtherSide(string linkTypeReferenceName, Guid sourceId)
        {
            Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource peerMigrationSource = 
                (sourceId == new Guid(LeftMigrationSource.InternalUniqueId)) ? RightMigrationSource : LeftMigrationSource;
            ILinkProvider linkProvider;
            if (m_linkProvidersByMigrationSourceId.TryGetValue(new Guid(peerMigrationSource.InternalUniqueId), out linkProvider))
            {
                return linkProvider.SupportedLinkTypes.ContainsKey(linkTypeReferenceName);
            }
            else
            {
                return false;
            }
        }

        public void LogError(string message)
        {
            string serverDiffMessage = String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ServerDiffError, message);
            if (UseTraceManager)
            {
                TraceManager.TraceError(serverDiffMessage);
            }
            else
            {
                Console.WriteLine(serverDiffMessage);
            }
            if (StoreResultsInDB)
            {
                m_serverDiffResultDetails.Add(message);
            }
        }

        public void LogInfo(string message)
        {
            string serverDiffMessage = String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ServerDiffInfo, message);
            if (UseTraceManager)
            {
                TraceManager.TraceInformation(serverDiffMessage);
            }
            else
            {
                Console.WriteLine(serverDiffMessage);
            }
        }

        public void LogVerbose(string message)
        {
            if (Verbose)
            {
                LogInfo(message);
            }
        }

        public void LogResult(string message)
        {
            LogInfo(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ServerDiffResult, message));
        }

        public void RegisterDiffComparer(IDiffComparer diffComparer)
        {
            Debug.Assert(DiffComparer == null,
                "ServerDiffEngine: Only one IDiffComparer can be registered");
            DiffComparer = diffComparer;
        }

        public static List<Guid> FindAllActiveSessionsOfType(SessionTypeEnum sessionType)
        {
            List<Guid> allActiveSessionsOfSelectedType = new List<Guid>();
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // Query all of the session groups ordered by Id descending to get the most recent first
                var sessionGroupQuery =
                    (from sg in context.RTSessionGroupSet
                     orderby sg.Id
                     select sg);

                // Iterate through the session groups until we find one that has the requested sessionType
                foreach (RTSessionGroup rtSessionGroup in sessionGroupQuery)
                {
                    BusinessModelManager bmManager = new BusinessModelManager();
                    Configuration config = bmManager.LoadConfiguration(rtSessionGroup.GroupUniqueId);
                    if (config == null)
                    {
                        continue;
                    }

                    HashSet<Guid> sessionGroupActiveSessions = new HashSet<Guid>();
                    rtSessionGroup.Sessions.Load();
                    foreach(RTSession rtSession in rtSessionGroup.Sessions)
                    {
                        if (rtSession.OrchestrationStatus != (int)PipelineState.Default &&
                            rtSession.OrchestrationStatus != (int)PipelineState.Stopped)
                        {
                            sessionGroupActiveSessions.Add(rtSession.SessionUniqueId);
                        }
                    }
                    List<Guid> sessionGroupActiveSessionsOfSelectedType = FilterActiveSessionsByType(config, sessionGroupActiveSessions, sessionType);
                    allActiveSessionsOfSelectedType.AddRange(sessionGroupActiveSessionsOfSelectedType);
                }

            }

            return allActiveSessionsOfSelectedType;
        }
        #endregion

        #region private methods
        private Session FindMostRecentSessionOfType(SessionTypeEnum sessionType)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // Query all of the session groups ordered by Id descending to get the most recent first
                var sessionGroupQuery =
                    (from sg in context.RTSessionGroupSet
                     orderby sg.Id descending
                     select sg);

                // Iterate through the session groups until we find one that has the requested sessionType
                foreach (RTSessionGroup rtSessionGroup in sessionGroupQuery)
                {
                    Session session = GetSessionFromSessionGroup(rtSessionGroup.GroupUniqueId, sessionType);
                    if (session != null)
                    {
                        return session;
                    }
                }

                // No session groups had the requested session type, so return null
                return null;
            }
        }

        private Session FindSession(Guid sessionGuid, SessionTypeEnum expectedSessionType)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionQuery =
                    (from rts in context.RTSessionSet
                     where rts.SessionUniqueId == sessionGuid
                     select rts);

                if (sessionQuery.Count() == 0)
                {
                    return null;
                }

                RTSession rtSession = sessionQuery.First();
                rtSession.SessionGroupReference.Load();
                return GetSessionFromSessionGroup(rtSession.SessionGroup.GroupUniqueId, expectedSessionType);
            }
        }

        private Session GetSessionFromSessionGroup(Guid sessionGroupId, SessionTypeEnum sessionType)
        {
            Configuration config;
            config = this.BusinessModelManager.LoadConfiguration(sessionGroupId);
            if (config == null)
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, ServerDiffResources.SessionGroupNotFound, sessionGroupId.ToString()),
                    "sessionGroupId");
            }

            foreach (var session in config.SessionGroup.Sessions.Session)
            {
                if (session.SessionType == sessionType)
                {
                    Config = config;
                    return session;
                }
            }
            return null;
        }

        private static List<Guid> FilterActiveSessionsByType(Configuration config, HashSet<Guid> activeSessionGuids, SessionTypeEnum sessionType)
        {
            List<Guid> sessionGuidOfSelectedType = new List<Guid>();
            foreach (var session in config.SessionGroup.Sessions.Session)
            {
                if (activeSessionGuids.Contains(session.SessionUniqueIdGuid) && session.SessionType == sessionType)
                {
                    sessionGuidOfSelectedType.Add(session.SessionUniqueIdGuid);
                }
            }
            return sessionGuidOfSelectedType;
        }

        /// <summary>
        /// Returns an IDiffProvider given a Migration source and a Dictionary of the providers found
        /// </summary>
        private IDiffProvider GetDiffProviderForMigrationSource(
            Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrationSource,
            Dictionary<Guid, ProviderHandler> providerHandlers)
        {
            Guid providerGuid = new Guid(migrationSource.InternalUniqueId);
            ProviderHandler providerHandler = providerHandlers[providerGuid];
            Type diffProviderType = Session.SessionType == SessionTypeEnum.VersionControl ? typeof(IVCDiffProvider) : typeof(IWITDiffProvider);
            IDiffProvider diffProvider = providerHandler.Provider.GetService(diffProviderType) as IDiffProvider;
            if (diffProvider == null)
            {

                throw new Exception(string.Format(CultureInfo.InvariantCulture, ServerDiffResources.ProviderDoesNotImplementVCDiffInterface, providerHandler.ProviderName));
            }

            DiffServiceContainer = new ServiceContainer();
            DiffServiceContainer.AddService(typeof(ConfigurationService), new ConfigurationService(Config, Session, providerGuid));
            DiffServiceContainer.AddService(typeof(Session), Session);
            DiffServiceContainer.AddService(typeof(ICredentialManagementService), new CredentialManagementService(Config));

            // If this is a work item session, and the provider implements ILinkProvider, get and initialize
            // the ILinkProvider service and add it to the ServiceContainer so that the IDiffProvider implementation
            // can get and use the ILinkProvider implementation
            ILinkProvider linkProvider = null;
            if (Session.SessionType == SessionTypeEnum.WorkItemTracking)
            {
                DiffServiceContainer.AddService(typeof(ITranslationService), new WITTranslationService(Session, null));

                linkProvider = providerHandler.Provider.GetService(typeof(ILinkProvider)) as ILinkProvider;
                if (linkProvider != null)
                {
                    ServerDiffLinkTranslationService diffLinkTranslationService =
                        new ServerDiffLinkTranslationService(this, new Guid(migrationSource.InternalUniqueId), new LinkConfigurationLookupService(Config.SessionGroup.Linking));
                    DiffServiceContainer.AddService(typeof(ILinkTranslationService), diffLinkTranslationService);
                        
                    linkProvider.Initialize(DiffServiceContainer);
                    linkProvider.RegisterSupportedLinkTypes();
                    DiffServiceContainer.AddService(typeof(ILinkProvider), linkProvider);
                    m_linkProvidersByMigrationSourceId.Add(new Guid(migrationSource.InternalUniqueId), linkProvider);
                }
            }

            diffProvider.InitializeServices(DiffServiceContainer);
            diffProvider.InitializeClient(migrationSource);

            return diffProvider;
        }

        private void InsertServerDiffResultInDB(DateTime diffStartTime, bool allContentsMatch, int durationOfDiffInSeconds, string leftQualifier, string rightQualifier)
        {
            using (RuntimeEntityModel runtimeEntityModel = RuntimeEntityModel.CreateInstance())
            {
                RTServerDiffResult serverDiffResult =
                    RTServerDiffResult.CreateRTServerDiffResult(
                        0,
                        Session.SessionType.ToString(),
                        diffStartTime,
                        durationOfDiffInSeconds,
                        Session.SessionUniqueIdGuid,
                        allContentsMatch,
                        BuildOptionsStringForServerDiffResult(leftQualifier, rightQualifier));

                runtimeEntityModel.AddToRTServerDiffResultSet(serverDiffResult);

                foreach(string serverDiffResultDetail in m_serverDiffResultDetails)
                {
                    RTServerDiffResultDetail rtServerDiffResultDetail = RTServerDiffResultDetail.CreateRTServerDiffResultDetail(
                        0, serverDiffResultDetail);

                    rtServerDiffResultDetail.ServerDiffResult = serverDiffResult;

                    runtimeEntityModel.AddToRTServerDiffResultDetailSet(rtServerDiffResultDetail);
                }

                runtimeEntityModel.TrySaveChanges();
            }
        }

        private string BuildOptionsStringForServerDiffResult(string leftQualifier, string rightQualifier)
        {
            StringBuilder sb = new StringBuilder();

            if (SessionGuidArgument != Guid.Empty)
            {
                sb.Append("Session=");
                sb.Append(SessionGuidArgument.ToString());
                sb.Append(' ');
            }

            if (Verbose)
            {
                sb.Append("Verbose=true ");
            }

            if (NoContentComparison)
            {
                sb.Append("NoContentComparison=true ");
            }
            
            if (leftQualifier != null)
            {
                sb.Append("leftQualifier=");
                sb.Append(leftQualifier);
                sb.Append(' ');
            }

            if (rightQualifier != null)
            {
                sb.Append("rightQualifier=");
                sb.Append(rightQualifier);
            }

            return sb.ToString();
        }

        #endregion
    }
}