// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    internal class SubversionVCAnalysisProvider : IAnalysisProvider
    {
        #region Private Members

        Dictionary<Guid, ChangeActionHandler> m_supportedChangeActions;
        Collection<ContentType> m_supportedContentTypes;

        ICollection<Guid> m_supportedChangeActionsOther;
        Collection<ContentType> m_supportedContentTypesOther;

        IServiceContainer m_analysisServiceContainer;

        ChangeActionRegistrationService m_changeActionRegistrationService;
        ChangeGroupService m_changeGroupService;
        ConfigurationService m_configurationService;
        
        ConflictManager m_conflictManagementService;

        HighWaterMark<long> m_hwmDelta;

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

            m_hwmDelta = new HighWaterMark<long>(Constants.HwmDelta);
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
            initializeSnapshotTable();
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
            //TODO implement actual analysis logic here
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
            throw new NotImplementedException();
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

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Establishes a connection to the configured subversion server
        /// </summary>
        private void initializeSubversionClient()
        {
        }

        /// <summary>
        /// 1. Read snapshot information from configuration.
        /// 2. Populate the 2 in-memory snapshot table
        /// </summary>
        private void initializeSnapshotTable()
        {
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

            //TODO Add the supported change actions
            //m_supportedChangeActions.Add(WellKnownChangeActionId.Add, subversionChangeActionHandlers.BasicActionHandler);
            //m_supportedChangeActions.Add(WellKnownChangeActionId.Edit, subversionChangeActionHandlers.BasicActionHandler);
            //m_supportedChangeActions.Add(WellKnownChangeActionId.Delete, subversionChangeActionHandlers.BasicActionHandler);
            //....
        }

        #endregion
    }
}
