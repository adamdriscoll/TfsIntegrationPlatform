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
    internal class ClearQuestDiffProvider : IWITDiffProvider
    {
        private static InternalFieldSkipLogic s_skipFieldLogic = new InternalFieldSkipLogic();

        private ClearQuestOleServer.Session m_userSession;          // user session; instantiated after InitializeClient()
        #region not need until context sync requires us to access schema info
        // private ClearQuestOleServer.AdminSession m_adminSession;    // admin session; may be NULL if login info is not provided in config file 
        #endregion
        private IServiceContainer m_serviceContainer;
        private ConfigurationService m_configurationService;
        private CQRecordFilter m_cqRecordFilter;
        private bool m_provideForContentComparison;
        private ILinkProvider m_linkProvider;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer witDiffServiceContainer)
        {
            m_serviceContainer = witDiffServiceContainer;
            m_configurationService = (ConfigurationService)m_serviceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(m_configurationService != null, "Configuration service is not initialized");

            m_linkProvider = witDiffServiceContainer.GetService(typeof(ILinkProvider)) as ILinkProvider;
            Debug.Assert(m_linkProvider != null, "ILinkProvider service is not initialized");
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

        #region internal properties
        internal ILinkProvider LinkProvider
        {
            get { return m_linkProvider; }
        }
        #endregion

        #region IWITDiffProvider implementation

        /// <summary>
        /// The implementation can perform any one-time initialization here.
        /// Some adapter implementations may not need to perform any such initialization.
        /// It takes as optional arguments a filterString and a version that would be applied during subsequent queries.
        /// </summary>
        /// <param name="filterString">A string that specifies some filtering condition; if null or empty no additional filtering is applied</param>
        /// <param name="version">The version of the item; if null or empty, the tip version is accessed</param>
        /// <param name="provideForContentComparison">If true, any IDiffItem returned by any method should include the contents of the work item for comparison purposed.
        /// If false, detailed content data can be left out.
        /// </param>
        public void InitializeForDiff(string filterString, bool provideForContentComparison)
        {
            m_cqRecordFilter = CQRecordFilters.ParseFilterPath(filterString, m_userSession);
            m_provideForContentComparison = provideForContentComparison;
        }

        /// <summary>
        /// Enumerate the diff items found based on the query passed in as well as the filterString and version passed
        /// to InitializeForDiff.  The return type is IEnumerable<> so that adapter implementations do not need download and keep 
        /// all of the IWITDiffItems in memory at once.
        /// </summary>
        /// <param name="queryCondition">A string that specifies a query used to select a subset of the work items defined by 
        /// the set that the filter string identified.</param>
        /// <returns>An enumeration of IWITDiffItems each representing a work item to be compared by the WIT Diff operation</returns>
        public IEnumerable<IWITDiffItem> GetWITDiffItems(string queryCondition)
        {
            List<CQRecordFilter> recordFilters = new List<CQRecordFilter>();
            recordFilters.Add(m_cqRecordFilter);
            if (!string.IsNullOrEmpty(queryCondition))
            {
                CQRecordFilter queryConditionRecordFilter = new CQRecordFilter(m_cqRecordFilter.RecordType, m_cqRecordFilter.SelectFromTable, queryCondition);
                recordFilters.Add(queryConditionRecordFilter);
            }
            CQRecordSqlQuery recordQuery = new CQRecordSqlQuery(m_userSession, recordFilters, null, this);

            foreach (OAdEntity record in recordQuery)
            {
                if (record != null) // this if check is HACK
                {
                    OAdHistoryFields histFields = CQWrapper.GetHistoryFields(record);
                    int historyFldCount = CQWrapper.HistoryFieldsCount(histFields);
                    yield return new CQWITDiffItem(this, record, historyFldCount - 1);
                }
            }
        }

        /// <summary>
        /// Return a IWITDiffItem representing a single work item as identified by the adapter specific workItemId string
        /// </summary>
        /// <param name="workItemId"></param>
        /// <returns></returns>
        public IWITDiffItem GetWITDiffItem(string workItemId)
        {
            string queryCondition = String.Format(CultureInfo.InvariantCulture, "entity_dbid = {0}", workItemId);
            foreach (IWITDiffItem witDiffItem in GetWITDiffItems(queryCondition))
            {
                return witDiffItem;
            }
            return null;
        }

        /// <summary>
        /// IgnoreFieldInComparison is called to allow an adapter to specify that a named field should not be compared
        /// in the diff operation
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool IgnoreFieldInComparison(string fieldName)
        {
            return s_skipFieldLogic.SkipField(fieldName);
        }

        /// <summary>
        /// Give the IWITDiffProvider a chance to cleanup any reources allocated during InitializeForDiff()
        /// </summary>
        public void Cleanup()
        {
            m_cqRecordFilter = null;
            // TODO: Close down stuff opened in InitializeClient, or need a TerminateClient() for that?
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
            if (serviceType == typeof(IWITDiffProvider))
            {
                return this;
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
            MigrationContext = new ClearQuestMigrationContext(m_userSession, migrationSourceConfig);
        }

        #region internal properties
        internal ClearQuestMigrationContext MigrationContext
        {
            get;
            set;
        }
        #endregion

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
