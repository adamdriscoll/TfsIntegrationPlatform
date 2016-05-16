// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: VSTS connection class

#region Using directives

using System;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Converters.Reporting;
using Microsoft.TeamFoundation.Converters.Utility;

#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    /// <remarks>
    /// Class for connection to the VSTS system
    /// </remarks>
    sealed public class VSTSConnection
    {
        /// <summary>
        /// VSTSconnection constructor
        /// </summary>
        /// <param name="configuration"> configuration parameters</param>
        public VSTSConnection(string pBisUri, string pProjectName)
        {
            Initialize(pBisUri, pProjectName);
        }

        public VSTSConnection(XmlNode configuration)
        {
            string bisUri = configuration.SelectSingleNode("URI").InnerText.Trim();
            string projectName = configuration.SelectSingleNode("ProjectName").InnerText.Trim();

            Initialize(bisUri, projectName);
            
            // Ensure that the current user is part of Service Accounts
            VSTSUtil.IsCurrentUserInServiceAccount(bisUri);
        }

        private void Initialize(string pBisUri, string pProjectName)
        {
            m_tfs = UtilityMethods.ValidateAndGetTFS(pBisUri);
            m_bisUri = pBisUri;
            m_projectName = pProjectName;

            if (Common.ConverterMain.MigrationReport != null)
            {
                // set in Report
                Common.ConverterMain.MigrationReport.TargetSystem = Microsoft.TeamFoundation.Converters.Reporting.ReportTargetSystem.WorkItemTracking;
                Common.ConverterMain.MigrationReport.Summary.SourceAndDestination.SummaryTarget.Uri = m_bisUri;
                Common.ConverterMain.MigrationReport.Summary.SourceAndDestination.SummaryTarget.TeamProjectName = m_projectName;
            }

            Logger.WritePerf(LogSource.WorkItemTracking, "Establishing VSTS connection....");
            Display.StartProgressDisplay(UtilityMethods.Format(
                VSTSResource.VstsConnectionStart, m_bisUri, m_projectName));

            try
            {
                m_store = new WorkItemStore(m_tfs);
            }
            catch (ApplicationException ex)
            {
                Logger.WriteException(LogSource.WorkItemTracking, ex);
                ConverterException conEx = new ConverterException(ex.Message, ex);

                if (Common.ConverterMain.MigrationReport != null)
                {
                    Common.ConverterMain.MigrationReport.WriteIssue(string.Empty, ex.Message, string.Empty, null, IssueGroup.Config.ToString(), ReportIssueType.Critical);
                }
                throw conEx;
            }
            finally
            {
                Display.StopProgressDisplay();
            }

            // Make sure Currituck is also ready
            if (!store.Projects.Contains(projectName))
            {
                string errMsg = UtilityMethods.Format(
                    VSTSResource.InvalidProject, projectName, m_tfs.Name);
                if (Common.ConverterMain.MigrationReport != null)
                {
                    Common.ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty,
                        null, string.Empty, ReportIssueType.Critical);
                }
                throw new ConverterException(errMsg);
            }
            m_project = store.Projects[projectName];
            // set the connection handle to VSTSConstants
            VSTSConstants.VstsConn = this;

            Logger.WritePerf(LogSource.WorkItemTracking, "done");
        }

        /// <summary>
        /// Get work item type for this helper instance
        /// </summary>
        /// <param name="workItemTypeName"></param>
        /// <returns></returns>
        public WorkItemType GetWorkItemType(String workItemTypeName)
        {
            WorkItemType retWIT = null;
            if (m_project.WorkItemTypes.Contains(workItemTypeName))
            {
                retWIT = m_project.WorkItemTypes[workItemTypeName];
            }
            else
            {
                // reget the project handle from workitemstore
                Refresh();

                if (m_project.WorkItemTypes.Contains(workItemTypeName))
                {
                    retWIT = m_project.WorkItemTypes[workItemTypeName];
                }
            }
            return retWIT;
        }

        internal void Refresh()
        {
            store.RefreshCache();
            m_project = store.Projects[projectName];
        }

        // public properties
        public WorkItemStore store
        { get { return m_store; } }

        public string bisUri
        { get { return m_bisUri; } }

        public TeamFoundationServer Tfs
        { get { return m_tfs; } }

        public string projectName
        { get { return m_projectName; } }

        public Microsoft.TeamFoundation.WorkItemTracking.Client.Project project
        { get { return m_project; } }

        // private fields
        private WorkItemStore m_store;
        private string m_bisUri, m_projectName;
        private TeamFoundationServer m_tfs;
        private Microsoft.TeamFoundation.WorkItemTracking.Client.Project m_project;
    }
}
