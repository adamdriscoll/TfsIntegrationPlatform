// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: VSTS Work item helper class used by PSConverter, 
//              ClearQuestConverter etc for migrating in memory work 
//              item into Currituck 

#region Using directives

using System;
using System.Globalization;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using Microsoft.TeamFoundation.Converters.Reporting;
using CurClient = Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Server;

#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    /// <remarks>
    /// VSTS Work Item Helper Class
    /// </remarks>
    public partial class VSTSWorkItemHelper
    {
        /// <summary>
        /// Get Information regarding Work Item currently getting migrated
        /// </summary>
        /// <returns>String Info</returns>
        public string GetCurrentItemInfo()
        {
            // Do NOT localize this.
            return string.Format(CultureInfo.InvariantCulture,
                "Item migrated to Work Item Tracking :--\r\n\tProject Name: {0}\r\n\tWork Item Type: {1}\r\n\tBug Number: {2}\r\n\tBug URI: {3}",
                m_vstsConnection.projectName, m_witName, m_currentWorkItem.Id, 
                string.Format(m_CurrituckUri, m_vstsConnection.Tfs.Uri, m_currentWorkItem.Id));
        }

        /// <summary>
        /// Validate the complete field mappings given as input
        /// </summary>
        private void ValidateFieldMaps()
        {
            WorkItem wi = new WorkItem(m_vstsConnection.GetWorkItemType(m_witName));
            Display.StartProgressDisplay(VSTSResource.ValidatingVSTSFieldsStart);
            StringBuilder invalidFields = new StringBuilder();

            // Check for to fields..
            // Currently this only validates field names only!
            foreach (FieldMapsFieldMap fnmap in m_maps.FieldMap)
            {
                if (TFStringComparer.XmlAttributeValue.Equals(fnmap.exclude, "false"))
                {
                    if (!wi.Fields.Contains(fnmap.to))
                    {
                        if (invalidFields.Length != 0)
                            invalidFields.Append(", ");

                        invalidFields.Append(fnmap.to);
                    }
                    if (VSTSConstants.SkipFields.ContainsKey(fnmap.to) &&
                        !m_skippedFields.ContainsKey(fnmap.to))
                    {
                        m_skippedFields.Add(fnmap.to, String.Empty);
                        string warningMsg = UtilityMethods.Format(
                            VSTSResource.VstsCannotEditFields, fnmap.to);
                        ConverterMain.MigrationReport.WriteIssue(string.Empty, warningMsg, string.Empty, string.Empty, "Misc", ReportIssueType.Warning, null);
                    }
                }
            }
            Display.StopProgressDisplay();
            if (invalidFields.Length != 0)
            {
                string errMsg = UtilityMethods.Format(
                    VSTSResource.InvalidFieldName, m_fieldMapFile,
                    invalidFields.ToString(), m_witName);
                ConverterMain.MigrationReport.WriteIssue(String.Empty,
                     ReportIssueType.Critical, errMsg, string.Empty, null, string.Empty, null);

                throw new ConverterException(errMsg);
            }
            Display.NewLine();
        }

        /// <summary>
        /// Constructor for VSTS work Item Helper
        /// </summary>
        /// <param name="conn">Team Foundation Connection</param>
        /// <param name="witName">Work Item Type Name</param>
        /// <param name="maps">Field Maps reference</param>
        /// <param name="userMap">User Maps refernce</param>
        /// <param name="sourceConvIndex">Source Converter Index</param>
        public VSTSWorkItemHelper(VSTSConnection conn,
                                  string witName,
                                  FieldMaps maps,
                                  UserMappings userMap,
                                  int sourceConvIndex,
                                  string fieldMapFile)
        {
            m_vstsConnection = conn;
            m_maps = maps;
            m_witName = witName;
            m_userMappings = userMap;
            m_fieldMapFile = fieldMapFile;

            // validate the field mappings
            ValidateFieldMaps();

            // build the field mappings
            BuildFieldMappings();

            CurClient.WorkItemType witType = m_vstsConnection.GetWorkItemType(m_witName);

            m_convSourceIndex = sourceConvIndex;

            // Check if the Duplicate WI ID is used by CQ!
            if (witType.FieldDefinitions.Contains(m_duplicateWiId) &&
                !m_fieldMappings.Contains(m_duplicateWiId))
            {
                m_canSetDuplicateWiId = true;
            }

            m_wi = CreateNewWorkItem();

            // prepare intial work item snapshot with all required and available field values
            foreach (string reqField in m_requiredFieldsForCreatingWI)
            {
                Field fld = m_wi.Fields[reqField];
                Debug.Assert(fld != null, "Null handle for core field while preparing initial snapshot");
                if (fld != null)
                {
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Base WI Snapshot - Field [{0}], Value [{1}]", fld.ReferenceName, fld.Value);
                    m_baseWiSnapShot.Add(fld.ReferenceName, fld.Value);
                }
            }

            m_baseWiSnapShot.Add(VSTSConstants.CreatedDateFieldRefName, VSTSConstants.CurrentDate);
            m_baseWiSnapShot.Add(VSTSConstants.CreatedByFieldRefName, "Converter");

            m_MigStatusField = m_wi.Fields[m_migrationStatusFieldName];
            VSTSConstants.MigrationStatusField = m_MigStatusField.ReferenceName;

            // initialize web service URI's
            WSHelper.SetupProxy(conn.Tfs.Name);
        }

        /// <summary>
        /// Construct the in memory table for all the field name/value/type mappings
        /// ( converter does not support type mappings! )
        /// </summary>
        private void BuildFieldMappings()
        {
            m_fieldMappings = new Hashtable(TFStringComparer.OrdinalIgnoreCase);
            m_inverseFieldMappings = new Hashtable(TFStringComparer.OrdinalIgnoreCase);
            m_valueMappings = new Hashtable(TFStringComparer.Ordinal);

            foreach (FieldMapsFieldMap fnmap in m_maps.FieldMap)
            {
                // check if this field map is already not there
                if (m_fieldMappings[fnmap.from] == null)
                {
                    // Add this mapping in the inverse field mappings also
                    if (!m_inverseFieldMappings.Contains(fnmap.to))
                    {
                        m_inverseFieldMappings.Add(fnmap.to, fnmap);
                    }
                    else
                    {
                        Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, "Field Map for target field {0} already exist", fnmap.to);
                    }


                    m_fieldMappings.Add(fnmap.from, fnmap);
                    if (fnmap.ValueMaps != null)
                    {
                        if (fnmap.ValueMaps.id != null)
                        {
                            m_valueMappings.Add(fnmap.ValueMaps.id, fnmap.ValueMaps);
                        }

                        if (fnmap.ValueMaps.refer != null &&
                            !m_valueMappings.Contains(fnmap.ValueMaps.refer))
                        {
                            // check if this is usermap!
                            if (!TFStringComparer.XmlAttributeValue.Equals(fnmap.ValueMaps.refer, "UserMap"))
                            {
                                string errMsg = UtilityMethods.Format(
                                    VSTSResource.InvalidFieldMapReference,
                                    m_fieldMapFile, fnmap.ValueMaps.refer);
                                ConverterMain.MigrationReport.WriteIssue(String.Empty,
                                     ReportIssueType.Critical, errMsg, string.Empty, null, string.Empty, null);

                                throw new ConverterException(errMsg);
                            }
                        }
                    }
                }
                else
                {
                    // should move it to WriteOnce
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, "Field Map for source field {0} already exist", fnmap.from);
                }
            }
        }

        /// <summary>
        /// Get the Currituck field name from the Field Map
        /// </summary>
        /// <param name="from">Source field name</param>
        /// <param name="fvalue">Field value if changed because of some value map</param>
        /// <returns>Currituck Field Name</returns>
        private string GetMappedFieldName(string fName, ref object fValue)
        {
            string toval = string.Empty;
            FieldMapsFieldMap nmap = (FieldMapsFieldMap)m_fieldMappings[fName];

            if (nmap != null)
            {
                FieldMapsFieldMapValueMaps fvmaps = null;
                toval = nmap.to;
                // check if there are value mappings for this field!
                if (nmap.ValueMaps != null)
                {
                    fvmaps = nmap.ValueMaps;
                    if (fvmaps.refer != null && !TFStringComparer.XmlAttributeValue.Equals(fvmaps.refer, "UserMap"))
                    {
                        fvmaps = (FieldMapsFieldMapValueMaps)m_valueMappings[nmap.ValueMaps.refer];
                    }

                    if (TFStringComparer.XmlAttributeValue.Equals(fvmaps.refer, "UserMap"))
                    {
                        string currUser = fValue.ToString();
                        string toUser = currUser;
                        if (m_userMappings != null && m_userMappings.UserMap != null)
                        {
                            foreach (UserMappingsUserMap userMap in m_userMappings.UserMap)
                            {
                                if (TFStringComparer.UserName.Equals(userMap.From, currUser))
                                {
                                    toUser = userMap.To;
                                    break;
                                }
                            }
                        }
                        if (m_vstsConnection.store.UserDisplayMode == UserDisplayMode.AccountName)
                        {
                            // for alias mode, no resolution is ever required
                            fValue = toUser;
                        }
                        else
                        {
                            // resolve the user for Display Name mode
                            if (VSTSUtil.ResolvedUsers.Contains(toUser))
                            {
                                fValue = VSTSUtil.ResolvedUsers[toUser];
                            }
                            else
                            {
                                Identity userIdentity = VSTSUtil.ResolveUser(m_vstsConnection, toUser);
                                if (userIdentity == null)
                                {
                                    CommonConstants.UnresolvedUsers.Append(string.Concat(toUser, ", "));
                                    VSTSUtil.ResolvedUsers.Add(toUser, toUser);
                                    fValue = toUser;
                                }
                                else
                                {
                                    VSTSUtil.ResolvedUsers.Add(toUser, userIdentity.DisplayName);
                                    fValue = userIdentity.DisplayName;
                                }
                            }
                        }
                    }
                    else if (fvmaps.ValueMap != null)
                    {
                        int score = 0;
                        ValueMap bestMatch = null;
                        string fldVal = fValue.ToString();
                        foreach (ValueMap fvmap in fvmaps.ValueMap)
                        {
                            if (fldVal.StartsWith(fvmap.from, StringComparison.OrdinalIgnoreCase))
                            {
                                // found one candidate
                                if (fvmap.from.Length > score)
                                {
                                    score = fvmap.from.Length;
                                    bestMatch = fvmap;
                                }
                            }
                        }
                        if (score > 0)
                        {
                            fValue = String.Concat(bestMatch.to, fValue.ToString().Substring(bestMatch.from.Length));
                        }
                    }
                }
                // If it is Area Path, calculate the Area ID and update that field
                if (TFStringComparer.WorkItemFieldFriendlyName.Equals(nmap.to, VSTSConstants.AreaPathField) ||
                    TFStringComparer.WorkItemFieldFriendlyName.Equals(nmap.to, VSTSConstants.IterationPathField))
                {
                    Node.TreeType type = Node.TreeType.Area;
                    if (TFStringComparer.WorkItemFieldFriendlyName.Equals(nmap.to, VSTSConstants.AreaPathField))
                    {
                        toval = VSTSConstants.AreaIdField;
                        type = Node.TreeType.Area;
                    }
                    else
                    {
                        toval = VSTSConstants.IterationIdField;
                        type = Node.TreeType.Iteration;
                    }
                    

                    Node tmpNode = null;
                    string fValueStr = fValue.ToString().Trim();

                    if(fValueStr.Length != 0)
                    {
                        tmpNode = VSTSUtil.FindNodeInSubTree(m_vstsConnection, fValueStr, type);
                    }
                    if (tmpNode != null)
                    {
                        if (type == Node.TreeType.Area)
                        {
                            // update the current work item structure with this area id
                            m_vstsWorkItem.areaId = tmpNode.Id;
                        }
                        fValue = tmpNode.Id;
                    }
                    else
                    {
                        if (fvmaps != null && fvmaps.defaultValue != null && fvmaps.defaultValue.Trim().Length > 0)
                        {
                            string defaultValueStr = fvmaps.defaultValue.Trim();
                            tmpNode = VSTSUtil.FindNodeInSubTree(m_vstsConnection, defaultValueStr, type);
                            if (tmpNode != null)
                            {
                                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning,
                                    UtilityMethods.Format(
                                        "Using: '{0}' instead of: '{1}' for: '{2}'",
                                        defaultValueStr, fValueStr, nmap.to));
                                ConverterMain.MigrationReport.WriteIssue("MigrationCheck", ReportIssueType.Info,
                                    UtilityMethods.Format(
                                        VSTSResource.UsingDefaultValue,
                                        defaultValueStr, fValueStr, nmap.to));
                                fValue = tmpNode.Id;
                            }
                            else
                            {
                                string errMsg = UtilityMethods.Format(
                                    VSTSResource.InvalidDefaultValueMap,
                                    m_sourceWorkItemId, nmap.to, fValueStr,
                                    m_fieldMapFile);
                                throw new ConverterException(errMsg);
                            }
                        }
                        else
                        {
                            string errMsg = UtilityMethods.Format(
                                VSTSResource.NullDefaultValueMap,
                                m_sourceWorkItemId, nmap.to, fValueStr,
                                m_fieldMapFile);
                            throw new ConverterException(errMsg);
                        }
                    }
                }
            }
            return toval;
        }

        /// <summary>
        /// Check if the work item is already migrated
        /// This function also sets the context for the current work item
        /// </summary>
        /// <param name="hid">List of criteria to match</param>
        /// <returns>true if migrated</returns>
        public bool IsWIMigrated(ArrayList checkList)
        {
            try
            {
                // Add project name and work item type names to the check list!
                checkList.Add(new WorkItemNameValueRelation("System.WorkItemType", m_witName));
                checkList.Add(new WorkItemNameValueRelation("System.AreaPath", m_vstsConnection.projectName,
                    WorkItemNameValueRelation.RelationShipType.UNDER));

                m_currentWorkItem = VSTSUtil.FindWorkItem(m_vstsConnection, checkList);
                if (m_currentWorkItem != null)
                {
                    object statusObj = m_currentWorkItem["Migration Status"];
                    if (statusObj != null)
                    {
                        Debug.Assert(statusObj is string);
                        if (String.Equals(statusObj as string, "Done", StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
            }
            return (false);
        }

        /// <summary>
        /// Check if the work item exists
        /// and set the context for the current work item
        /// </summary>
        /// <param name="workItemId">Work Item handle</param>
        /// <returns>true if exists and successfully sets to current</returns>
        public void SetCurrentWorkItem(object workItem)
        {
            m_currentWorkItem = (WorkItem)workItem;
        }

        /// <summary>
        /// Get current work item.
        /// </summary>
        /// <returns></returns>
        public object GetCurrentWorkItem()
        {
            return m_currentWorkItem;
        }

        /// <summary>
        /// Another wrapper around find work item
        /// </summary>
        /// <param name="checkList"></param>
        /// <returns></returns>
        public WorkItem FindWorkItem(ArrayList checkList)
        {
            // Add project name and work item type names to the check list!
            checkList.Add(new WorkItemNameValueRelation("System.WorkItemType", m_witName));
            checkList.Add(new WorkItemNameValueRelation("System.AreaPath", m_vstsConnection.projectName,
                WorkItemNameValueRelation.RelationShipType.UNDER));

            WorkItem tempWi = VSTSUtil.FindWorkItem(m_vstsConnection, checkList);
            return (tempWi);
        }

        /// <summary>
        /// Check if the current work item is valid.
        /// Useful in case of incremental migration
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentWorkItemValid()
        {
            return (m_currentWorkItem != null);
        }

        /// <summary>
        /// Get history count for the current work item
        /// Used during incremental migration
        /// </summary>
        /// <returns>history count</returns>
        public int GetCurrentWorkItemHistoryCount()
        {
            m_currentWorkItem.Open();
            string migStatus = (string)m_currentWorkItem[m_migrationStatusFieldName];
            Debug.Assert(!String.Equals (migStatus, "Done", StringComparison.OrdinalIgnoreCase), "Bug is already migrated");
            if (String.IsNullOrEmpty(migStatus))
            {
                return 0;
            }
            int historyCount = 0;
            int.TryParse(migStatus, out historyCount);
            return historyCount;
        }

        public int GetCurrentWorkItemLinksCount()
        {
            m_currentWorkItem.Open();
            return m_currentWorkItem.Links.Count;
        }

        public int GetCurrentWorkItemAttachmentsCount()
        {
            m_currentWorkItem.Open();
            return m_currentWorkItem.Attachments.Count;
        }

        /// <summary>
        /// Create new work item in currituck
        /// </summary>
        /// <returns></returns>
        private WorkItem CreateNewWorkItem()
        {
            return (new WorkItem(m_vstsConnection.GetWorkItemType(m_witName)));
        }

        // private fields!
        private WorkItem m_currentWorkItem;
        private VSTSConnection m_vstsConnection;
        private FieldMaps m_maps;
        private Hashtable m_fieldMappings;
        private Hashtable m_inverseFieldMappings;
        private Hashtable m_valueMappings;
        private UserMappings m_userMappings;
        private string m_fieldMapFile;
        private string m_sourceWorkItemId;
        private int m_convSourceIndex;

        // static variables
        private string m_witName;
        private static Hashtable m_skippedFields = new Hashtable(TFStringComparer.OrdinalIgnoreCase);

        // private constant hardcoded field names
        private const int m_maxRetries = 5;
        private bool m_canSetDuplicateWiId;
        private const string m_migrationStatusFieldName = "Migration Status";
        private const string m_duplicateWiId = "Duplicate WI ID";
        private const string m_CurrituckUri = @"{0}WorkItemTracking/WorkItem.aspx?artifactMoniker={1}";

        private static readonly string[] m_requiredFieldsForCreatingWI = { 
                VSTSConstants.WorkItemTypeFieldRefName,
                VSTSConstants.StateFieldRefName,
                // VSTSConstants.CreatedDateFieldRefName,   // set separately
                // VSTSConstants.CreatedByFieldRefName,     // set separately
                VSTSConstants.AreaIdFieldRefName,
                VSTSConstants.IterationIdFieldRefName,
                VSTSConstants.ReasonFieldRefName
            };
    }
}
