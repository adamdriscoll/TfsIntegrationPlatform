// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class ClearQuestSyncMonitorProvider : ISyncMonitorProvider
    {
        private ClearQuestOleServer.Session m_userSession;          // user session; instantiated after InitializeClient()
        private CQRecordFilters m_filters;
        #region not need until context sync requires us to access schema info
        // private ClearQuestOleServer.AdminSession m_adminSession;    // admin session; may be NULL if login info is not provided in config file 
        #endregion
        private IServiceContainer m_serviceContainer;
        private ConfigurationService m_configurationService;
        private ClearQuestMigrationContext m_migrationContext;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer syncMonitorServiceContainer)
        {
            m_serviceContainer = syncMonitorServiceContainer;
            m_configurationService = (ConfigurationService)m_serviceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient(MigrationSource migrationSource)
        {
            try
            {
                InitializeCQClient();
            }
            catch (ClearQuestCOMDllNotFoundException cqComNotFoundEx)
            {
                UtilityMethods.HandleCOMDllNotFoundException(cqComNotFoundEx, null, null);
            }
            catch (ClearQuestCOMCallException cqComCallEx)
            {
                UtilityMethods.HandleCQComCallException(cqComCallEx, null, null);
            }
            catch (Exception ex)
            {
                UtilityMethods.HandleGeneralException(ex, null, null);
            }
        }

        #region ISyncMonitorProvider implementation
        public ChangeSummary GetSummaryOfChangesSince(string lastProcessedChangeItemId, List<string> filterStrings)
        {
            // lastProcessedChangeItemId is in a form such as "Defect:UCM0100019437:history:0"
            // Parse it and query for the record with that Id to get the time is was last changed
            string[] identity = UtilityMethods.ParseCQRecordMigrationItemId(lastProcessedChangeItemId);
            OAdEntity lastProcessedRecord = CQWrapper.GetEntity(m_userSession, identity[0], identity[1]);
            string lastProcessedRecordAuthor; 
            DateTime lastProcessedRecordChangeDate;
            ClearQuestRecordItem.FindLastRevDtls(lastProcessedRecord, out lastProcessedRecordAuthor, out lastProcessedRecordChangeDate);

            string lastProcessedRecordChangeDateStr = lastProcessedRecordChangeDate.ToString(m_migrationContext.CQQueryDateTimeFormat, CultureInfo.InvariantCulture);
            if (lastProcessedRecordChangeDateStr.LastIndexOf('.') >= 0)
            {
                lastProcessedRecordChangeDateStr = lastProcessedRecordChangeDateStr.Substring(0, lastProcessedRecordChangeDateStr.LastIndexOf('.')); // drop the millisec
            }

            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeCount = 0;
            changeSummary.FirstChangeModifiedTimeUtc = DateTime.MinValue;

            foreach (CQRecordFilter filter in m_filters)
            {
                CQRecordQueryBase recordQuery =
                    CQRecordQueryFactory.CreateQuery(m_userSession, filter, lastProcessedRecordChangeDateStr, this);

                foreach (OAdEntity record in recordQuery)
                {
                    // HACK HACK
                    if (record != null) // this if check is HACK
                    {
                        DateTime lastChangeDate;
                        string lastAuthor;
                        ClearQuestRecordItem.FindLastRevDtls(record, out lastAuthor, out lastChangeDate);

                        // Make sure the lastChangeDate on this record is after the lastProcessedRecordChangeDate before counting it in the backclog
                        // because the query issued above is imprecise because the milliseconds are dropped
                        if (lastChangeDate > lastProcessedRecordChangeDate)
                        {
                            changeSummary.ChangeCount++;
                            DateTime lastChangeDateUtc = lastChangeDate.ToUniversalTime();
                            if (changeSummary.FirstChangeModifiedTimeUtc == DateTime.MinValue ||
                                lastChangeDateUtc < changeSummary.FirstChangeModifiedTimeUtc)
                            {
                                changeSummary.FirstChangeModifiedTimeUtc = lastChangeDateUtc;
                            }
                        }
                    }
                }
            }

            return changeSummary;
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
            if (serviceType == typeof(ISyncMonitorProvider))
            {
                return this;
            }

            if (serviceType == typeof(ClearQuestMigrationContext))
            {
                return m_migrationContext;
            }

            return null;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion

        private void InitializeCQClient()
        {
            Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrationSourceConfig = m_configurationService.MigrationSource;
            string dbSet = migrationSourceConfig.ServerUrl;
            string userDb = migrationSourceConfig.SourceIdentifier;

            ICredentialManagementService credManagementService =
                m_serviceContainer.GetService(typeof(ICredentialManagementService)) as ICredentialManagementService;

            ICQLoginCredentialManager loginCredManager = 
                CQLoginCredentialManagerFactory.CreateCredentialManager(credManagementService, migrationSourceConfig);

            // connect to user session
            UserSessionConnConfig = new ClearQuestConnectionConfig(loginCredManager.UserName,
                                                           loginCredManager.Password,
                                                           userDb,
                                                           dbSet);
            m_userSession = CQConnectionFactory.GetUserSession(UserSessionConnConfig);

            #region we won't need admin session until we start syncing cq schema
            //// connect to admin session
            //if (!string.IsNullOrEmpty(loginCredManager.AdminUserName))
            //{
            //    AdminSessionConnConfig = new ClearQuestConnectionConfig(loginCredManager.AdminUserName,
            //                                                                loginCredManager.AdminPassword ?? string.Empty,
            //                                                                userDb,
            //                                                                dbSet);
            //    m_adminSession = CQConnectionFactory.GetAdminSession(AdminSessionConnConfig);
            //} 
            #endregion

           // parse the filter strings in the configuration file
            m_filters = new CQRecordFilters(m_configurationService.Filters, m_userSession);

            m_migrationContext = new ClearQuestMigrationContext(m_userSession, migrationSourceConfig);
        }

        #region private properties
        private ClearQuestConnectionConfig UserSessionConnConfig
        {
            get;
            set;
        }

        private ClearQuestConnectionConfig AdminSessionConnConfig
        {
            get;
            set;
        }
        #endregion
    }

}
