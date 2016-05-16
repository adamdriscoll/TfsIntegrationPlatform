// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion;
using System.Globalization;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    internal class SubversionVCAnalysisProvider : IAnalysisProvider
    {
        #region Private Members

        private Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        private Collection<ContentType> m_supportedContentTypes;

        private ICollection<Guid> m_supportedChangeActionsOther;
        private Collection<ContentType> m_supportedContentTypesOther;

        private IServiceContainer m_analysisServiceContainer;

        private ChangeActionRegistrationService m_changeActionRegistrationService;
        private ChangeGroupService m_changeGroupService;
        private ConfigurationService m_configurationService;

        private ConfigurationManager m_configurationManager;
        private ConflictManager m_conflictManagementService;

        private HighWaterMark<int> m_hwmDelta;
        
        private SubversionAnalysisAlgorithms m_algorithm;

        private Repository m_repository;

        #endregion

        #region IAnalysisProvider implementation

        /// <summary>
        /// List of change actions supported by the analysis provider. 
        /// </summary>
        public Dictionary<Guid, ChangeActionHandler> SupportedChangeActions
        {
            get { return m_supportedChangeActions; }
        }

        /// <summary>
        /// List of change actions supported by the other side. 
        /// </summary>
        public ICollection<Guid> SupportedChangeActionsOther
        {
            set { m_supportedChangeActionsOther = value; }
        }

        /// <summary>
        /// List of content types supported by this provider
        /// </summary>
        public Collection<ContentType> SupportedContentTypes
        {
            get { return m_supportedContentTypes; }
        }

        /// <summary>
        /// List of content types supported by the other side
        /// </summary>
        public Collection<ContentType> SupportedContentTypesOther
        {
            set { m_supportedContentTypesOther = value; }
        }

        /// <summary>
        /// Initialize method of the analysis provider - acquire references to the services provided by the platform and register the HighWaterMark/>
        /// </summary>
        public void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            m_analysisServiceContainer = analysisServiceContainer;

            m_configurationService = (ConfigurationService)m_analysisServiceContainer.GetService(typeof(ConfigurationService));
            m_configurationManager = new ConfigurationManager(m_configurationService);

            m_hwmDelta = new HighWaterMark<int>(Constants.HwmDelta);
            m_configurationService.RegisterHighWaterMarkWithSession(m_hwmDelta);

            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new SubversionMigrationItemSerialzier());
        }

        /// <summary>
        /// Initialize method of the analysis provider. Establishes the connection to the subversion server
        /// </summary>
        public void InitializeClient()
        {
            initializeSubversionClient();
        }

        /// <summary>
        /// Register adapter's supported change actions.
        /// </summary>
        public void RegisterSupportedChangeActions(ChangeActionRegistrationService changeActionRegistrationService)
        {
            if (changeActionRegistrationService == null)
            {
                throw new ArgumentNullException("changeActionRegistrationService");
            }

            initiazlieSupportedChangeActions();

            m_changeActionRegistrationService = changeActionRegistrationService;

            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in m_supportedChangeActions)
            {
                // note: for now, VC adapter uses a single change action handler for all content types
                foreach (ContentType contentType in SupportedContentTypes)
                {
                    m_changeActionRegistrationService.RegisterChangeAction(
                        supportedChangeAction.Key,
                        contentType.ReferenceName,
                        supportedChangeAction.Value);
                }
            }
        }

        /// <summary>
        /// Register adapter's supported content types.
        /// </summary>
        public void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            initializeSupportedContentTypes();
        }

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        public void RegisterConflictTypes(ConflictManager conflictManager)
        {
            if (conflictManager == null)
            {
                throw new ArgumentNullException("conflictManager");
            }

            m_conflictManagementService = conflictManager;
            m_conflictManagementService.RegisterConflictType(new VCBranchParentNotFoundConflictType());

            //TODO Register conflicts that may occur here
            //m_conflictManagementService.RegisterConflictType(new VCInvalidPathConflictType());
            //...
        }

        /// <summary>
        /// Generate the context info table
        /// </summary>
        public void GenerateContextInfoTable()
        {
        }

        /// <summary>
        /// Generate the delta table
        /// </summary>
        public void GenerateDeltaTable()
        {
            int[] mappedChangesets = getMappedSubversionChanges();

            if (null == mappedChangesets || 0 == mappedChangesets.Length)
            {
                TraceManager.TraceInformation("There are no changes in the repository '{0}'", m_repository.URI);
                return;
            }

            var pager = new ChangeSetPageManager(m_repository, mappedChangesets, m_configurationManager.ChangesetCacheSize);

            do
            {
                TraceManager.TraceInformation("Analyzing Subversion revision {0} : {1}/{2}", pager.CurrentRevision, pager.CurrentIndex + 1, mappedChangesets.Length);

                ChangeSet changeSet = pager.Current;
                if (null != changeSet)
                {
                    int actions = analyzeChangeset(changeSet, mappedChangesets);
                    TraceManager.TraceInformation("Created {0} actions for subversion revision {1}", actions, pager.CurrentRevision);
                }
                else
                {
                    //TODO Maybe add a conflict here so that the user can decide what to do. This condition should not occur though
                    TraceManager.TraceWarning("Unable to retrieve the change details for revision {0}", pager.CurrentRevision);
                }

                m_hwmDelta.Update(pager.CurrentRevision);
                m_changeGroupService.PromoteDeltaToPending();
            }
            while (pager.MoveNext());

            pager.Reset();
        }

        /// <summary>
        /// Detects adapter-specific conflicts.
        /// </summary>
        /// <param name="changeGroup"></param>
        public void DetectConflicts(ChangeGroup changeGroup)
        {
        }

        /// <summary>
        /// Gets a unique string to identify the endpoint system, from which the migration data is retrieved from and written to
        /// </summary>
        /// <param name="migrationSourceConfig">The configuration data for the current session</param>
        /// <returns>Returns a unique id for the configured subversion repository</returns>
        public string GetNativeId(BusinessModel.MigrationSource migrationSourceConfig)
        {
            return m_repository.GetUniqueId().ToString();
        }

        #endregion

        #region IServiceProvider implementation

        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            return (IServiceProvider)this;
        }

        #endregion

        #region Internal Properties

        internal ConflictManager ConflictManager
        {
            get
            {
                return m_conflictManagementService;
            }
        }

        internal ConfigurationManager ConfigurationManager
        {
            get
            {
                return m_configurationManager;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (null != m_repository)
            {
                m_repository.Dispose();
                m_repository = null;
            }
        }

        #endregion

        #region Private Helpers

        private int[] getMappedSubversionChanges()
        {
            m_hwmDelta.Reload();
            Debug.Assert(m_hwmDelta.Value >= 0, "High water mark of delta table must be non-negtive");

            int latestChangeset = m_repository.GetLatestRevisionNumber();
            if (m_hwmDelta.Value >= latestChangeset)
            {
                // No new changesets on server, return.
                return new int[0];
            }

            int startingChangeset = m_hwmDelta.Value + 1;
            string skipComment = m_configurationService.GetValue<string>(Constants.SkipComment, "**NOMIGRATION**");

            var lookup = new HashSet<int>();

            foreach (var mappedPath in m_configurationManager.MappedServerPaths)
            {
                var records = m_repository.QueryHistoryRange(mappedPath, startingChangeset, latestChangeset, false);

                // Todo: Skip mirrored changes created by migration tool itself

                //Iterate across all records and filter out those records that are already in the list
                foreach (var record in records.Values)
                {
                    //check wether we alredy have this record. If this is the case, we can simply continue with the next record
                    if (lookup.Contains(record.Revision))
                    {
                        continue;
                    }

                    //The skip comment is empty. Therefore we can skip the evaluation of the skip comment
                    if (!string.IsNullOrEmpty(skipComment))
                    {
                        if (record.Comment != null && record.Comment.Contains(skipComment))
                        {
                            //The record has the skip commnet. Just print an information and resume with the next one
                            TraceManager.TraceInformation("LogRecord {0} contains the skip comment {1}", record.Revision, skipComment);
                            continue;
                        }
                    }

                    //Add the record to the hashmap. We alredy verified that this is the first record
                    lookup.Add(record.Revision);
                }
            }

            if (lookup.Count > 0)
            {
                int[] revisions = lookup.ToArray();
                Array.Sort(revisions);
                return revisions;
            }
            else
            {
                // No new changesets are found, update the HWM as current latest
                m_hwmDelta.Update(latestChangeset);
                return new int[0];
            }
        }

        /// <summary>
        /// Analyzes the TFS changeset to generate a change group.
        /// </summary>
        /// <param name="logRecord"></param>
        /// <returns></returns>
        private int analyzeChangeset(ChangeSet changeSet, int[] mappedChanges)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSet");
            }

            lazyInit();

            TraceManager.TraceInformation("Starting analysis of Subversion revision {0}", changeSet.Revision);

            int changeCount = 0;
            ChangeGroup group = m_changeGroupService.CreateChangeGroupForDeltaTable(changeSet.Revision.ToString(CultureInfo.InvariantCulture));
            populateChangeGroupMetaData(group, changeSet);
            if (changeSet != null)
            {
                m_algorithm.CurrentChangeset = changeSet;
                foreach (Change change in changeSet.Changes)
                {
                    // Either no snapshot start point is specified or we already passed the snapshot start point.
                    if (IsPathMapped(change.FullServerPath))
                    {
                        m_algorithm.Execute(change, group);
                    }
                    else
                    {
                        m_algorithm.ExecuteNonMapped(change, group, mappedChanges);
                    }
                }
                m_algorithm.Finish(group);
            }

            changeCount = group.Actions.Count;

            if (group.Actions.Count > 0)
            {
                group.Save();
            }

            if (changeCount == 0)
            {
                TraceManager.TraceInformation("No relevent changes found in SVN revision {0}", changeSet.Revision);
            }
            return changeCount;
        }

        /// <summary>
        /// Populates the changegroup with the needed meta data like author, comments and so on
        /// </summary>
        /// <param name="group">The changegroup that has to be populated</param>
        /// <param name="logRecord">The logRecord of subversion</param>
        private static void populateChangeGroupMetaData(ChangeGroup group, ChangeSet changeSet)
        {
            group.Owner = changeSet.Author;
            group.Comment = changeSet.Comment;
            group.ChangeTimeUtc = changeSet.CommitTime.ToUniversalTime();
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = changeSet.Revision;
        }

        /// <summary>
        /// Initializes the analysis algorithm
        /// </summary>
        private void lazyInit()
        {
            if (m_algorithm == null)
            {
                m_algorithm = new SubversionAnalysisAlgorithms(this);
            }
        }

        /// <summary>
        /// Determines whether the given server path is configured in the mapping file
        /// </summary>
        /// <param name="serverPath">The fully qualified SVN server path to the item</param>
        /// <returns></returns>
        internal bool IsPathMapped(Uri serverPath)
        {
            //Check whether any of the filter path is the prefix for the current items server path. First check is whether it is cloaked somewhere
            if (m_configurationManager.CloakedServerPaths.Where(x => PathUtils.IsChildItem(x, serverPath)).Count() > 0)
            {
                return false;
            }
            
            //the path is not cloaked. test whether it is part of any of the mapped paths
            return m_configurationManager.MappedServerPaths.Where(x => PathUtils.IsChildItem(x, serverPath)).Count() > 0;
        }

        /// <summary>
        /// Determines whether the given server path is configured in the mapping file
        /// </summary>
        /// <param name="serverPath">The fully qualified SVN server path to the item</param>
        /// <returns></returns>
        internal bool IsPathMapped(string serverPath)
        {
            Uri serverPathUri = new Uri(serverPath);
            //Check whether any of the filter path is the prefix for the current items server path. First check is whether it is cloaked somewhere
            if (m_configurationManager.CloakedServerPaths.Where(x => PathUtils.IsChildItem(x, serverPathUri)).Count() > 0)
            {
                return false;
            }

            //the path is not cloaked. test whether it is part of any of the mapped paths
            return m_configurationManager.MappedServerPaths.Where(x => PathUtils.IsChildItem(x, serverPathUri)).Count() > 0;
        }

        /// <summary>
        /// Establishes a connection to the configured subversion server
        /// </summary>
        private void initializeSubversionClient()
        {
            m_repository = Repository.GetRepository(m_configurationManager.RepositoryUri, m_configurationManager.Username, m_configurationManager.Password);
            m_repository.EnsureAuthenticated();
        }

        /// <summary>
        /// Initializes the collection that contains all supported <see cref="ContentType"/>s
        /// </summary>
        private void initializeSupportedContentTypes()
        {
            m_supportedContentTypes = new Collection<ContentType>();
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlChangeGroup);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledFile);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledFolder);
            m_supportedContentTypes.Add(WellKnownContentType.VersionControlledArtifact);
        }

        /// <summary>
        /// Initialize SupportedChangeActions list.
        /// </summary>
        private void initiazlieSupportedChangeActions()
        {
            var subversionChangeActionHandlers = new SubversionChangeActionHandlers(this);
            m_supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>();
            m_supportedChangeActions.Add(WellKnownChangeActionId.Add, subversionChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, subversionChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, subversionChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Branch, subversionChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Rename, subversionChangeActionHandlers.BasicActionHandler);
            m_supportedChangeActions.Add(WellKnownChangeActionId.Undelete, subversionChangeActionHandlers.BasicActionHandler);
        }

        #endregion
    }
}
