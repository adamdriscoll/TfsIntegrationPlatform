// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Encapsulates the Clear Quest connection details
// and provide the methods for CQ User Session and Admin Session
// Initializes using the Source CQ node as given in config xml file

#region Using directives

using System;
using ClearQuestOleServer;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.Reporting;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using System.IO;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    class CQConnection
    {
        #region Private Members

        private Session cqUserSession;
        private AdminSession cqAdminSession;

        // provided by customer in config xml file
        private string m_user;    // user name
        private string m_pwd;     // pwd
        private string m_userDb;  // user db
        private string m_dbSet;   // db connection
        private string m_query;   // CQ query
        private string m_configFile;
        private OAdQuerydef m_queryDef;

        #endregion

        /// <summary>
        /// Gets the Clear Quest User Session
        /// </summary>
        /// <returns>Session object</returns>
        public Session GetUserSession()
        {
            return cqUserSession;
        }

        /// <summary>
        /// Gets the Clear Quest Admin Session
        /// </summary>
        /// <returns>Admin Session object</returns>
        public AdminSession GetAdminSession()
        {
            return cqAdminSession;
        }

        /// <summary>
        /// Initializes the class by reading the connection
        /// data from the xml node for source (CQ)
        /// </summary>
        /// <param name="configuration">Source configuration xml node</param></param>
        public CQConnection(XmlNode configuration, string configFile)
        {
            m_configFile = configFile;
            // Validate configuration for CQ using XSD
            UtilityMethods.ValidateXmlFragment(Path.GetFileName(configFile), configuration.FirstChild, CommonConstants.CQConverterXsdFile);

            // do not check for a valid connection string as for a local CQ installation
            // user can connect to CQ without providing a connection
            XmlNode xmlNode = configuration.SelectSingleNode("ClearQuest/ConnectionName");
            m_dbSet = xmlNode.InnerText.Trim();

            xmlNode = configuration.SelectSingleNode("ClearQuest/UserDatabase");
            m_userDb = xmlNode.InnerText.Trim();

            xmlNode = configuration.SelectSingleNode("ClearQuest/QueryName");
            m_query = xmlNode.InnerText.Trim();

            xmlNode = configuration.SelectSingleNode("ClearQuest/UserID");
            m_user = xmlNode.InnerText.Trim();

            CQReportSummarySource cqSummary = new CQReportSummarySource();
            cqSummary.Connection = m_dbSet;
            cqSummary.Database = m_userDb;
            cqSummary.Query = m_query;
            cqSummary.User = m_user;

            ConverterMain.MigrationReport.Summary.SourceAndDestination.SummarySource = cqSummary;
            ConverterMain.MigrationReport.SourceSystem = Microsoft.TeamFoundation.Converters.Reporting.ReportSourceSystem.ClearQuest;

        } // end of CQConnection Ctor

        /// <summary>
        /// Creates User session, Admin session and executes the query
        /// </summary>
        public void Initialize()
        {
            m_pwd = System.Environment.GetEnvironmentVariable("CQUserPwd");
            if (m_pwd != null)
            {
                m_pwd = m_pwd.Trim();
            }
            if (m_pwd == null)
            {
                Console.Write(UtilityMethods.Format(CQResource.CQ_ENTER_PWD, m_user));
                m_pwd = Microsoft.TeamFoundation.Converters.Utility.LocalizedPasswordReader.ReadLine();
                Display.NewLine();
            }

            CreateUserSession();
            CreateAdminSession();

            try
            {
                m_queryDef = CQWrapper.GetQueryDef(CQWrapper.GetWorkSpace(cqUserSession), m_query);
            }
            catch (ConverterException conEx)
            {
                string errMsg = UtilityMethods.Format(CQResource.CQ_INVALID_QUERY,
                                                      CurConResource.Analysis,
                                                      m_query, m_configFile);
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty /* no item */,
                    null, "Config", ReportIssueType.Critical);

                throw new ConverterException(errMsg, conEx);
            }
        }

        /// <summary>
        /// Creates a User Session
        /// </summary>
        private void CreateUserSession()
        {
            // create and initialize session object
            cqUserSession = CQWrapper.CreateSession();
            Logger.WritePerf(LogSource.CQ, "Connecting to CQ User Session");
            CQWrapper.UserLogon(cqUserSession, m_user, m_pwd, m_userDb, (int)CQConstants.SessionType.PRIVATE, m_dbSet, m_configFile);
            Logger.WritePerf(LogSource.CQ, "Connected to CQ User Session");
        } // end of CreateUserSession

        /// <summary>
        /// Creates a Admin Session
        /// </summary>
        private void CreateAdminSession()
        {
            // create and initialize admin session object
            cqAdminSession = CQWrapper.CreateAdminSession();
            Logger.WritePerf(LogSource.CQ, "Connecting to CQ Admin Session");
            CQWrapper.AdminLogon(cqAdminSession, m_user, m_pwd, m_dbSet, m_configFile);
            Logger.WritePerf(LogSource.CQ, "Connected to CQ Admin Session");
            OAdUser cqUser = CQWrapper.GetUser(cqAdminSession, m_user);
            try
            {
                if (!CQWrapper.IsSuperUser(cqUser))
                {
                    string errMsg = UtilityMethods.Format(CQResource.CQ_NO_ADMIN_RIGHT, 
                                                          CurConResource.Analysis, 
                                                          m_configFile);
                    Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                    ConverterMain.MigrationReport.WriteIssue(String.Empty,
                             errMsg, string.Empty /* no item */, null, "Config", ReportIssueType.Critical);

                    throw new ConverterException(errMsg);
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                string errMsg = UtilityMethods.Format(CQResource.CQ_NO_ADMIN_RIGHT, m_configFile);
                Logger.WriteException(LogSource.CQ, ex);
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty /* no item */,
                    null, "Config", ReportIssueType.Critical);

                throw new ConverterException(errMsg);
            }
        } // end of CreateAdminSession

        /// <summary>
        /// Returns the stored CQ query as given in config xml
        /// </summary>
        /// <returns>Clear Quest query definition handle</returns>
        public OAdQuerydef QueryDefinition
        {
            get { return m_queryDef; }
        }

        public string ConnectionName
        {
            get { return m_dbSet; }
        }

        public string UserDbName
        {
            get { return m_userDb; }
        }

        public string QueryName
        {
            get { return m_query; }
        }
    } // end of class CQConnection
}
