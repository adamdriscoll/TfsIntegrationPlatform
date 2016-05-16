// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Helper Class to provide generic method implementations
// for VSTS

#region Using directives
using System;
using System.Xml;
using System.Globalization;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Converters.Utility;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using Microsoft.TeamFoundation.Converters.Reporting;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Client;
using System.Xml.Schema;
using System.Security.Principal;
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    /// <summary>
    /// VSTS side performance counters
    /// </summary>
    public enum VstsPerformanceCounters
    {
        VstsCreateInitialView,
        VstsAddRevision,
        VstsMigrateAttachments,
        VstsMigrateLinks,
        VstsWorkItemMigratedCheck,
        VstsWebServiceCallInsert,
        VstsWebServiceCallUpdate,
        VstsSetCurrentWorkItem,
        VstsProvisioning,
        VstsCssCreation,
        VstsMaxPerfCounters
    }

    /// <summary>
    /// Class for representing (name - value - relationship type)
    /// </summary>
    public class WorkItemNameValueRelation
    {
        private readonly string[] m_relationTypes = new string[] { "UNDER", "=", "!=" };
        public enum RelationShipType
        {
            UNDER = 0,
            EQUALS = 1,
            NOTEQUALS = 2
        }
        public enum QueryDataType
        {
            String,
            Int
        }

        private string m_name;
        private string m_value;
        private string m_relation;
        private QueryDataType m_dataType;

        public WorkItemNameValueRelation(string name, string value)
        {
            m_name = name;
            Value = value;
            m_relation = m_relationTypes[(int)RelationShipType.EQUALS];
            m_dataType = QueryDataType.String;
        }

        public WorkItemNameValueRelation(string name, string value, RelationShipType relationType)
        {
            m_name = name;
            Value = value;
            m_relation = m_relationTypes[(int)relationType];
            m_dataType = QueryDataType.String;
        }

        public string Name
        {
            get { return (m_name); }
        }
        public string Value
        {
            get { return (m_value); }
            set 
            {
                // incase the WIQL contains apostrophes, replace it with double apostrophes
                // fix for bug# 48871
                m_value = value;
                if (m_value.Contains("'"))
                {
                    m_value = m_value.Replace("'", "''");
                }
            }
        }
        public string Relation
        {
            get { return (m_relation); }
        }
        public QueryDataType DataType
        {
            get { return m_dataType; }
        }
    }

    static class VSTSUtil
    {
        public static void ImportToCurrituck(Microsoft.TeamFoundation.WorkItemTracking.Client.Project VSTSProj, string witdFile)
        {
            try
            {
                /* first check if the FORM section has Layout in this.. this is possible if the customer
                 * doesn't modifies the generated WITD in Analyze phase
                 */
                Display.StartProgressDisplay(UtilityMethods.Format(
                    VSTSResource.VstsSchemaImporting, witdFile));
                string witDefinition;
                using (StreamReader stream = new StreamReader(witdFile))
                {
                    witDefinition = stream.ReadToEnd();
                }
                
                WorkItemTypeCollection wits = VSTSProj.WorkItemTypes;
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Provisioning Work Item Type : {0}", witdFile);

                wits.Import(witDefinition);
            }
            // Any changes made in this catch block should be reproduced in the below catch block too;
            // This is to get rid of the nonclscompliant message from FxCop
            catch (XmlSchemaValidationException e)
            {
                Logger.WriteException(LogSource.WorkItemTracking, e);
                string errMsg = UtilityMethods.Format(
                    VSTSResource.VstsWITValidationFailed, witdFile,
                    e.LineNumber, e.LinePosition, e.Message);
                Common.ConverterMain.MigrationReport.WriteIssue(string.Empty, errMsg, string.Empty, null,
                    IssueGroup.Witd.ToString(), ReportIssueType.Critical);

                throw new ConverterException(errMsg, e);
            }
            catch (Exception e)
            {
                Logger.WriteException(LogSource.WorkItemTracking, e);
                string errMsg = UtilityMethods.Format(
                    VSTSResource.VstsWITProvisionFailed, witdFile, e.Message);
                Common.ConverterMain.MigrationReport.WriteIssue(string.Empty,
                      errMsg, string.Empty, null, IssueGroup.Witd.ToString(), ReportIssueType.Critical);

                throw new ConverterException(errMsg, e);
            }
            finally
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Work Item Type Provisioning Done : {0}", witdFile);
                Display.StopProgressDisplay();
            }
        } // end of ImportToCurrituck

        /// <summary>
        /// Internal function call used to get HTML string
        /// </summary>
        /// <param name="paragraph"></param>
        /// <returns></returns>
        internal static string ToParaHTML(string paragraph)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("p");
            element.InnerText = paragraph;
            return element.OuterXml;
        }

        /// <summary>
        /// Create a string in new line in the description of VSTS client
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConvertTextToHtml(string str)
        {
            if (!String.IsNullOrEmpty(str))
            {
                str = ToParaHTML(str);
                str = str.Replace("\r\n", "<BR>");
                return (str.Replace("\n", "<BR>"));
            }
            else
            {
                return (null);
            }
        }

        /// <summary>
        /// Execute a query given a list of name/value/relation sets as criteria
        /// </summary>
        /// <param name="vstsConn"></param>
        /// <param name="checkList"></param>
        /// <returns></returns>
        public static WorkItemCollection ExecuteVstsQuery(VSTSConnection vstsConn, ArrayList checkList)
        {
            StringBuilder curquery = new StringBuilder("Select ID from workitems where ");

            foreach (WorkItemNameValueRelation namevaluerelation in checkList)
            {
                if (checkList.IndexOf(namevaluerelation) != 0)
                {
                    curquery.Append(" AND ");
                }

                curquery.Append(" [");
                curquery.Append(namevaluerelation.Name); // name
                curquery.Append("] ");
                curquery.Append(namevaluerelation.Relation);   // relation

                // no need to put '' for int types
                if (namevaluerelation.DataType == WorkItemNameValueRelation.QueryDataType.String)
                {
                    curquery.Append(" '");
                }

                curquery.Append(namevaluerelation.Value);      // value
                if (namevaluerelation.DataType == WorkItemNameValueRelation.QueryDataType.String)
                {
                    curquery.Append("'");
                }
            }

            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Executing Query : {0}", curquery.ToString());

            return (vstsConn.store.Query(curquery.ToString()));
        }

        /// <summary>
        /// Find a work item that satisfies given checkList
        /// </summary>
        /// <param name="vstsConn"></param>
        /// <param name="checkList"></param>
        /// <returns></returns>
        public static WorkItem FindWorkItem(VSTSConnection vstsConn, ArrayList checkList)
        {
            WorkItemCollection workitems = ExecuteVstsQuery(vstsConn, checkList);
            Debug.Assert(workitems.Count <= 1, "Work Item " + " was migrated more than once");

            WorkItem localWI = null;
            if (workitems != null && workitems.Count > 0)
            {
                localWI = vstsConn.store.GetWorkItem(workitems[0].Id);
            }

            return localWI;
        }

        /// <summary>
        /// Return Currituck work item Identifier given reference to the 
        /// in memory instance of the same work item
        /// </summary>
        /// <param name="wi"></param>
        /// <returns></returns>
        public static int GetIdForWorkItem(Object wi)
        {
            WorkItem witem = (WorkItem)wi;
            if (witem != null)
                return (witem.Id);
            return (-1);            // invalid Id!
        }

        /// <summary>
        /// Get the work item given its id!
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static WorkItem GetWorkItemForId(VSTSConnection conn, int id)
        {
            return (conn.store.GetWorkItem(id));
        }

        /// <summary>
        /// Check the field name in already used field names accross different WIT's
        /// and make sure there are no field name and type collisions.
        /// </summary>
        /// <param name="witName">WIT Name</param>
        /// <param name="witdFldDef">Field Definition handle</param>
        /// <param name="usedFiledNameTypes">List of already used field names/types across WIT's</param>
        public static void CheckFieldTypeCollision(string witName, ref Common.FieldDefinition witFldDef,
                                                       Dictionary<string, Common.FieldDefinition> usedFiledNameDefs)
        {
            if (usedFiledNameDefs.ContainsKey(witFldDef.name))
            {
                // Field name is already used across WIT's, we should repeat it with exact casing.
                witFldDef.name = usedFiledNameDefs[witFldDef.name].name;

                string witFieldType = witFldDef.type.ToString();
                string usedType = usedFiledNameDefs[witFldDef.name].type.ToString();

                if (!TFStringComparer.FieldType.Equals(witFieldType, usedType))
                {
                    // The type is different from what already used in some WIT.
                    // Qualify field name by prefixing WIT name.
                    witFldDef.name = witName + "_" + witFldDef.name;

                    CheckFieldTypeCollision(witName, ref witFldDef, usedFiledNameDefs);
                }
            }
        }

        /// <summary>
        /// Check the field name in Store.FieldCollection and come out with a unique field name
        /// and the corresponding unique refname
        /// </summary>
        /// <param name="conn">VSTS connection handle</param>
        /// <param name="witdFldDef">Field Definition handle</param>
        /// <param name="usedFieldNames">list of already used field names</param>
        public static void ValidateFieldNames(VSTSConnection conn,
                                              ref Common.FieldDefinition witdFldDef,
                                              Hashtable usedFieldNames,
                                              Hashtable usedFieldRefNames)
        {
            Debug.Assert(conn != null);
            Debug.Assert(witdFldDef != null);

            WorkItemStore store = conn.store;
            FieldDefinitionCollection fldDefCollection = store.FieldDefinitions;

            // if this field is already used in current schema, append numbers to it monotonically increasing
            if (usedFieldNames[witdFldDef.name] != null)
            {
                int i = 1;
                while (usedFieldNames[witdFldDef.name + i.ToString(CultureInfo.InvariantCulture)] != null)
                {
                    i++;
                }

                // got a field which is not there in existing list
                witdFldDef.name = witdFldDef.name + i.ToString(CultureInfo.InvariantCulture);
            }

            if (fldDefCollection.Contains(witdFldDef.name))
            {
                FieldDefinition currituckFldDef = fldDefCollection[witdFldDef.name];
                string witdtype = witdFldDef.type.ToString();

                string vstsFldType = currituckFldDef.FieldType.ToString();

                if (String.Equals (witdtype, vstsFldType, StringComparison.OrdinalIgnoreCase))
                {
                    // reuse the same refname from the existing field in store
                    witdFldDef.refname = currituckFldDef.ReferenceName;
                }
                else
                {
                    // generate new field based on using field name and type..
                    witdFldDef.name = witdFldDef.name + " " + witdtype;

                    // recheck if this new field exists in the currituck
                    ValidateFieldNames(conn, ref witdFldDef, usedFieldNames, usedFieldRefNames);
                }
            }
            else
            {
                // this field does not exist in curritcuk.. 

                // check if this is one of the core internal field not supposed to be used by the customer
                /*
                 * fix for bug#11206
                 * there is additional constraint that some of the core fields names are internal
                 * and cannot be used by the user... for temporary solution listing out all those 
                 * core fields.. shall go out once there is some better solution from currituck
                 */
                if (!VSTSConstants.TfsInternalFields.ContainsKey(witdFldDef.name))
                {
                    // can use this safely with our own type and refname
                    witdFldDef.refname = ReferenceNamePrefix + witdFldDef.name;

                    // remove unwanted characters from refname
                    // if the refname is taken from Currituck, there would not be any unwanted characters to be replaced
                    witdFldDef.refname = FieldRefNameRegEx.Replace(witdFldDef.refname, "_");

                    // see if this refname is already used with some other field in tfs
                    // or in the current schema, append numbers to it monotonically increasing
                    if (fldDefCollection.Contains(witdFldDef.refname) ||
                        usedFieldRefNames.ContainsKey(witdFldDef.refname))
                    {
                        string newFldRefName = String.Empty;
                        // generate a unique ref name
                        int suffix = 1;
                        do
                        {
                            newFldRefName = String.Concat(witdFldDef.refname, suffix++);
                        }
                        while (fldDefCollection.Contains(newFldRefName) ||
                            usedFieldRefNames.ContainsKey(newFldRefName));
                        witdFldDef.refname = newFldRefName;
                    }
                }
                else
                {
                    /*
                     * If the field name happen to be one of the intenal core field name, treat as
                     * that is a existing field with different type and generate new field name
                     */
                    // generate new field based using field name and type..
                    witdFldDef.name = witdFldDef.name + " " + witdFldDef.type.ToString();

                    // recheck if this new field exists in the currituck
                    ValidateFieldNames(conn, ref witdFldDef, usedFieldNames, usedFieldRefNames);
                }
            }
        }

        /// <summary>
        /// Validate users in UserMap. .should be part of TFSEveryone group
        /// </summary>
        /// <param name="usersMap"></param>
        public static void ValidateCurrituckUsers(UserMappingsUserMap[] usersMap, VSTSConnection vstsConn, string userMapFile)
        {
            if (usersMap == null || usersMap.Length == 0)
            {
                // null user maps allowed.. bug#11100
                return;
            }
            Logger.WritePerf(LogSource.WorkItemTracking, "Validating users on Work Item Tracking");

            // there can be multiple source users mapped to one currituck users..
            // since currituck user validation is costly, will store the currituck user once validated
            Hashtable processedUsers = new Hashtable(TFStringComparer.UserName);

            Display.StartProgressDisplay(VSTSResource.VstsValidatingUsers);
            try
            {
                StringBuilder invalidUsers = new StringBuilder();

                foreach (UserMappingsUserMap uMap in usersMap)
                {
                    Debug.Assert(uMap.To != null, "Found null user in UserMap");

                    if (!ResolvedUsers.ContainsKey(uMap.To) &&
                        !processedUsers.ContainsKey(uMap.To))
                    {
                        processedUsers.Add(uMap.To, null);
                        Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Verifying ClientUserName in BIS: {0}", uMap.To);

                        // Get the user identity
                        Identity user = ResolveUser(vstsConn, uMap.To);

                        if (user == null)
                        {
                            // add in the invalid users list
                            if (invalidUsers.Length > 0)
                            {
                                invalidUsers.Append(", ");
                            }
                            invalidUsers.Append(uMap.To);
                        }
                        else
                        {
                            // work item tracking now works in two modes.. Account Name and Friendly Name
                            // if the current mode is "Friendly Name".. add the alias->friendly name in Resolved users
                            if (vstsConn.store.UserDisplayMode == UserDisplayMode.FriendlyName)
                            {
                                ResolvedUsers.Add(uMap.To, user.DisplayName);
                            }
                            else
                            {
                                // add alias->alias in Resolved Users
                                ResolvedUsers.Add(uMap.To, uMap.To);
                            }
                        }
                    }
                }
                if (invalidUsers.Length > 0)
                {
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, "Invalid Users in usermap file: {0}",
                        invalidUsers.ToString());
                    string errMsg = UtilityMethods.Format(
                        VSTSResource.VstsInvalidUsers, userMapFile, invalidUsers.ToString());
                    Common.ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty, null, string.Empty,
                        ReportIssueType.Critical);

                    throw new ConverterException(errMsg);
                }
            }
            finally
            {
                Display.StopProgressDisplay();
                Logger.WritePerf(LogSource.WorkItemTracking, "Validation of users on Work Item Tracking completed");
            }
        }

        internal static Identity ResolveUser(VSTSConnection vstsConn, string userName)
        {
            Debug.Assert(vstsConn != null, "Null vstsConn handle");
            Debug.Assert(userName != null, "Null userName param");
            if (m_gss == null)
            {
                TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(vstsConn.bisUri);
                m_gss = (IGroupSecurityService)tfs.GetService(typeof(IGroupSecurityService));
            }

            // Get the user identity
            Identity userIdentity = null;
            try
            {
                userIdentity = m_gss.ReadIdentityFromSource(SearchFactor.AccountName, userName);
            }
            catch (Exception ex)
            {
                // if there is some Active Directory issue, currently GSS throws GroupSecuritySubsystemException
                // bug#57749 plans to give specific exception for this..
                // we are handling Exception here as the bug 57749 lists this exception as well
                // as one of the possible exception that can be thrown
                // for this exception assume that the user cannot be resolved
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, "Could not resolve user {0} because of {1}",
                    userName, ex.Message);
            }
            return userIdentity;
        }

        /// <summary>
        /// Check if the the current user is part of Service Accounts
        /// </summary>
        /// <param name="bisUri">Application Tier URI</param>
        /// throws ConverterException if the user is not part of 'Service Accounts' security group
        internal static void IsCurrentUserInServiceAccount(string bisUri)
        {
            try
            {
                // initialize gss
                TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(bisUri);
                IGroupSecurityService gss = (IGroupSecurityService)tfs.GetService(typeof(IGroupSecurityService));

                // Get the Service Account group identity
                Identity serviceGroup = gss.ReadIdentity(SearchFactor.ServiceApplicationGroup, string.Empty, QueryMembership.None);
                Debug.Assert(serviceGroup != null, "serviceGroup != null");

                // check if this is Windows AD user or workgroup user
                int res = 0;
                IntPtr ptrDomain = IntPtr.Zero;
                bool isDomain = true;
                int status = 0;
                try
                {
                    res = NetGetJoinInformation(null, out ptrDomain, out status);
                    if (0 == res && 2 == status)    // workgroup name
                    {
                        isDomain = false;
                    }
                }
                finally
                {
                    if (IntPtr.Zero != ptrDomain)
                    {
                        NetApiBufferFree(ptrDomain);
                    }
                }

                string currentUser = String.Empty;
                if (!isDomain)
                {
                    // workgroup user..
                    currentUser = Environment.UserName;
                }
                else
                {
                    // windows AD user
                    currentUser = String.Concat(Environment.UserDomainName, Path.DirectorySeparatorChar, Environment.UserName);
                }

                Identity user = gss.ReadIdentity(SearchFactor.AccountName, currentUser, QueryMembership.None);
                if (user == null || gss.IsMember(serviceGroup.Sid, user.Sid) == false)
                {
                    // not part of service accounts group
                    string errMsg = UtilityMethods.Format(
                        VSTSResource.VstsUserNotInServiceAccounts, currentUser);
                    throw new ConverterException(errMsg);
                }
            }
            catch (Exception e)
            {
                if (e is ConverterException)
                {
                    throw;
                }

                throw new ConverterException(e.Message, e);
            }
        }

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetGetJoinInformation(
          [In, MarshalAs(UnmanagedType.LPWStr)] string server,
          out IntPtr domain,
          out int status);

        [DllImport("Netapi32.dll", SetLastError = true)]
        private static extern int NetApiBufferFree(IntPtr Buffer);
        
        /// <summary>
        /// This method validates that the given work item type
        /// contains WITD/WORKITEMTYPE node and "name" attribute
        /// matches the entityNamematches
        /// </summary>
        /// <param name="entityName">Entity Name</param>
        /// <param name="witdFileName">WITD file name</param>
        internal static void ValidateWorkItemType(
            string entityName, 
            string witdFileName,
            string schemaMapFile)
        {
            // verify if the xml file exist and read permission is there
            UtilityMethods.ValidateFile(witdFileName, schemaMapFile);

            // open the xml file and look for WorkItemType node
            XmlDocument witdDoc;

            try
            {
                witdDoc = UtilityMethods.LoadFileAsXmlDocument(witdFileName);
            }
            catch (XmlException xmlEx)
            {
                // some error in loading file.. malformed xml
                // no WORKITEMTYPE element found.. invalid xml
                string errMsg = UtilityMethods.Format(
                    VSTSResource.VstsInvalidXmlFile, witdFileName, schemaMapFile,
                    xmlEx.Message, xmlEx.LineNumber, xmlEx.LinePosition);
                PostMigrationReport.WriteIssue(null, null,
                    ReportStatisticsStatisicsDetails.MigrationStatus.Failed,
                    ReportIssueType.Critical, String.Empty,
                    entityName, IssueGroup.Config, errMsg);
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, errMsg);
                throw new ConverterException(errMsg);
            }
            
            XmlNamespaceManager nsm = new XmlNamespaceManager(witdDoc.NameTable);
            nsm.AddNamespace("CURR", Common.CommonConstants.WITDTypesNamespace);

            string tagWitdWithNS = "//CURR:WITD";
            string tagWitdWithoutNS = "/WITD";
            string tagWitWithNS = "//CURR:WORKITEMTYPE";
            string tagWitWithoutNS = "WORKITEMTYPE";
            // try with the namespace prefix
            XmlNode witdNode = witdDoc.SelectSingleNode(tagWitdWithNS, nsm);
            if (witdNode == null)
            {
                // no node found with this namespace definition.. try without namespace
                witdNode = witdDoc.SelectSingleNode(tagWitdWithoutNS, nsm);
            }

            if (witdNode == null)
            {
                string errMsg = UtilityMethods.Format(
                    VSTSResource.VstsNoWitd, witdFileName, schemaMapFile);
                PostMigrationReport.WriteIssue(null, null,
                    ReportStatisticsStatisicsDetails.MigrationStatus.Failed,
                    ReportIssueType.Critical, String.Empty,
                    entityName, IssueGroup.Config, errMsg);
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, errMsg);
                throw new ConverterException(errMsg);
            }

            XmlNode witNode = witdNode.SelectSingleNode(tagWitWithNS, nsm);
            if (witNode == null)
            {
                witNode = witdNode.SelectSingleNode(tagWitWithoutNS, nsm);
            }
            if (witNode == null)
            {
                // no WORKITEMTYPE element found.. invalid xml
                string errMsg = UtilityMethods.Format(
                    VSTSResource.VstsNoWorkItemType, witdFileName, schemaMapFile);
                PostMigrationReport.WriteIssue(null, null,
                    ReportStatisticsStatisicsDetails.MigrationStatus.Failed,
                    ReportIssueType.Critical, String.Empty,
                    entityName, IssueGroup.Config, errMsg);
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, errMsg);
                throw new ConverterException(errMsg);
            }
            else
            {
                // look for name attribute
                XmlAttribute nameAttrib = witNode.Attributes["name"];
                if (nameAttrib == null)
                {
                    // no "name" attribute set
                    string errMsg = UtilityMethods.Format(
                        VSTSResource.VstsNoNameAttribute, witdFileName, schemaMapFile);
                    PostMigrationReport.WriteIssue(null, null,
                        ReportStatisticsStatisicsDetails.MigrationStatus.Failed,
                        ReportIssueType.Critical, String.Empty,
                        entityName, IssueGroup.Config, errMsg);
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, errMsg);
                    throw new ConverterException(errMsg);
                }
                if (!TFStringComparer.XmlAttributeValue.Equals(entityName, nameAttrib.Value))
                {
                    // mismatch in target entity
                    string errMsg = UtilityMethods.Format(
                        VSTSResource.VstsWitMismatch, witdFileName, entityName, schemaMapFile);
                    PostMigrationReport.WriteIssue(null, null, 
                        ReportStatisticsStatisicsDetails.MigrationStatus.Failed,
                        ReportIssueType.Critical, String.Empty,
                        entityName, IssueGroup.Config, errMsg);
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, errMsg);
                    throw new ConverterException(errMsg);
                }
            }
        }

        /// <summary>
        /// static constructor
        /// </summary>
        internal static List<string>[] InitialFields
        {
            get
            {
                if (m_initialFields == null)
                {
                    m_initialFields = new List<string>[(int)ConverterSource.MAX];

                    for (int count = 0; count < (int)ConverterSource.MAX; count++)
                    {
                        m_initialFields[count] = new List<string>();
                    }

                    // PS fields
                    m_initialFields[(int)ConverterSource.PS].Add("ID");
                    m_initialFields[(int)ConverterSource.PS].Add("PS Bug DB");
                    m_initialFields[(int)ConverterSource.PS].Add("PS Feedback ID");

                    // CQ fields
                    m_initialFields[(int)ConverterSource.CQ].Add(Common.CommonConstants.VSTSSrcIdField);
                    m_initialFields[(int)ConverterSource.CQ].Add(Common.CommonConstants.VSTSSrcDbField);
                }
                return m_initialFields;
            }
        }

        /// <summary>
        /// Static method to check if a work item is migrated given a currituck work item
        /// </summary>
        /// <param name="item">currituck work item</param>
        /// <returns></returns>
        public static bool IsWorkItemMigrated(Object item)
        {
            WorkItem wi = (WorkItem)item;
            if (wi != null)
            {
                object statusObj = wi["Migration Status"];
                if (statusObj != null)
                {
                    Debug.Assert(statusObj is string);
                    if (String.Equals(statusObj as string, "Done", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Handle catching DeniedOrNotExistException in case
        /// path looked up for is not found
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Node FindNodeInSubTree(VSTSConnection vstsConn, string path, Node.TreeType type)
        {
            Node localNode = null;
            try
            {
                localNode = vstsConn.project.FindNodeInSubTree(path, type);
            }
            catch (DeniedOrNotExistException)
            {
                // try to create the given css path
                if (path.IndexOfAny(VSTSConstants.UnsupportedCSSChars) > -1)
                {
                    String newPath = AreaPathRegEx.Replace(path, "_");
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, "Generating new CSS path from [{0}] to [{1}]",
                        path, newPath);
                    localNode = FindNodeInSubTree(vstsConn, newPath, type);
                }
                else
                {
                    try
                    {
                        // try to create the CSS path
                        if (Css == null)
                        {
                            Css = (ICommonStructureService)vstsConn.Tfs.GetService(typeof(ICommonStructureService));
                        }
                        SetDefaultCSSUri(Css, vstsConn.projectName);
                        if (type == Node.TreeType.Area)
                        {
                            CreateCSSPath(Css, path, RootAreaNodeUri);
                        }
                        else if (type == Node.TreeType.Iteration)
                        {
                            CreateCSSPath(Css, path, RootIterationNodeUri);
                        }
                        // after creating CSS path, sync the CSS tree
                        if (VstsProjectInfo == null)
                        {
                            VstsProjectInfo = Css.GetProjectFromName(vstsConn.projectName);
                        }
                        WSHelper.SyncBisCSS(VstsProjectInfo);
                        // refresh the connection
                        vstsConn.Refresh();

                        // try to get the id now
                        try
                        {
                            localNode = vstsConn.project.FindNodeInSubTree(path, type);
                        }
                        catch (DeniedOrNotExistException)
                        {
                            localNode = null;
                            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning,
                                "Cannot find the CSS path {0} even after creating", path);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            return localNode;
        }

        /// <summary>
        /// Set the root node URI for Area Path and Iteration Path
        /// </summary>
        /// <param name="css">Handle to Common Structure Services</param>
        /// <param name="projectName">Team Foundation Project Name</param>
        private static void SetDefaultCSSUri(ICommonStructureService css, string projectName)
        {
            if (RootAreaNodeUri == null &&
                RootIterationNodeUri == null)
            {
                ProjectInfo m_project = css.GetProjectFromName(projectName);
                NodeInfo[] nodes = css.ListStructures(m_project.Uri);
                bool fFoundArea = false;
                bool fFoundIteration = false; 
                for (int i = 0; (fFoundArea == false || fFoundIteration == false) && i < nodes.Length; i++)
                {
                    if (!fFoundArea &&
                        string.Equals(nodes[i].StructureType, VSTSConstants.AreaRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        RootAreaNodeUri = nodes[i].Uri;
                        fFoundArea = true;
                    }

                    if (!fFoundIteration &&
                        string.Equals(nodes[i].StructureType, VSTSConstants.IterationRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        RootIterationNodeUri = nodes[i].Uri;
                        fFoundIteration = true;
                    }
                }
            }
        }

        /// <summary>
        /// Create the CSS path (Area Path or Iteration Path)
        /// </summary>
        /// <param name="css">Handle to ICommonStructureService</param>
        /// <param name="cssPath">Path to be created</param>
        /// <param name="defaultUri">URI for the root node</param>
        private static void CreateCSSPath(ICommonStructureService css, string cssPath, string defaultUri)
        {
            string[] cssPathFragments = cssPath.Split('\\');
            int pathLength = 0;
            string tempPath = String.Empty;
            NodeInfo rootNode = css.GetNode(defaultUri);
            NodeInfo parentNode = rootNode; // parent is root for now
            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Creating CSS path [{0}]", cssPath);

            // for each fraction of path, see if it exists
            for (pathLength = 0; pathLength < cssPathFragments.Length; pathLength++)
            {
                tempPath = String.Concat(parentNode.Path, "\\", cssPathFragments[pathLength]);
                NodeInfo childNode = null;
                try
                {
                    if (NodeInfoCache.ContainsKey(tempPath))
                    {
                        childNode = NodeInfoCache[tempPath];
                    }
                    else
                    {
                        childNode = css.GetNodeFromPath(tempPath);
                        NodeInfoCache.Add(tempPath, childNode);
                    }
                }
                catch (SoapException)
                {
                    // node does not exist.. ignore the exception
                }
                catch (ArgumentException)
                {
                    // node does not exist.. ignore the exception
                }

                if (childNode == null)
                {
                    // given node does not exist.. create it
                    for (int restCSSPath = pathLength; restCSSPath < cssPathFragments.Length; restCSSPath++)
                    {
                        string nodeUri = css.CreateNode(cssPathFragments[restCSSPath], parentNode.Uri);
                        // once a node is created, all the subsequent nodes can be created without any lookup
                        // set the parent node
                        parentNode = css.GetNode(nodeUri);
                    }
                    break;  // out of for loop
                }
                else
                {
                    // set the parent node to current node
                    parentNode = childNode;
                }
            }
        }

        #region Private Variables
        private static int[] m_perfCounter = new int[(int)VstsPerformanceCounters.VstsMaxPerfCounters];
        private const string ReferenceNamePrefix = "Microsoft.TeamFoundation.Converters.";
        public static readonly string ConverterComment = "[" + Assembly.GetExecutingAssembly().GetName().Name + "] ";
        private static readonly Regex FieldRefNameRegEx = new Regex(@"[^a-zA-Z0-9_.]", RegexOptions.Compiled);
        private static readonly Regex AreaPathRegEx = new Regex("[/$?*:\"&<>#%|\t]", RegexOptions.Compiled);

        // these fields will be set by CreateInitialView API for specific converters
        internal static List<string>[] m_initialFields;

        private static string RootAreaNodeUri;
        private static string RootIterationNodeUri;
        private static ProjectInfo VstsProjectInfo;
        private static ICommonStructureService Css;
        private static Dictionary<string, NodeInfo> m_nodeInfoCache;
        private static Dictionary<string, NodeInfo> NodeInfoCache
        {
            get
            {
                if (m_nodeInfoCache == null)
                {
                    m_nodeInfoCache = new Dictionary<string, NodeInfo>(TFStringComparer.CssTreePathName);
                }

                return m_nodeInfoCache;
            }
        }

        private static Hashtable m_ResolvedUsers;
        internal static Hashtable ResolvedUsers
        {
            get
            {
                if (m_ResolvedUsers == null)
                {
                    m_ResolvedUsers = new Hashtable(TFStringComparer.UserName);
                }

                return m_ResolvedUsers;
            }
        }

        private static IGroupSecurityService m_gss;

        #endregion

    }
} //end of namespace
