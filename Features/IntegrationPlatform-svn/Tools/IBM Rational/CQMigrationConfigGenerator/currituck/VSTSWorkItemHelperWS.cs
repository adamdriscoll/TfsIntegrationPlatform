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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using Microsoft.TeamFoundation.Converters.Reporting;
using Microsoft.TeamFoundation.Converters.WorkItemTracking;
using CurClient = Microsoft.TeamFoundation.WorkItemTracking.Client;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    /// <remarks>
    /// VSTS Work Item Helper Class
    /// </remarks>
    public partial class VSTSWorkItemHelper
    {
        private static Hashtable m_areaNodeUriCache = new Hashtable();

        // following fields are to be maintained for the current work item
        // in order to remove the dependenvy to maintain the OM work item handle
        private VSTSWorkItem m_vstsWorkItem;
        private Hashtable m_baseWiSnapShot = new Hashtable();
        private WorkItem m_wi = null;
        
        // current work item revision
        private int m_revision = 0;
        private Field m_MigStatusField;

        /// <summary>
        /// Create work item in the Currituck corresponding to the given memory work item
        /// </summary>
        /// <param name="sourceWIId">Source Work Item ID</param>
        /// <param name="imWorkItem">In Memory Work Item containing Initial View, Attachments and Links</param>
        /// <param name="setMigStatus">Set the Migration Status field to Done or not</param>
        /// <returns>true if it is able to save all fields, attachments and links, else false</returns>
        public bool CreateInitialViewOfWorkItem(string sourceWIId, InMemoryWorkItem imWorkItem, bool setMigStatusDone)
        {
            bool retVal = true;
 
            m_vstsWorkItem = new VSTSWorkItem();
            try
            {
                m_sourceWorkItemId = sourceWIId;
                m_vstsWorkItem.sourceId = sourceWIId;

                // create new webs service call xml fragment
                WSHelper webServiceHelper = new WSHelper(WSHelper.WebServiceType.InsertWorkItem);

                // push the initial snapshot
                IDictionaryEnumerator enumerator = m_baseWiSnapShot.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    webServiceHelper.AddColumn(enumerator.Key.ToString(), enumerator.Value.ToString());
                }
                
                // set the default value of area id to root node initially
                m_vstsWorkItem.areaId = m_wi.AreaId;

                // first set the initial required fields.. if some save happens in 
                // state processing, bug will be created with minimal information
                foreach (string fldName in VSTSUtil.InitialFields[m_convSourceIndex])
                {
                    if (imWorkItem.InitialView[fldName] != null)
                    {
                        UpdateWorkItemField(webServiceHelper, fldName, imWorkItem.InitialView[fldName]);
                    }
                }

                // while creating initial view the fields with no values does not
                // makes any sense... they are required for further revisions where
                // the values would have been removed.. 
                // filter the initial view to remove all the null values
                ArrayList nullFields = new ArrayList();
                foreach (DictionaryEntry de in imWorkItem.InitialView)
                {
                    if (de.Value == null)
                    {
                        nullFields.Add(de.Key);
                    }
                    else
                    {
                        // see if it is empty string
                        if (de.Value is string)
                        {
                            if (String.IsNullOrEmpty((string)de.Value))
                            {
                                nullFields.Add(de.Key);
                            }
                        }
                    }
                }
                foreach (object toRemove in nullFields)
                {
                    imWorkItem.InitialView.Remove(toRemove);
                }


                ProcessRevision(webServiceHelper, imWorkItem.InitialView, "0");

                // Set migration status field only if no links and attachments exist
                if (setMigStatusDone &&
                    imWorkItem.Attachments.Count == 0 &&
                    imWorkItem.Links.Count == 0)
                {
                    // completed the migration for initial view
                    webServiceHelper.AddColumn(VSTSConstants.MigrationStatusField, "Done");
                }

                int workItemId = webServiceHelper.Save();
                Debug.Assert(workItemId != 0, "Work Item save returned ID as 0");

                // set the m_currentworkitem context
                m_currentWorkItem = SetCurrentWorkItem(workItemId);
                m_revision = m_currentWorkItem.Rev - 1;
                m_vstsWorkItem.Id = workItemId;
            }
            finally
            {
            }
            // process attachments and links only if SetMigStatus is true.. 
            // i.e. this is the only revision to be created
            if (setMigStatusDone &&
                (imWorkItem.Attachments.Count > 0 ||
                imWorkItem.Links.Count > 0))
            {
                retVal = ProcessAttachmentsAndLinks(imWorkItem, setMigStatusDone);
            }

            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Created new work item : {0}", m_currentWorkItem.Id);

            return retVal;
        }

        /// <summary>
        /// Add/Update the web service package with the given field and value
        /// </summary>
        /// <param name="webServiceHelper">Web Service package</param>
        /// <param name="fName">Field Name</param>
        /// <param name="fValue">Field Value</param>
        internal void UpdateWorkItemField(WSHelper webServiceHelper, string fName, object fValue)
        {
            if (fValue == null || fValue.ToString().Trim().Length == 0)
            {
                fValue = String.Empty;
            }

            // apply field map and value map
            string toFldName = GetMappedFieldName(fName, ref fValue);
            if (string.IsNullOrEmpty(toFldName))
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info, "No field map for work item [{0}], field [{1}].. Dropping", m_sourceWorkItemId, fName);
                return;
            }

            Field fld = m_wi.Fields[toFldName];
            if (fld != null)
            {
                if (VSTSConstants.SkipFields.ContainsKey(fld.ReferenceName))
                {
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info,
                        "Field {0} is being skipped as it cannot be modified using web service", fName);
                    return;
                }
                
                switch (fld.FieldDefinition.FieldType)
                {
                    // html type fields
                    case CurClient.FieldType.Html:
                    case CurClient.FieldType.History:
                        // apply html formatting on this
                        // and add to InsertText
                        webServiceHelper.AddDescriptiveField(fld.ReferenceName, VSTSUtil.ConvertTextToHtml(fValue.ToString()), true);
                        break;

                    case CurClient.FieldType.PlainText:
                        // add to InsertText
                        webServiceHelper.AddDescriptiveField(fld.ReferenceName, fValue.ToString(), false);
                        break;

                    case CurClient.FieldType.DateTime:
                        DateTime value;
                        string dateValue = String.Empty;
                        if (fValue is DateTime)
                        {
                            value = (DateTime)fValue;
                            dateValue = CommonConstants.ConvertDateToString(value);
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(fValue.ToString()))
                            {
                                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning,
                                    "Value of field {0} is not of type DateTime", fld.Name, fValue.ToString());
                                if (DateTime.TryParse(fValue.ToString(), out value) == true)
                                {
                                    dateValue = CommonConstants.ConvertDateToString(value);
                                }
                            }
                        }
                        webServiceHelper.AddColumn(fld.ReferenceName, dateValue);
                        break;

                    case CurClient.FieldType.String:
                        string fStrVal = fValue.ToString();
                        string fTruncatedValue = fStrVal;
                        if (fStrVal.Length > VSTSConstants.MaxStringFieldLength)
                        {
                            // put the original value in history field
                            webServiceHelper.AddDescriptiveField(VSTSConstants.HistoryFieldRefName, 
                                String.Concat(toFldName, ": ", fStrVal), true);

                            // Fix for 58661 CQConverter: The truncation of string fields should 
                            // be reported in the migration report.
                            string warnMsg = string.Format(VSTSResource.WarningFieldTruncated, 
                                                            toFldName, fStrVal, fTruncatedValue);
                            ConverterMain.MigrationReport.WriteIssue(String.Empty, warnMsg,
                                m_sourceWorkItemId.ToString(CultureInfo.InvariantCulture), string.Empty, 
                                                            IssueGroup.Wi.ToString(), ReportIssueType.Warning, 
                                                            null);

                            // truncate the field value
                            fTruncatedValue = fStrVal.Substring(0, VSTSConstants.MaxStringFieldLength);
                            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, 
                                "Truncating field '{0}' value , From [{1}] to [{2}]",
                                toFldName, fStrVal, fTruncatedValue);
                        }
                        webServiceHelper.AddColumn(fld.ReferenceName, fTruncatedValue);
                        break;
                    
                    default:
                        webServiceHelper.AddColumn(fld.ReferenceName, fValue.ToString());
                        break;
                }
            }
        }

        /// <summary>
        /// Set the work item context with OM wi handle
        /// </summary>
        /// <param name="id">Work Item ID</param>
        /// <returns></returns>
        internal WorkItem SetCurrentWorkItem(int id)
        {
            try
            {
                m_currentWorkItem = m_vstsConnection.store.GetWorkItem(id);
            }
            finally
            {
            }

            return m_currentWorkItem;
        }

        /// <summary>
        /// Process all the history revisions
        /// </summary>
        /// <param name="sourceWIId"></param>
        /// <param name="imWorkItem"></param>
        /// <param name="setMigStatusDone"></param>
        /// <returns></returns>
        public bool WriteHistoryItems(string sourceWIId, InMemoryWorkItem imWorkItem, bool setMigStatusDone)
        {
            m_revision = m_currentWorkItem.Rev - 1;
            m_vstsWorkItem.Id = m_currentWorkItem.Id;
            m_vstsWorkItem.areaId = m_currentWorkItem.AreaId;

            m_sourceWorkItemId = sourceWIId;
            string currentMigStatus = (string)m_currentWorkItem[m_migrationStatusFieldName];
            int revCount = 0;
            bool isMigStatusInt = int.TryParse(currentMigStatus, out revCount);
            Debug.Assert(isMigStatusInt); // should be able to parse always.. otherwise a code bug

            // process all history revisions and save in each call
            for (int historyIdx = 0; historyIdx < imWorkItem.HistoryItems.Count; historyIdx++)
            {
                // create new webs service call xml fragment
                WSHelper webServiceHelper = new WSHelper(WSHelper.WebServiceType.UpdateWorkItem);

                // set the required attributes
                webServiceHelper.SetWorkItemAndRevision(m_currentWorkItem.Id, ++m_revision);

                InMemoryHistoryItem imHistoryItem = (InMemoryHistoryItem)imWorkItem.HistoryItems[historyIdx];

                if (historyIdx == imWorkItem.HistoryItems.Count - 1 && // last history item
                    imWorkItem.Attachments.Count == 0 &&    // no attachments
                    imWorkItem.Links.Count == 0 &&          // no links
                    setMigStatusDone)                       // caller asked for it
                {
                    // last item.. set mig status to done
                    ProcessRevision(webServiceHelper, imHistoryItem.UpdatedView, "Done");
                }
                else
                {
                    ProcessRevision(webServiceHelper, imHistoryItem.UpdatedView, (++revCount).ToString());
                }

                try
                {
                    webServiceHelper.Save();
                }
                finally
                {
                }
            }

            if (imWorkItem.Attachments.Count > 0 ||
                imWorkItem.Links.Count > 0)
            {
                return ProcessAttachmentsAndLinks(imWorkItem, setMigStatusDone);
            }

            return true;
        }

        /// <summary>
        /// Process a specific revision of work item
        /// </summary>
        /// <param name="webServiceHelper">Handle to web service helper object</param>
        /// <param name="wiView">Current revision of work item to be updated</param>
        /// <param name="rev">Revision Id</param>
        private void ProcessRevision(WSHelper webServiceHelper,
                                     Hashtable wiView,
                                     string rev)
        {
            // now apply the source bug information and save
            IDictionaryEnumerator enumerator = wiView.GetEnumerator();
            while (enumerator.MoveNext())
            {
                UpdateWorkItemField(webServiceHelper, enumerator.Key.ToString(), enumerator.Value);
            }

            // set the revision
            webServiceHelper.AddColumn(VSTSConstants.MigrationStatusField, rev);
        }

        private string GetAreaNodeUri(int areaId)
        {
            string areaNodeUri = String.Empty;
            if (m_areaNodeUriCache[areaId] == null)
            {
                // cache miss.. generate areanodeuri
                if (areaId == m_vstsConnection.project.Id)
                {
                    areaNodeUri = WSHelper.GetAreaRootNodeUri(m_vstsConnection.project.Uri.ToString());
                }
                else
                {
                    foreach (Node node in m_vstsConnection.project.AreaRootNodes)
                    {
                        if (node.Id == areaId)
                        {
                            areaNodeUri = node.Uri.ToString();
                            break;
                        }
                        try
                        {
                            Node node2 = node.FindNodeInSubTree(areaId);
                            areaNodeUri = node2.Uri.ToString();
                            break;
                        }
                        catch (DeniedOrNotExistException)
                        {
                            // ignore if not found and continue to next area root node
                            continue;
                        }
                    }
                }
                // update the cache
                m_areaNodeUriCache[areaId] = areaNodeUri;
            }
            else
            {
                // already in cache. just return it
                areaNodeUri = (string)m_areaNodeUriCache[areaId];
            }
            return areaNodeUri;
        }

        /// <summary>
        /// Process Attachments and Links creation. Has to be at the end
        /// </summary>
        /// <param name="imWorkItem"></param>
        /// <param name="setMigStatus"></param>
        private bool ProcessAttachmentsAndLinks(InMemoryWorkItem imWorkItem, bool setMigStatus)
        {
            bool retVal = true;
            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, 
                    "[{0}] attachments for work item [{1}]", imWorkItem.Attachments.Count, m_sourceWorkItemId);
            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, 
                    "[{0}] links for work item [{1}]", imWorkItem.Links.Count, m_sourceWorkItemId);
            try
            {
                // create new web service call xml fragment
                WSHelper webServiceHelper = new WSHelper(WSHelper.WebServiceType.UpdateWorkItem);

                webServiceHelper.SetWorkItemAndRevision(m_currentWorkItem.Id, ++m_revision);

                // process links
                if (imWorkItem.Links.Count > 0)
                {
                    foreach (InMemoryLinkItem imLink in imWorkItem.Links)
                    {
                        if (!IsLinkMigrated(imLink))
                        {
                            if (imLink.CurrituckLinkedId == -1)
                            {
                                // link cannot be set as well as description cannot be found in history
                                webServiceHelper.AddDescriptiveField(VSTSConstants.HistoryFieldRefName,
                                    String.Concat(VSTSUtil.ConverterComment, imLink.LinkDescription), true);
                            }
                            else
                            {
                                // set the link information in work item.. as a related link
                                webServiceHelper.AddLink(imLink.CurrituckLinkedId, imLink.LinkDescription);

                                // check if it is duplicate link and setting Duplicate WI is allowed
                                if (m_canSetDuplicateWiId && 
                                    imLink is InMemoryDuplicateLinkItem &&
                                    m_vstsConnection.store.FieldDefinitions.Contains(m_duplicateWiId))
                                {
                                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info, "Creating Duplicate Link as Related Link from {0} to {1} with comment {2}",
                                            m_currentWorkItem.Id, imLink.CurrituckLinkedId, imLink.LinkDescription);
                                    webServiceHelper.AddColumn(m_wi.Fields[m_duplicateWiId].ReferenceName, imLink.CurrituckLinkedId.ToString(CultureInfo.InvariantCulture));
                                }
                            } // end of else

                        } // end of isLinkMigrated()
                        else
                        {
                            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning,
                                "Cannot add link as it already exists: {0}", imLink.CurrituckLinkedId);
                        }
                    }
                }

                // process attachments
                if (imWorkItem.Attachments.Count > 0)
                {
                    int noOfAttachmentsProcessed = 0;
                    string areaNodeUri = GetAreaNodeUri(m_vstsWorkItem.areaId);
                    Debug.Assert(!String.IsNullOrEmpty(areaNodeUri), "No area node uri found");
                    foreach (InMemoryAttachment attach in imWorkItem.Attachments)
                    {
                        if (IsAttachmentMigrated(attach))
                        {
                            continue;
                        }
                        try
                        {
                            webServiceHelper.AddAttachment(attach.FileName, attach.Comment, attach.IsLinkedFile, areaNodeUri);
                        }
                        catch (ConverterException conEx)
                        {
                            // attachment upload failed.. add into migration report
                            string errMsg = UtilityMethods.Format(
                                VSTSResource.VstsAttachmentUploadFailed,
                                Path.GetFileName(attach.FileName),
                                m_sourceWorkItemId, conEx.Message);

                            ConverterMain.MigrationReport.WriteIssue(String.Empty, ReportIssueType.Error, 
                                errMsg, m_sourceWorkItemId.ToString(CultureInfo.InvariantCulture));
                            Display.DisplayError(errMsg);
                            // and make sure that Migration Status is not set
                            setMigStatus = false;
                            retVal = false;
                        }
                        noOfAttachmentsProcessed++;

                        if (noOfAttachmentsProcessed % 32 == 0)
                        {
                            // save at every 32nd attachment
                            webServiceHelper.AddDescriptiveField(
                                VSTSConstants.HistoryFieldRefName,
                                UtilityMethods.Format(
                                    VSTSResource.VstsAttachmentLinkHistory), true);
                            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, "Performing interim save for work item {0} since no of attachments exceeds 32", m_sourceWorkItemId);
                            if (imWorkItem.Attachments.Count == noOfAttachmentsProcessed)
                            {
                                // boundary case .. this is the last attachment..
                                // set the Migration Status also
                                if (setMigStatus)
                                {
                                    webServiceHelper.AddColumn(VSTSConstants.MigrationStatusField, "Done");
                                }
                            }
                            webServiceHelper.Save();
                            if (noOfAttachmentsProcessed < imWorkItem.Attachments.Count)
                            {
                                // some attachemnts left.. reset the webserviceHelper handle
                                webServiceHelper = new WSHelper(WSHelper.WebServiceType.UpdateWorkItem);
                                webServiceHelper.SetWorkItemAndRevision(m_currentWorkItem.Id, ++m_revision);
                            }
                            else
                            {
                                // no more save required for the current work item
                                webServiceHelper = null;
                            }
                        } // end of if (noOfAttachmentsProcessed % 32 == 0)
                    } // end of foreach attachments
                }

                if (webServiceHelper != null)
                {
                    webServiceHelper.AddDescriptiveField(
                        VSTSConstants.HistoryFieldRefName,
                        UtilityMethods.Format(
                            VSTSResource.VstsAttachmentLinkHistory), true);

                    // Set migration status field
                    if (setMigStatus)
                    {
                        webServiceHelper.AddColumn(VSTSConstants.MigrationStatusField, "Done");
                    }

                    webServiceHelper.Save();
                }
                SetCurrentWorkItem(m_currentWorkItem.Id);
            }
            finally
            {
            }
            return retVal;
        }

        /// <summary>
        /// Check if the given attachment/hyperlink is already migrated
        /// </summary>
        /// <param name="imAttach">InMemoryAttachment handle</param>
        /// <returns>true if migrated false otherwise</returns>
        private bool IsAttachmentMigrated(InMemoryAttachment imAttach)
        {
            if (imAttach.IsLinkedFile) // hyperlinks
            {
                // first check if its not there
                foreach (Link currLink in m_currentWorkItem.Links)
                {
                    if (currLink.BaseType == BaseLinkType.Hyperlink)
                    {
                        Hyperlink currHyperLink = currLink as Hyperlink;
                        Debug.Assert(currHyperLink != null);

                        // Safely look for the first occurence for this link as currituck does not allow
                        // multiple hyperlink with same value
                        if (TFStringComparer.FilePath.Equals(currHyperLink.Location, imAttach.FileName))
                        {
                            // hyper link already migrated.. skip this link
                            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info, "Hyperlink {0} already migrated", imAttach.FileName);
                            return true;
                        }
                    }
                }
            }
            else // file attachments
            {
                FileInfo attachFileInfo = new FileInfo(imAttach.FileName);
                Debug.Assert(attachFileInfo.Exists);

                foreach (Attachment currAttach in m_currentWorkItem.Attachments)
                {
                    // Safely look for the first occurence for this attached file as currituck does not allow
                    // multiple attachment with same file name
                    if (TFStringComparer.AttachmentName.Equals (currAttach.Name, attachFileInfo.Name) &&
                         String.Equals (currAttach.Comment, imAttach.Comment, StringComparison.OrdinalIgnoreCase))
                    {
                        // attachment already migrated.. skip this attachment
                        Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info, "Attachment {0} already migrated", imAttach.FileName);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the given Link is already migrated
        /// </summary>
        /// <param name="imLink">InMemoryLinkItem handle</param>
        /// <returns>true if migrated false otherwise</returns>
        private bool IsLinkMigrated(InMemoryLinkItem imLink)
        {
            if (imLink.CurrituckLinkedId == -1)
            {
                // check if the related description is already there in some History
                int revCount = m_currentWorkItem.Revisions.Count;
                Revision wiRev;
                for (int revIndex = revCount - 1; revIndex >= 0; revIndex--)
                {
                    wiRev = m_currentWorkItem.Revisions[revIndex];
                    string revisionHistory = (string)wiRev[VSTSConstants.HistoryFieldRefName];
                    if (revisionHistory != null && revisionHistory.Contains(imLink.LinkDescription))
                    {
                        Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info, 
                            "Link with Description {0} already migrated", imLink.LinkDescription);
                        return true;
                    }
                }
            }
            else
            {
                foreach (Link currentLink in m_currentWorkItem.Links)
                {
                    RelatedLink currentRelatedLink = currentLink as RelatedLink;
                    if (currentRelatedLink == null)
                    {
                        // incase receive any other type of link, ignore it 
                        // as in V1 currituck only supports related links
                        continue;
                    }
                    if (currentRelatedLink.RelatedWorkItemId == imLink.CurrituckLinkedId)
                    {
                        Logger.Write(LogSource.WorkItemTracking, TraceLevel.Info, 
                            "Link with Work Item Id {0} and Description {1} already migrated",
                            imLink.CurrituckLinkedId, imLink.LinkDescription);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get the current work item's ID
        /// </summary>
        /// <returns></returns>
        public int WorkItemId
        {
            get { return m_currentWorkItem.Id; }
        }

        public int WorkItemRevision
        {
            get { return m_revision; }
            set { m_revision = value; }
        }
    }

    internal struct VSTSWorkItem
    {
        internal int Id;             // currituck id
        internal string sourceId;    // source work item id
        internal int areaId;         // area id
    }
}
