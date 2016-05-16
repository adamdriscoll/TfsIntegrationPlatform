// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.EntityModel;
using EM = Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// BusinessModelManager class
    /// </summary>
    public class BusinessModelManager
    {
        /// <summary>
        /// Session State enumeration.
        /// </summary>
        public enum SessionStateEnum
        {
            Initialized = 0,
            Running = 1,
            Paused = 2,
            Completed = 3,
            OneTimeCompleted = 4,
            StartedInStandaloneProcess = 10,
            MarkedForDeletion = 11,
        }

        private Configuration m_attachedConfiguration;
        private TfsMigrationConsolidatedDBEntities m_context;

        private readonly Dictionary<Guid, EMSession> m_savedSession = new Dictionary<Guid, EMSession>();
        private readonly Dictionary<Guid, SessionConfig> m_savedSessionConfig = new Dictionary<Guid, SessionConfig>();
        private readonly Dictionary<Guid, MigrationSourceConfig> m_savedMigrationSourceConfig = new Dictionary<Guid, MigrationSourceConfig>();
        private LinkingSetting m_savedLinkingSetting;
        private readonly Dictionary<Guid, EventSinkSetting> m_savedEventSinks = new Dictionary<Guid, EventSinkSetting>();

        private readonly Dictionary<Guid, Provider> m_savedProviders = new Dictionary<Guid, Provider>();
        private readonly Dictionary<int, EventSink> m_loadedEventSinks = new Dictionary<int, EventSink>();
        private readonly Dictionary<Guid, ProviderElement> m_loadedProviders = new Dictionary<Guid, ProviderElement>();
        private readonly List<Guid> m_loadedAddinRefNames = new List<Guid>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public BusinessModelManager()
        {
        }

        /// <summary>
        /// Save a detached configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns>The Id the of the save session group configuration</returns>
        public int SaveDetachedConfiguration(Configuration config)
        {
            AttachToDetachedConfiguration(config);
            config.Status = Configuration.ConfigurationStatus.Valid;
            return SaveConfiguration(config);
        }

        /// <summary>
        /// Save a detached configuration in "Proposed" status
        /// </summary>
        /// <param name="config"></param>
        /// <returns>The Id the of the proposed session group configuration</returns>
        public int ProposeDetachedConfiguration(Configuration config)
        {
            AttachToDetachedConfiguration(config);
            config.Status = Configuration.ConfigurationStatus.Proposed;
            return SaveConfiguration(config);
        }

        /// <summary>
        /// Save modifications to a configuration.
        /// </summary>
        /// <returns>The Id the of saved session group configuration</returns>
        public int SaveChanges()
        {
            if (m_attachedConfiguration == null)
            {
                return int.MinValue;
            }

            m_attachedConfiguration.Status = Configuration.ConfigurationStatus.Valid;
            return SaveConfiguration(m_attachedConfiguration);
        }

        /// <summary>
        /// Propose modifications to a configuration.
        /// </summary>
        /// <returns>The Id the of the proposed session group configuration</returns>
        public int ProposeChanges()
        {
            if (m_attachedConfiguration != null)
            {
                m_attachedConfiguration.Status = Configuration.ConfigurationStatus.Proposed;
                return SaveConfiguration(m_attachedConfiguration);
            }
            else
            {
                return int.MinValue;
            }
        }

        /// <summary>
        /// Gets a collections of Session Group Ids, the owner of which are not completed
        /// </summary>
        /// <returns></returns>
        public List<Guid> GetActiveSessionGroupUniqueIds()
        {
            int initializedStateVal = (int)SessionStateEnum.Initialized;
            int runningStateVal = (int)SessionStateEnum.Running;
            int pausedStateVal = (int)SessionStateEnum.Paused;
            int completedStateVal = (int)SessionStateEnum.Completed;

            var activeGroupIdQuery =
                from g in Context.SessionGroupSet
                where g.State == initializedStateVal
                   || g.State == runningStateVal
                   || g.State == pausedStateVal
                   || g.State == completedStateVal
                select g.GroupUniqueId;

            return activeGroupIdQuery.ToList();
        }

        /// <summary>
        /// Gets a collections of running Session Group Ids
        /// </summary>
        /// <returns></returns>
        public List<Guid> GetRunningSessionGroupUniqueIds()
        {
            int runningStateVal = (int)SessionStateEnum.Running;

            var runningGroupIdQuery =
                from g in Context.SessionGroupSet
                where g.State == runningStateVal
                select g.GroupUniqueId;

            return runningGroupIdQuery.ToList();
        }

        /// <summary>
        /// Load active configuration of a session group by its unique Id.
        /// </summary>
        /// <param name="sessionGroupUniqueId"></param>
        /// <returns></returns>
        public Configuration LoadConfiguration(Guid sessionGroupUniqueId)
        {
            int sessionGroupInternalId = LookupSessionGroupId(sessionGroupUniqueId);
            Configuration configuration = Configuration.LoadActiveConfiguration(sessionGroupInternalId);
            return configuration;
        }

        /// <summary>
        /// Load active configuration of a session group by the configuration's storage Id.
        /// </summary>
        /// <param name="internalConfigurationId"></param>
        /// <returns></returns>
        public Configuration LoadConfiguration(int internalConfigurationId)
        {
            if (m_attachedConfiguration != null)
            {
                throw new InvalidOperationException(Resource.ErrorAttachManagerToMultipleModel);
            }

            var sessionGroupConfigs = Context.SessionGroupConfigSet.Where(s => s.Id == internalConfigurationId);
            int SessionGroupConfigsCount = sessionGroupConfigs.Count();

            if (SessionGroupConfigsCount == 0)
            {
                throw new InconsistentDataException(Resource.ErrorInternalIdOfSessionGroupConfigNotExist);
            }

            if (SessionGroupConfigsCount > 1)
            {
                throw new InconsistentDataException(Resource.ErrorMultiConfigWithSameId);
            }

            SessionGroupConfig sessionGroupConfig = sessionGroupConfigs.First();
            return LoadConfiguration(sessionGroupConfig);
        }

        /// <summary>
        /// Load active configuration of a session group by its storage Id.
        /// </summary>
        /// <param name="sessionGroupId"></param>
        /// <returns></returns>
        public Configuration LoadActiveConfiguration(int sessionGroupId)
        {
            if (m_attachedConfiguration != null)
            {
                throw new InvalidOperationException(Resource.ErrorAttachManagerToMultipleModel);
            }

            var sessionGroupConfigs = Context.SessionGroupConfigSet
                                        .Where(s => s.Status == 0 && s.SessionGroup.Id == sessionGroupId);

            int sessionGroupConfigsCount = sessionGroupConfigs.Count();

            if (sessionGroupConfigsCount == 0)
            {
                return null;
            }

            if (sessionGroupConfigsCount > 1)
            {
                throw new InconsistentDataException(Resource.ErrorMultiActiveConfigExist);
            }

            SessionGroupConfig sessionGroupConfig = sessionGroupConfigs.First();
            return LoadConfiguration(sessionGroupConfig);
        }

        /// <summary>
        /// Checks if a configuration has been persisted to storage.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool IsConfigurationPersisted(Configuration config)
        {
            Guid configId = NormalizeUniqueId(config.UniqueId);
            var existingSessionGroupConfigWithGivenUniqueId =
                from c in Context.SessionGroupConfigSet
                where c.UniqueId == configId
                select c;

            return existingSessionGroupConfigWithGivenUniqueId.Count() > 0;
        }
        
        private int LookupSessionGroupId(Guid sessionoGroupUniqueId)
        {
            var sessionGroupIdQuery = 
                from g in Context.SessionGroupSet
                where g.GroupUniqueId.Equals(sessionoGroupUniqueId)
                select g.Id;
            return (sessionGroupIdQuery.Count() > 0 ? sessionGroupIdQuery.First() : 0);
        }
        
        private static Guid NormalizeUniqueId(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                throw new ArgumentNullException("uniqueId");
            }
            return new Guid(uniqueId);
        }

        private void CommitChanges()
        {
            Context.SaveChanges(false);
            Context.AcceptAllChanges();
        }

        private TfsMigrationConsolidatedDBEntities Context
        {
            get
            {
                if (m_context == null)
                {
                    m_context = TfsMigrationConsolidatedDBEntities.CreateInstance();
                }
                return m_context;
            }
        }

        /// <summary>
        /// The shared logic of saving a version of the configuration to the storage
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private int SaveConfiguration(Configuration config)
        {
            if (IsConfigurationPersisted(config))
            {
                throw new DuplicateConfigurationException(Resource.ErrorMultiSessionGroupConfigWithSameId);
            }

            ClearLocalEntityModelCache();
            
            try
            {
                ResetConfigChangeTracker(config);
                ChangeTracker.AnalyzeChangeImpact();

                SaveProviders(config);
                SaveAddins(config);
                SaveEventSinks(config);
                SaveMigrationSources(config);
                SaveLinkingSettings(config);
                SaveSessionConfigs(config);
                SessionGroupConfig groupConfig = SaveSessionGroupConfig(config);
                CommitChanges();

                MigrationDataCache dataCache = new MigrationDataCache();
                dataCache.TryCleanup(ChangeTracker);

                return groupConfig.Id;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                ClearLocalEntityModelCache();
            }
        }

        private ConfigurationChangeTracker ChangeTracker
        {
            get;
            set;
        }

        private void ResetConfigChangeTracker(Configuration config)
        {
            ChangeTracker = new ConfigurationChangeTracker(config);
        }

        private void ClearLocalEntityModelCache()
        {
            m_savedProviders.Clear();
            m_savedEventSinks.Clear();
            m_savedMigrationSourceConfig.Clear();
            m_savedLinkingSetting = null;
            m_savedSessionConfig.Clear();
            m_savedSession.Clear();
        }

        private SessionGroupConfig SaveSessionGroupConfig(Configuration config)
        {
            var groupConfig = CreateSessionGroupConfig(config);
            DeprecateSessionGroups(config);
            return groupConfig;
        }

        private void DeprecateSessionGroups(Configuration config)
        {
            Guid configId = NormalizeUniqueId(config.UniqueId);
            Guid groupId = NormalizeUniqueId(config.SessionGroup.SessionGroupGUID);
            var sessionGroupToBeDeprecated = Context.SessionGroupConfigSet.Where
                (s => s.UniqueId != configId
                    && s.Status == 0
                    && s.SessionGroup.GroupUniqueId.Equals(groupId));

            foreach (SessionGroupConfig groupConfig in sessionGroupToBeDeprecated)
            {
                groupConfig.Status = 2;
            }
        }

        private SessionGroup FindCreateSessionGroup(SessionGroupElement sessionGroupElement)
        {
            Guid sessionGroupId = NormalizeUniqueId(sessionGroupElement.SessionGroupGUID);
            var sessionGroupInEM = Context.SessionGroupSet.Where
                (s => s.GroupUniqueId.Equals(sessionGroupId));

            int sessionGroupInEMCount = sessionGroupInEM.Count();

            if (sessionGroupInEMCount > 1)
            {
                throw new InconsistentDataException(Resource.ErrorMultiSessionGroupWithSameId);
            }

            SessionGroup group = null;
            if (sessionGroupInEMCount == 1)
            {
                sessionGroupInEM.First().FriendlyName = sessionGroupElement.FriendlyName;
                group = sessionGroupInEM.First();
            }
            else
            {
                group = SessionGroup.CreateSessionGroup
                    (0,
                     sessionGroupId,
                     sessionGroupElement.FriendlyName ?? string.Empty);
                
                group.State = (int)SessionStateEnum.Initialized;
                Context.AddToSessionGroupSet(group);
            }

            foreach (Session session in sessionGroupElement.Sessions.Session)
            {
                Guid sessionUniqueGuid = NormalizeUniqueId(session.SessionUniqueId);
                
                if (m_savedSession.ContainsKey(sessionUniqueGuid)
                    && m_savedSession[sessionUniqueGuid].SessionGroup == null)
                {
                    m_savedSession[sessionUniqueGuid].SessionGroup = group;
                }
            }
            return group;
        }

        private SessionGroupConfig CreateSessionGroupConfig(Configuration config)
        {
            SessionGroupConfig groupConfig = SessionGroupConfig.CreateSessionGroupConfig
                (0, config.SessionGroup.CreationTime = DateTime.Now.ToUniversalTime(),
                config.Status == Configuration.ConfigurationStatus.Valid ? 0 : 1,
                NormalizeUniqueId(config.UniqueId), config.SessionGroup.WorkFlowType.StorageValue);
            groupConfig.Creator = config.SessionGroup.Creator;
            groupConfig.FriendlyName = config.FriendlyName;
            groupConfig.LinkingSetting = m_savedLinkingSetting;
            groupConfig.UserIdentityMappingsConfig = 
                Serialize(typeof(UserIdentityMappings), config.SessionGroup.UserIdentityMappings);
            groupConfig.ErrorManagementConfig = Serialize(typeof(ErrorManagement), config.SessionGroup.ErrorManagement);
            groupConfig.AddinsConfig = Serialize(typeof(Addins), config.Addins);
            groupConfig.Settings = Serialize(typeof(CustomSettingsElement), config.SessionGroup.CustomSettings);

            foreach (Session session in config.SessionGroup.Sessions.Session)
            {
                Guid sessionUniqueGuid = NormalizeUniqueId(session.SessionUniqueId);
                if (m_savedSessionConfig.ContainsKey(sessionUniqueGuid))
                {
                    m_savedSessionConfig[sessionUniqueGuid].SessionGroupConfig = groupConfig;
                }
                else
                {
                    throw new InvalidConfigurationChangeException(Resource.ErrorMissingSessionConfig);
                }
            }

            groupConfig.SessionGroup = FindCreateSessionGroup(config.SessionGroup);
            Context.AddToSessionGroupConfigSet(groupConfig);

            return groupConfig;
        }

        private void SaveSessionConfigs(Configuration config)
        {
            var sessionElements = config.SessionGroup.Sessions;
            foreach (Session sessionConfig in sessionElements.Session)
            {
                Debug.Assert(!string.IsNullOrEmpty(sessionConfig.SessionUniqueId));

                SessionConfig newConfig = SessionConfig.CreateSessionConfig(
                        0,
                        NormalizeUniqueId(sessionConfig.SessionUniqueId),
                        sessionConfig.FriendlyName,
                        sessionConfig.CreationTime = DateTime.Now.ToUniversalTime(),
                        sessionConfig.SessionType == SessionTypeEnum.VersionControl ? 0 : 1);

                foreach (EventSink bzEventSink in sessionConfig.EventSinks.EventSink)
                {
                    Guid eventSinkProviderId = NormalizeUniqueId(bzEventSink.ProviderReferenceName);
                    if (!m_savedEventSinks.ContainsKey(eventSinkProviderId))
                    {
                        throw new InvalidConfigurationChangeException(Resource.ErrorMissingEventSink);
                    }
                    newConfig.EventSinks.Add(m_savedEventSinks[eventSinkProviderId]);
                }

                Guid leftSourceId = NormalizeUniqueId(sessionConfig.LeftMigrationSourceUniqueId);
                if (!m_savedMigrationSourceConfig.ContainsKey(leftSourceId))
                {
                    throw new InvalidConfigurationChangeException(Resource.ErrorMissingMigrSourceConfig);
                }
                newConfig.LeftMigrationSourceConfig = m_savedMigrationSourceConfig[leftSourceId];

                Guid rightSourceId = NormalizeUniqueId(sessionConfig.RightMigrationSourceUniqueId);
                if (!m_savedMigrationSourceConfig.ContainsKey(rightSourceId))
                {
                    throw new InvalidConfigurationChangeException(Resource.ErrorMissingMigrSourceConfig);
                }
                newConfig.RightMigrationSourceConfig = m_savedMigrationSourceConfig[rightSourceId];

                newConfig.SettingXml = null == sessionConfig.CustomSettings.SettingXml ? string.Empty : GenericSettingXmlToString(sessionConfig.CustomSettings.SettingXml);
                newConfig.SettingXmlSchema = null == sessionConfig.CustomSettings.SettingXmlSchema ? string.Empty : GenericSettingXmlSchemaToString(sessionConfig.CustomSettings.SettingXmlSchema);

                foreach (FilterPair bzFilterPair in sessionConfig.Filters.FilterPair)
                {
                    Debug.Assert(bzFilterPair.FilterItem.Count == 2, "filter map configuration is invalid");
                    FilterItemPair pair = FilterItemPair.CreateFilterItemPair(0, bzFilterPair.Neglect);
                    pair.SessionConfiguration = newConfig;
                    pair.Filter1MigrationSourceReferenceName = NormalizeUniqueId(bzFilterPair.FilterItem[0].MigrationSourceUniqueId);
                    pair.Filter1 = bzFilterPair.FilterItem[0].FilterString;
                    pair.Filter1SnapshotPoint = bzFilterPair.FilterItem[0].SnapshotStartPoint;
                    pair.Filter1PeerSnapshotPoint = bzFilterPair.FilterItem[0].PeerSnapshotStartPoint;
                    pair.Filter1MergeScope = bzFilterPair.FilterItem[0].MergeScope;
                    pair.Filter2MigrationSourceReferenceName = NormalizeUniqueId(bzFilterPair.FilterItem[1].MigrationSourceUniqueId);
                    pair.Filter2 = bzFilterPair.FilterItem[1].FilterString;
                    pair.Filter2SnapshotPoint = bzFilterPair.FilterItem[1].SnapshotStartPoint;
                    pair.Filter2PeerSnapshotPoint = bzFilterPair.FilterItem[1].PeerSnapshotStartPoint;
                    pair.Filter2MergeScope = bzFilterPair.FilterItem[1].MergeScope;
                }

                Context.AddToSessionConfigSet(newConfig);
                
                m_savedSessionConfig.Add(NormalizeUniqueId(sessionConfig.SessionUniqueId), newConfig);

                // create the corresponding session
                FindCreateSession(sessionConfig);
            }
        }

        private void FindCreateSession(
            Session sessionConfig)
        {
            Guid sessionUniqueId = NormalizeUniqueId(sessionConfig.SessionUniqueId);
            var emSessionQuery =
                from s in Context.EMSessionSet
                where s.SessionUniqueId.Equals(sessionUniqueId)
                select s;

            if (emSessionQuery.Count() > 0
                && !m_savedSession.ContainsKey(sessionUniqueId))
            {
                m_savedSession.Add(sessionUniqueId, emSessionQuery.First());
                return;
            }

            EMSession newSession = EMSession.CreateEMSession(0, sessionUniqueId);
            newSession.State = (int)SessionStateEnum.Initialized;
            m_savedSession.Add(newSession.SessionUniqueId, newSession);
            
            Guid leftMigrSourceId = NormalizeUniqueId(sessionConfig.LeftMigrationSourceUniqueId);
            Guid rightMigrSourceId = NormalizeUniqueId(sessionConfig.RightMigrationSourceUniqueId);

            Debug.Assert(m_savedMigrationSourceConfig.ContainsKey(leftMigrSourceId));
            Debug.Assert(m_savedMigrationSourceConfig.ContainsKey(rightMigrSourceId));
            newSession.LeftSource = m_savedMigrationSourceConfig[leftMigrSourceId].MigrationSource;
            newSession.RightSource = m_savedMigrationSourceConfig[rightMigrSourceId].MigrationSource;

            Context.AddToEMSessionSet(newSession);
        }

        private void SaveMigrationSourcesConfig(MigrationSource migrationSource, EM.MigrationSource emMigrSource)
        {
            MigrationSourceConfig config = MigrationSourceConfig.CreateMigrationSourceConfig(0, DateTime.Now.ToUniversalTime());

            config.MigrationSource = emMigrSource;

            string settingXml = null == migrationSource.CustomSettings ? string.Empty : SettingXmlToString(migrationSource.CustomSettings);
            config.SettingXml = string.IsNullOrEmpty(settingXml) ? string.Empty : settingXml;

            config.SettingXmlSchema = string.Empty;

            config.GeneralSettingXml = (null == migrationSource.Settings)
                                       ? string.Empty
                                       : Serialize(typeof(Settings), migrationSource.Settings);

            Context.AddToMigrationSourceConfigSet(config);
            
            Guid migrSourceInternalId = NormalizeUniqueId(migrationSource.InternalUniqueId);
            Debug.Assert(!m_savedMigrationSourceConfig.ContainsKey(migrSourceInternalId));
            m_savedMigrationSourceConfig.Add(migrSourceInternalId, config);
        }

        private void SaveLinkingSettings(Configuration config)
        {
            if (null == config.SessionGroup.Linking)
            {
                return;
            }

            string linkingSettingStr = string.Empty;
            var linkingElemSerializer = new XmlSerializer(typeof(LinkingElement));
            using (var memStream = new MemoryStream())
            {
                linkingElemSerializer.Serialize(memStream, config.SessionGroup.Linking);
                memStream.Seek(0, SeekOrigin.Begin);
                using (var streamReader = new StreamReader(memStream))
                {
                    linkingSettingStr = streamReader.ReadToEnd();
                }
            }

            LinkingSetting emLinkingSetting = Context.FindLastestLinkingSetting(linkingSettingStr);
            if (null == emLinkingSetting)
            {
                emLinkingSetting = LinkingSetting.CreateLinkingSetting(0, linkingSettingStr);
            }

            m_savedLinkingSetting = emLinkingSetting;
        }

        private void SaveMigrationSources(Configuration config)
        {
            var bmMigrationSources = config.SessionGroup.MigrationSources.MigrationSource;
            foreach (MigrationSource bmMigrationSource in bmMigrationSources)
            {
                Guid migrSourceProviderRefName = NormalizeUniqueId(bmMigrationSource.ProviderReferenceName);
                if (string.IsNullOrEmpty(bmMigrationSource.InternalUniqueId))
                {
                    bmMigrationSource.InternalUniqueId = Utility.GetUniqueIdString();
                }

                if (!m_savedProviders.ContainsKey(migrSourceProviderRefName))
                {
                    throw new InvalidConfigurationChangeException(Resource.ErrorMissingProviderForMigrationSource);
                }

                Provider emProvider = m_savedProviders[migrSourceProviderRefName];

                var emMigrationSource =
                    Context.FindMigrationSourceByInternalUniqueId(NormalizeUniqueId(bmMigrationSource.InternalUniqueId));

                if (null != emMigrationSource)
                {
                    emMigrationSource.ProviderReference.Load();

                    if (!string.IsNullOrEmpty(emMigrationSource.NativeId)
                        && !string.IsNullOrEmpty(bmMigrationSource.NativeId)
                        && !emMigrationSource.NativeId.Equals(bmMigrationSource.NativeId, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new MismatchingMigrationSourceNativeIdException(
                            emMigrationSource.NativeId, bmMigrationSource.NativeId);
                    }

                    emMigrationSource.FriendlyName = bmMigrationSource.FriendlyName;
                    emMigrationSource.ServerIdentifier = bmMigrationSource.ServerIdentifier;
                    emMigrationSource.ServerUrl = bmMigrationSource.ServerUrl; 
                    emMigrationSource.SourceIdentifier = bmMigrationSource.SourceIdentifier;
                    emMigrationSource.Provider = emProvider;

                    SaveStoredCredential(emMigrationSource, bmMigrationSource.StoredCredential);

                    SaveMigrationSourcesConfig(bmMigrationSource, emMigrationSource);
                }
                else
                {
                    emMigrationSource = EM.MigrationSource.CreateMigrationSource(
                        0,
                        bmMigrationSource.FriendlyName,
                        bmMigrationSource.ServerIdentifier,
                        bmMigrationSource.ServerUrl,
                        bmMigrationSource.SourceIdentifier,
                        NormalizeUniqueId(bmMigrationSource.InternalUniqueId));
                    emMigrationSource.NativeId = bmMigrationSource.NativeId;
                    Context.AddToMigrationSourceSet(emMigrationSource);
                    SaveStoredCredential(emMigrationSource, bmMigrationSource.StoredCredential);
                    emMigrationSource.Provider = emProvider;
                    SaveMigrationSourcesConfig(bmMigrationSource, emMigrationSource);
                }
            }
        }

        private void SaveStoredCredential(EM.MigrationSource migrSource, StoredCredential storedCredential)
        {
            if (null == storedCredential || string.IsNullOrEmpty(storedCredential.CredentialString))
            {
                return;
            }

            var storedCreds = Context.StoredCredentialSet.Where(sc => sc.MigrationSource.Id == migrSource.Id);

            int storedCredsCount = storedCreds.Count();
            if (storedCredsCount > 1)
            {
                throw new InconsistentDataException(string.Format(Resource.ErrorGenericDBInconsistency, typeof(StoredCredential).FullName));
            }
            if (storedCredsCount == 1)
            {
                storedCreds.First().CredentialString = storedCredential.CredentialString;
            }
            else
            {
                EM.StoredCredential newSC = EM.StoredCredential.CreateStoredCredential(0, storedCredential.CredentialString);
                Context.AddToStoredCredentialSet(newSC);
                newSC.MigrationSource = migrSource;
            }
        }

        private void SaveEventSinks(Configuration config)
        {            
            foreach (Session session in config.SessionGroup.Sessions.Session)
            {
                foreach (EventSink eventSink in session.EventSinks.EventSink)
                {
                    Guid sinkProviderRefName = NormalizeUniqueId(eventSink.ProviderReferenceName);
                    if (m_savedEventSinks.ContainsKey(sinkProviderRefName))
                    {
                        continue;
                    }

                    EventSinkSetting eventSinkSetting = Context.FindEventSinkSetting(sinkProviderRefName);
                    if (null != eventSinkSetting)
                    {
                        eventSinkSetting.FriendlyName = eventSink.FriendlyName;
                        eventSinkSetting.SettingXml = SettingXmlToString(eventSink.CustomSettings);
                        eventSinkSetting.SettingXmlSchema = string.Empty;
                    }
                    else
                    {
                        eventSinkSetting = EventSinkSetting.CreateEventSinkSetting(0, eventSink.FriendlyName, eventSink.CreationTime = DateTime.Now.ToUniversalTime());
                        Context.AddToEventSinkSettingSet(eventSinkSetting);

                        if (!m_savedProviders.ContainsKey(sinkProviderRefName))
                        {
                            throw new InconsistentDataException(Resource.ErrorMissingEventSinkProvider);
                        }
                        eventSinkSetting.Provider = m_savedProviders[sinkProviderRefName];
                    }

                    if (!m_savedEventSinks.ContainsKey(sinkProviderRefName))
                    {
                        m_savedEventSinks.Add(sinkProviderRefName, eventSinkSetting);
                    }
                }
            }
        }

        private void SaveProviders(Configuration config)
        {
            foreach (ProviderElement providerElement in config.Providers.Provider)
            {
                Provider provider = Context.FindProviderByReferenceName(NormalizeUniqueId(providerElement.ReferenceName));

                if (null != provider)
                {
                    provider.FriendlyName = providerElement.FriendlyName;
                }
                else
                {
                    provider = Provider.CreateProvider(0, providerElement.FriendlyName, NormalizeUniqueId(providerElement.ReferenceName));
                    Context.AddToProviderSet(provider);
                }

                m_savedProviders.Add(provider.ReferenceName, provider);
            }
        }
        
        private void SaveAddins(Configuration config)
        {
            List<Guid> savedAddins = new List<Guid>(config.Addins.Addin.Count);

            foreach (var addin in config.Addins.Addin)
            {
                Guid addinRefName = NormalizeUniqueId(addin.ReferenceName);

                if (savedAddins.Contains(addinRefName))
                {
                    continue;
                }

                var addinQuery =
                    from a in Context.AddinSet
                    where a.ReferenceName.Equals(addinRefName)
                       && a.FriendlyName.Equals(addin.FriendlyName)
                    select a;

                if (addinQuery.Count() == 0)
                {
                    Addin newAddinRow = Addin.CreateAddin(0, addinRefName, addin.FriendlyName);
                    Context.AddToAddinSet(newAddinRow);
                }

                savedAddins.Add(addinRefName);
            }
        }

        private Configuration LoadConfiguration(SessionGroupConfig sessionGroupConfig)
        {
            try
            {
                Debug.Assert(null != sessionGroupConfig, "null == sessionGroupConfig");

                // Top down walk through stored SessionGroupConfig to build a runtime Configuration.  Note
                // that a Configuration is effectively a fully materialized entity framework object graph 
                // in a serializable form that adheres to the defined schema.
                var configuration = new Configuration();
                PopulateData(configuration, sessionGroupConfig);

                AttachToDetachedConfiguration(configuration);

                return configuration;
            }
            finally
            {
                m_loadedAddinRefNames.Clear();
                m_loadedEventSinks.Clear();
                m_loadedProviders.Clear();
            }
        }

        private void PopulateData(Configuration configuration, SessionGroupConfig sessionGroupConfig)
        {
            configuration.FriendlyName = sessionGroupConfig.FriendlyName;
            // configuration.Manager - We don't wire up this property until the Configuration gets attached.
            // configuration.Providers - Loaded later when we are walking MigrationSources

            LoadSessionGroupElement(configuration, sessionGroupConfig);

            configuration.Status = (Configuration.ConfigurationStatus) sessionGroupConfig.Status;
            configuration.UniqueId = sessionGroupConfig.UniqueId.ToString();
        }

        private void LoadSessionGroupElement(Configuration configuration, SessionGroupConfig sessionGroupConfig)
        {
            // Force SessionGroups to be loaded
            if (! sessionGroupConfig.SessionGroupReference.IsLoaded)
            {
                sessionGroupConfig.SessionGroupReference.Load();
            }

            var sessionGroupElement = configuration.SessionGroup;

            // TODO: Added CreationTime for completeness, but not sure why it was not set in 
            // favor of CreationTimeUtc only
            sessionGroupElement.CreationTime = sessionGroupConfig.CreationTime;
            sessionGroupElement.CreationTimeUtc = sessionGroupConfig.CreationTime;
            sessionGroupElement.Creator = sessionGroupConfig.Creator;

            if (sessionGroupConfig.DeprecationTime.HasValue)
            {
                sessionGroupElement.DeprecationTimeUtc = sessionGroupConfig.DeprecationTime.Value;
            }

            sessionGroupElement.FriendlyName = sessionGroupConfig.SessionGroup.FriendlyName;
            LoadLinkingElement(configuration, sessionGroupConfig);
            // sessionGroupElement.MigrationSources - Loaded later when we are processing Sessions
            sessionGroupElement.SessionGroupGUID = sessionGroupConfig.SessionGroup.GroupUniqueId.ToString();
            LoadSessions(configuration, sessionGroupConfig);

            sessionGroupElement.WorkFlowType = new WorkFlowType(sessionGroupConfig.WorkFlowType);

            UserIdentityMappings userIdMappingsConfig =
                Deserialize(sessionGroupConfig.UserIdentityMappingsConfig, typeof(UserIdentityMappings)) as UserIdentityMappings;
            sessionGroupElement.UserIdentityMappings = userIdMappingsConfig ?? new UserIdentityMappings();

            CustomSettingsElement customSettingsConfig =
                Deserialize(sessionGroupConfig.Settings, typeof(CustomSettingsElement)) as CustomSettingsElement;
            sessionGroupElement.CustomSettings = customSettingsConfig ?? new CustomSettingsElement();

            ErrorManagement errorManagementConfig =
                Deserialize(sessionGroupConfig.ErrorManagementConfig, typeof(ErrorManagement)) as ErrorManagement;
            sessionGroupElement.ErrorManagement = errorManagementConfig ?? new ErrorManagement();

            if (!string.IsNullOrEmpty(sessionGroupConfig.AddinsConfig))
            {
                configuration.Addins = Deserialize(sessionGroupConfig.AddinsConfig, typeof(Addins)) as Addins;
            }
            else
            {
                // backward compatibility
                if (null != userIdMappingsConfig)
                {
                    LoadAddinsUsedInUserIdentityLookup(configuration, userIdMappingsConfig);
                }
            }
        }

        private void LoadAddinsUsedInUserIdentityLookup(
            Configuration configuration,
            UserIdentityMappings userIdMappingsConfig)
        {
            foreach (string addinRefNameStr in userIdMappingsConfig.UserIdentityLookupAddins.UserIdentityLookupAddin)
            {
                LoadAddin(configuration, addinRefNameStr);
            }
        }

        private void LoadAddin(Configuration configuration, string addinRefNameStr)
        {
            Guid addinRefName = NormalizeUniqueId(addinRefNameStr);

            if (!m_loadedAddinRefNames.Contains(addinRefName))
            {
                var addinQuery =
                    from a in Context.AddinSet
                    where a.ReferenceName.Equals(addinRefName)
                    select a;

                if (addinQuery.Count() > 0)
                {
                    AddinElement addinElem = new AddinElement();
                    addinElem.FriendlyName = addinQuery.First().FriendlyName;
                    addinElem.ReferenceName = addinRefNameStr;

                    configuration.Addins.Addin.Add(addinElem);
                    m_loadedAddinRefNames.Add(addinRefName);
                }
            }
        }

        private void LoadLinkingElement(Configuration configuration, SessionGroupConfig sessionGroupConfig)
        {
            // Force LinkingSetting to be loaded
            if (!sessionGroupConfig.LinkingSettingReference.IsLoaded)
            {
                sessionGroupConfig.LinkingSettingReference.Load();
            }

            LinkingElement linkingElement;

            if (null == sessionGroupConfig.LinkingSetting
                || string.IsNullOrEmpty(sessionGroupConfig.LinkingSetting.SettingXml))
            {
                linkingElement = new LinkingElement();
            }
            else
            {
                var serializer = new XmlSerializer(typeof(LinkingElement));
                using (var strReader = new StringReader(sessionGroupConfig.LinkingSetting.SettingXml))
                using (var xmlReader = XmlReader.Create(strReader))
                {
                    linkingElement = serializer.Deserialize(xmlReader) as LinkingElement;
                }
            }
            configuration.SessionGroup.Linking = linkingElement;
        }

        private void LoadSessions(Configuration configuration, SessionGroupConfig sessionGroupConfig)
        {
            // Force SessionConfigs to be loaded
            if (!sessionGroupConfig.SessionConfigs.IsLoaded)
            {
                sessionGroupConfig.SessionConfigs.Load();
            }

            SessionGroupElement sessionGroupElement = configuration.SessionGroup;

            Debug.Assert(sessionGroupElement.Sessions.Session.Count == 0);
            sessionGroupElement.Sessions = new SessionsElement();
            SessionsElement sessionsElem = sessionGroupElement.Sessions;

            foreach (SessionConfig sessionConfig in sessionGroupConfig.SessionConfigs)
            {
                var session = new Session();
                // TODO: Added CreationTime, but why do both exist?
                session.CreationTime = sessionConfig.CreationTime;
                session.CreationTimeUtc = sessionConfig.CreationTime;
                session.Creator = sessionConfig.Creator;
                session.CustomSettings.SettingXml = StringToGenericSettingXmlConverter(sessionConfig.SettingXml);
                session.CustomSettings.SettingXmlSchema = StringToGenericSettingXmlSchemaConverter(sessionConfig.SettingXmlSchema);

                if (sessionConfig.DeprecationTime.HasValue)
                {
                    session.DeprecationTimeUtc = sessionConfig.DeprecationTime.Value;
                }
                LoadEventSinks(configuration, session.EventSinks.EventSink, sessionConfig);

                // Force FILTER_ITEM_PAIRs to be loaded
                if (! sessionConfig.FilterItemPairs.IsLoaded)
                {
                    sessionConfig.FilterItemPairs.Load();
                }

                foreach (FilterItemPair filterItemPair in sessionConfig.FilterItemPairs)
                {
                    var pair = new FilterPair();
                    pair.Neglect = filterItemPair.Neglect;

                    var item1 = new FilterItem();
                    item1.MigrationSourceUniqueId = filterItemPair.Filter1MigrationSourceReferenceName.ToString();
                    item1.FilterString = filterItemPair.Filter1;
                    item1.SnapshotStartPoint = filterItemPair.Filter1SnapshotPoint;
                    item1.PeerSnapshotStartPoint = filterItemPair.Filter1PeerSnapshotPoint;
                    item1.MergeScope = filterItemPair.Filter1MergeScope;
                    var item2 = new FilterItem();
                    item2.MigrationSourceUniqueId = filterItemPair.Filter2MigrationSourceReferenceName.ToString();
                    item2.FilterString = filterItemPair.Filter2;
                    item2.SnapshotStartPoint = filterItemPair.Filter2SnapshotPoint;
                    item2.PeerSnapshotStartPoint = filterItemPair.Filter2PeerSnapshotPoint;
                    item2.MergeScope = filterItemPair.Filter2MergeScope;
                    pair.FilterItem.Add(item1);
                    pair.FilterItem.Add(item2);

                    session.Filters.FilterPair.Add(pair);
                }
                
                session.FriendlyName = sessionConfig.FriendlyName;

                // Force LeftMigrationSourceConfig to be loaded
                if (! sessionConfig.LeftMigrationSourceConfigReference.IsLoaded)
                {
                    sessionConfig.LeftMigrationSourceConfigReference.Load();
                }
                if (! sessionConfig.LeftMigrationSourceConfig.MigrationSourceReference.IsLoaded)
                {
                    sessionConfig.LeftMigrationSourceConfig.MigrationSourceReference.Load();
                }
                session.LeftMigrationSourceUniqueId = sessionConfig.LeftMigrationSourceConfig.MigrationSource.UniqueId.ToString();
                LoadMigrationSources(configuration, sessionConfig.LeftMigrationSourceConfig);

                // Now RightMigrationSourceConfig
                if (!sessionConfig.RightMigrationSourceConfigReference.IsLoaded)
                {
                    sessionConfig.RightMigrationSourceConfigReference.Load();
                }
                if (!sessionConfig.RightMigrationSourceConfig.MigrationSourceReference.IsLoaded)
                {
                    sessionConfig.RightMigrationSourceConfig.MigrationSourceReference.Load();
                }
                session.RightMigrationSourceUniqueId = sessionConfig.RightMigrationSourceConfig.MigrationSource.UniqueId.ToString();
                LoadMigrationSources(configuration, sessionConfig.RightMigrationSourceConfig);
                
                session.SessionType = sessionConfig.Type == 0 ? SessionTypeEnum.VersionControl : SessionTypeEnum.WorkItemTracking;
                session.SessionUniqueId = sessionConfig.SessionUniqueId.ToString();
                //TODO: session.VCCustomSetting - not implemented?
                //TODO: session.WITCustomSetting - not implemented?

                sessionsElem.Session.Add(session);
            }
        }

        private void LoadEventSinks(Configuration configuration, ICollection<EventSink> eventSinks, SessionConfig sessionConfig)
        {
            if (!sessionConfig.EventSinks.IsLoaded)
            {
                sessionConfig.EventSinks.Load();
            }

            IEnumerable<EventSinkSetting> eventSinkSettings = sessionConfig.EventSinks;

            if (eventSinks == null) throw new ArgumentNullException("eventSinks");
            if (eventSinkSettings == null) throw new ArgumentNullException("eventSinkSettings");
            foreach (var eventSinkSetting in eventSinkSettings)
            {
                if (m_loadedEventSinks.ContainsKey(eventSinkSetting.Id))
                {
                    eventSinks.Add(m_loadedEventSinks[eventSinkSetting.Id]);
                }

                var sink = new EventSink();
                sink.CreationTimeUtc = eventSinkSetting.CreationTime;
                sink.CustomSettings = StringToSettingXmlConverter(eventSinkSetting.SettingXml);
                sink.FriendlyName = eventSinkSetting.FriendlyName;

                if (! eventSinkSetting.ProviderReference.IsLoaded)
                {
                    eventSinkSetting.ProviderReference.Load();
                }
                sink.ProviderReferenceName = eventSinkSetting.Provider.ReferenceName.ToString();
                var tmpProvider = new ProviderElement();
                tmpProvider.FriendlyName = eventSinkSetting.Provider.FriendlyName;
                tmpProvider.ReferenceName = sink.ProviderReferenceName;
                LoadProviders(configuration, tmpProvider);

                eventSinks.Add(sink);
                m_loadedEventSinks.Add(eventSinkSetting.Id, sink);
            }
        }

        private void LoadProviders(Configuration configuration, ProviderElement p)
        {
            var providerRefName = new Guid(p.ReferenceName);
            if (!m_loadedProviders.ContainsKey(providerRefName))
            {
                configuration.Providers.Provider.Add(p);
                m_loadedProviders.Add(providerRefName, p);
            }
        }

        private void LoadMigrationSources(Configuration configuration, MigrationSourceConfig migrationSourceConfig)
        {
            foreach (MigrationSource ms in configuration.SessionGroup.MigrationSources.MigrationSource)
            {
                if (string.Equals(ms.InternalUniqueId, migrationSourceConfig.MigrationSource.UniqueId))
                {
                    return;
                }
            }

            var source = new MigrationSource();
            source.CustomSettings = StringToSettingXmlConverter(migrationSourceConfig.SettingXml);
            source.FriendlyName = migrationSourceConfig.MigrationSource.FriendlyName;
            source.InternalUniqueId = migrationSourceConfig.MigrationSource.UniqueId.ToString();
            Settings settings = Deserialize(migrationSourceConfig.GeneralSettingXml, typeof(Settings)) as Settings;
            source.Settings = settings ?? new Settings();

            // Force MigrationSource.Provider to be loaded
            if (! migrationSourceConfig.MigrationSource.ProviderReference.IsLoaded)
            {
                migrationSourceConfig.MigrationSource.ProviderReference.Load();
            }
            source.ProviderReferenceName = migrationSourceConfig.MigrationSource.Provider.ReferenceName.ToString();
                       
            source.ServerIdentifier = migrationSourceConfig.MigrationSource.ServerIdentifier;
            source.ServerUrl = migrationSourceConfig.MigrationSource.ServerUrl;
            source.SourceIdentifier = migrationSourceConfig.MigrationSource.SourceIdentifier;

            // Force MigrationSource.Provider to be loaded
            if (! migrationSourceConfig.MigrationSource.StoredCredentialReference.IsLoaded)
            {
                migrationSourceConfig.MigrationSource.StoredCredentialReference.Load();
            }
            var sc = new StoredCredential();
            source.StoredCredential = sc;
            // TODO: Something is not quite right here... StoredCredential should not be null after load, but it is
            if (migrationSourceConfig.MigrationSource.StoredCredential != null)
            {
                sc.CredentialString = migrationSourceConfig.MigrationSource.StoredCredential.CredentialString;
            }

            var tmpProvider = new ProviderElement();
            tmpProvider.FriendlyName = migrationSourceConfig.MigrationSource.Provider.FriendlyName;
            tmpProvider.ReferenceName = migrationSourceConfig.MigrationSource.Provider.ReferenceName.ToString();
            LoadProviders(configuration, tmpProvider);

            configuration.SessionGroup.MigrationSources.MigrationSource.Add(source);

            LoadAddinsUsedByMigrationSource(configuration, source);
        }

        private void LoadAddinsUsedByMigrationSource(Configuration configuration, MigrationSource source)
        {
            foreach (var addin in source.Settings.UserIdentityLookup.LookupAddin)
            {
                string addinRefNameStr = addin.ReferenceName;
                LoadAddin(configuration, addinRefNameStr);
            }
        }

        private void AttachToDetachedConfiguration(Configuration config)
        {
            if (config.Manager != null && config.Manager != this)
            {
                throw new InvalidOperationException(Resource.ErrorAttachMultipleManagerToBusinessModel);
            }
            if (m_attachedConfiguration != null)
            {
                throw new InvalidOperationException(Resource.ErrorAttachManagerToMultipleModel);
            }

            m_attachedConfiguration = config;
            config.Manager = this;
        }

        internal static string Serialize(Type objectType, object obj)
        {
            var serializer = new XmlSerializer(objectType);
            using (var memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, obj);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (var sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        internal static object Deserialize(string blob, Type objectType)
        {
            object retval;

            if (string.IsNullOrEmpty(blob))
            {
                retval = null;
            }
            else
            {
                var serializer = new XmlSerializer(objectType);
                using (var strReader = new StringReader(blob))
                using (var xmlReader = XmlReader.Create(strReader))
                {
                    retval = serializer.Deserialize(xmlReader);
                }
            }
            return retval;
        }

        internal static string SettingXmlToString(CustomSettingsElement settingXml)
        {
            return Serialize(typeof(CustomSettingsElement), settingXml);
        }

        internal static CustomSettingsElement StringToSettingXmlConverter(string blob)
        {
            CustomSettingsElement obj = Deserialize(blob, typeof(CustomSettingsElement)) as CustomSettingsElement;
            return obj ?? new CustomSettingsElement();
        }

        internal static string GenericSettingXmlToString(SettingXml settingXml)
        {
            if (null == settingXml.Any || settingXml.Any.Count() == 0)
            {
                return string.Empty;
            }

            return settingXml.Any.First().OuterXml;
        }

        internal static string GenericSettingXmlSchemaToString(SettingXmlSchema settingXmlSchema)
        {
            if (null == settingXmlSchema.Any || settingXmlSchema.Any.Count() == 0)
            {
                return string.Empty;
            }

            return settingXmlSchema.Any.First().OuterXml;
        }

        //
        // Load support
        //
        internal static SettingXmlSchema StringToGenericSettingXmlSchemaConverter(string p)
        {
            // Always give back a SettingXmlSchema object
            var retVal = new SettingXmlSchema();

            if (!string.IsNullOrEmpty(p))
            {
                var xml = new XmlDocument();
                xml.LoadXml(p);

                retVal.Any = new[] { xml.DocumentElement };
            }
            return retVal;
        }

        internal static SettingXml StringToGenericSettingXmlConverter(string p)
        {
            // Always hand back a SettingXml object, even if there is nothing to carry back in it.
            var retVal = new SettingXml();

            if (!string.IsNullOrEmpty(p))
            {
                var xml = new XmlDocument();
                xml.LoadXml(p);

                retVal.Any = new[] { xml.DocumentElement };
            }
            return retVal;
        }
    }
}
