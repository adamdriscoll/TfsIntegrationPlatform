// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

#region Using directives

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Converters.WorkItemTracking;
using System.Collections;

using ConverterMain = Microsoft.TeamFoundation.Converters.WorkItemTracking.Common.ConverterMain;

#endregion

namespace Microsoft.TeamFoundation.Converters.Reporting
{
    /// <summary>
    /// This class contains the statistical details of migration/analysis like number of errors, warnings etc;
    /// </summary>
    public partial class ReportStatisticsStatisicsDetails
    {
        public ReportStatisticsStatisicsDetails()
        {
        }

        private ArrayList m_entityStatistics;

        [XmlArrayItem(ElementName = "WorkItem", Type = typeof(Detail))]
        public ArrayList PerWorkItemType
        {
            get { return m_entityStatistics; }
            set { m_entityStatistics = value; }
        }

        public void AddToEntityStatistics(string fromEntity, string toEntity, MigrationStatus status)
        {
            if (m_entityStatistics == null)
            {
                m_entityStatistics = new ArrayList();
            }

            // find if this entity already exist in current list
            foreach (Detail map in m_entityStatistics)
            {
                if (TFStringComparer.WorkItemType.Equals(map.ToEntity, toEntity))
                {
                    switch (status)
                    {
                        case MigrationStatus.Passed:
                            map.PassedCount++;
                            break;
                        case MigrationStatus.Failed:
                            map.FailedCount++;
                            break;
                        case MigrationStatus.Warning:
                            map.WarningCount++;
                            break;
                        case MigrationStatus.Skipped:
                            map.SkippedCount++;
                            break;
                    }
                    return;
                }
            }
            // not found this entity.. add it now
            Detail newMap = new Detail();
            newMap.FromEntity = fromEntity;
            newMap.ToEntity = toEntity;
            switch (status)
            {
                case MigrationStatus.Passed:
                    newMap.PassedCount++;
                    break;
                case MigrationStatus.Failed:
                    newMap.FailedCount++;
                    break;
                case MigrationStatus.Warning:
                    newMap.WarningCount++;
                    break;
                case MigrationStatus.Skipped:
                    newMap.SkippedCount++;
                    break;
            }

            m_entityStatistics.Add(newMap);
        }

        public enum MigrationStatus
        {
            Passed = 0,
            Failed = 1,
            Warning = 2,
            Skipped = 3
        }
    } // end of class ReportStatisticsStatisicsDetails

    /// <summary>
    /// This class stores count of pass, failed and skipped migration count for a entity
    /// </summary>
    public class Detail
    {
        private string m_FromEntity;
        private string m_ToEntity;
        private int m_passCount;
        private int m_failedCount;
        private int m_warningCount;
        private int m_skipCount;

        [XmlAttribute("From")]
        public string FromEntity
        {
            get { return m_FromEntity; }
            set { m_FromEntity = value; }
        }

        [XmlAttribute("To")]
        public string ToEntity
        {
            get { return m_ToEntity; }
            set { m_ToEntity = value; }
        }

        [XmlAttribute("Pass")]
        public int PassedCount
        {
            get { return m_passCount; }
            set { m_passCount = value; }
        }

        [XmlAttribute("Fail")]
        public int FailedCount
        {
            get { return m_failedCount; }
            set { m_failedCount = value; }
        }

        [XmlAttribute("Warning")]
        public int WarningCount
        {
            get { return m_warningCount; }
            set { m_warningCount = value; }
        }

        [XmlAttribute("Skipped")]
        public int SkippedCount
        {
            get { return m_skipCount; }
            set { m_skipCount = value; }
        }
    }

    /// <summary>
    /// This class represents the nodes in the summary section for target project and its url;
    /// </summary>
    [System.SerializableAttribute()]
    [XmlTypeAttribute]
    public class ReportSummaryTarget
    {
        public ReportSummaryTarget()
        {
        }

        #region Properties

        public string Uri
        {
            get { return m_uri; }
            set { m_uri = value; }
        }

        public string TeamProjectName
        {
            get { return m_teamProjectName; }
            set { m_teamProjectName = value; }
        }

        public string WorkItemTypeName
        {
            get { return m_workItemTypeName; }
            set { m_workItemTypeName = value; }
        }
        #endregion

        private string m_uri;
        private string m_teamProjectName;
        private string m_workItemTypeName;
    }

    /// <summary>
    /// Base class for Source Summary Report
    /// Will be derived by specific sources!
    /// </summary>
    [System.SerializableAttribute()]
    [XmlTypeAttribute]
    [XmlIncludeAttribute(typeof(PSReportSummarySource))]
    [XmlIncludeAttribute(typeof(CQReportSummarySource))]
    public abstract class ReportSummarySource
    {
        // nothing as of now!
    }

    /// <summary>
    /// This class represents the nodes in the summary section for CQ
    /// </summary>
    [System.SerializableAttribute()]
    [XmlTypeAttribute]
    public class CQReportSummarySource : ReportSummarySource
    {
        public CQReportSummarySource()
        {
        }

        #region Properties

        public string Query
        {
            get { return m_query; }
            set { m_query = value; }
        }
        public string User
        {
            get { return m_user; }
            set { m_user = value; }
        }
        public string Connection
        {
            get { return m_conn; }
            set { m_conn = value; }
        }
        public string Database
        {
            get { return m_database; }
            set { m_database = value; }
        }
        #endregion

        private string m_conn;
        private string m_database;
        private string m_query;
        private string m_user;
    }

    /// <summary>
    /// This class represents the nodes in the summary section for PS
    /// Derived from ReportSummarySource!
    /// </summary>
    [System.SerializableAttribute()]
    [XmlTypeAttribute]
    public class PSReportSummarySource : ReportSummarySource
    {
        public PSReportSummarySource()
        {
        }

        #region Properties

        public string QueryFileName
        {
            get { return m_queryFileName; }
            set { m_queryFileName = value; }
        }
        #endregion

        private string m_queryFileName;
    }

    /// <summary>
    /// This class corresponds to the node SourceAndDestination in the report file which gives the details of
    /// the source project mapping to the corresponding destination.
    /// </summary>
    public partial class ReportSummarySourceAndDestination
    {
        private ReportSummarySource m_summarySource;
        private ReportSummaryTarget m_summaryTarget;
        private MigWorkItemTypes m_workItemTypesField;

        public ReportSummarySourceAndDestination()
        {
            // Allocate the summarySource based on the source system!
            this.m_summaryTarget = new ReportSummaryTarget();
            this.m_workItemTypesField = new MigWorkItemTypes();
        }

        /// <remarks/>
        [XmlElementAttribute("ReportSummarySource")]
        public ReportSummarySource SummarySource
        {
            get
            {
                return this.m_summarySource;
            }
            set
            {
                this.m_summarySource = value;
            }
        }

        /// <remarks/>
        [XmlElementAttribute("SummaryTarget")]
        public ReportSummaryTarget SummaryTarget
        {
            get
            {
                return this.m_summaryTarget;
            }
            set
            {
                this.m_summaryTarget = value;
            }
        }

        /// <remarks/>
        [XmlElementAttribute("WorkItemTypes")]
        public MigWorkItemTypes WorkItemTypes
        {
            get
            {
                return this.m_workItemTypesField;
            }
            set
            {
                m_workItemTypesField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    public partial class MigWorkItemTypes
    {
        private WorkItemTypeTypes[] workItemTypesField;

        /// <remarks/>
        [XmlElementAttribute("WorkItemType")]
        public WorkItemTypeTypes[] WorkItemTypeTypes
        {
            get
            {
                return this.workItemTypesField;
            }
            set
            {
                this.workItemTypesField = value;
            }
        }
    }

    /// <remarks/>
    [System.Serializable]
    public class WorkItemTypeTypes
    {
        private string m_fromField;
        private string m_toField;

        /// <remarks/>
        [XmlAttribute("From")]
        public string From
        {
            get { return this.m_fromField; }
            set { this.m_fromField = value; }
        }

        /// <remarks/>
        [XmlAttribute("To")]
        public string To
        {
            get { return this.m_toField; }
            set { this.m_toField = value; }
        }
    }

    /// <remarks>
    /// Post migration report class
    /// </remarks>
    internal class PostMigrationReport
    {
        internal static void WriteIssue(string fromEntity,
                                        string toEntity,
                                        ReportStatisticsStatisicsDetails.MigrationStatus status,
                                        ReportIssueType type,
                                        string id,
                                        string item,
                                        IssueGroup group,
                                        string msg)
        {
            Debug.Assert(ConverterMain.MigrationReport != null);

            // also add in per work item section only if it is work item issue
            if (group == IssueGroup.Wi)
            {
                ConverterMain.MigrationReport.Statistics.StatisicsDetails.AddToEntityStatistics(fromEntity, toEntity, status);
            }

            // add the error in migration report
            if (type != ReportIssueType.Info)
            {
                ConverterMain.MigrationReport.WriteIssue(id, msg, item, null, group.ToString(), type, null);
            }
        }
    }

    internal enum IssueGroup
    {
        Wi,
        Witd,
        Config
    }
}
