// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Does migration of one bug including history, links, attachments
// and dependent bugs.

#region Using directives
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using Microsoft.TeamFoundation.Converters.WorkItemTracking;
using Microsoft.TeamFoundation.Converters.Reporting;
using Stats = Microsoft.TeamFoundation.Converters.Reporting.ReportStatisticsStatisicsDetails;
using ClearQuestOleServer;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    /// <summary>
    /// Representation of a CQ record
    /// Min required data is the clearquest entity name + dbid
    /// and Currituck WIT name and id
    /// </summary>
    internal class CQEntityRec
    {
        #region Private Members
        private InMemoryWorkItem m_imWorkItem;
        private int m_dbid;
        private string m_sourceId;  // source id for CQ.. for stateful=>id else dbid
        private string m_entityName;
        private int m_WITId;

        // contains CQ entityname, vsts WIT name, vsts helper
        private SchemaMapping m_MySchemaMap;
        private OAdEntity m_CQEntity;

        // frequently used objects are stored in this data structure
        CQConverterParams m_cqParams;

        List<LinkRecord> m_referencedEntities;
        #endregion

        /// <summary>
        /// C'tor for creating insstance with minimal info
        /// </summary>
        /// <param name="entityDbId">DBID of the current record</param>
        /// <param name="curEntityName">Entity Name</param>
        /// <param name="pCQParams">ClearQuest Converter parameters</param>
        public CQEntityRec(int pEntityDbId,
                           string pCurEntityName,
                           CQConverterParams pCQParams
                           )
        {
            m_cqParams = pCQParams;
            m_dbid = pEntityDbId;
            m_entityName = pCurEntityName;
            m_referencedEntities = new List<LinkRecord>();

            // store the list of schema maps for passing into other 
            // recursive calls
            // find and load your own schema map
            foreach (SchemaMapping schMap in m_cqParams.schemaMaps)
            {
                if (TFStringComparer.WorkItemType.Equals(schMap.entity, pCurEntityName))
                {
                    m_MySchemaMap = schMap;
                    break;
                }
            }
        } // end of CQEntityRec CTor

        /// <summary>
        /// Populate the current record from CQ if its not already in Currituck
        /// and also process all its references (recursively), Links, History and Attachments
        /// Else just sets the currituck id for future reference
        /// </summary>
        public bool Populate()
        {
            bool partiallyMigrated = false;
            // first check if it exists in the memory cache
            CQEntity currentEntityRecords = m_cqParams.entityRecords[m_entityName];
            CQEntityRec lookupEntity = currentEntityRecords.FindEntityRec(m_entityName, m_dbid);
            if (lookupEntity != null)
            {
                // record already populated..
                Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Already populated record '{0}' for Entity '{1}', DBID:{2}",
                    lookupEntity.SourceId, lookupEntity.EntityName, lookupEntity.DBID);
                return true;
            }

            m_CQEntity = CQWrapper.GetEntityByDbId(m_cqParams.cqSession, m_entityName, m_dbid);

            // get the source id
            m_sourceId = CQWrapper.GetEntityDisplayName(m_CQEntity);
            Logger.Write(LogSource.CQ, TraceLevel.Verbose, UtilityMethods.Format(CQResource.CQ_PROCESSING_REC, m_sourceId));

            // check if it exist in currituck using static API
            VSTSWorkItemHelper wiHelper = (VSTSWorkItemHelper)m_MySchemaMap.vstsHelper;
            ArrayList checkList = new ArrayList();
            checkList.Add(new WorkItemNameValueRelation(CommonConstants.VSTSSrcIdField, m_sourceId));
            checkList.Add(new WorkItemNameValueRelation(CommonConstants.VSTSSrcDbField, m_cqParams.uniqueInstId));

            wiHelper = (VSTSWorkItemHelper)m_MySchemaMap.vstsHelper;
            if (wiHelper.IsWIMigrated(checkList) == true)
            {
                // need not to load the data from CQ..
                // just set the currituck id
                // not going to update this bug from CQ->Currituck even
                // if it is updated.. just get out from here as my population is done
                // with minimal required stuff
                string warningMsg = UtilityMethods.Format(CQResource.CQ_REC_MIGRATED, m_sourceId);
                Logger.Write(LogSource.CQ, TraceLevel.Warning, warningMsg);
                PostMigrationReport.WriteIssue(m_MySchemaMap.entity, m_MySchemaMap.WIT,
                                               Stats.MigrationStatus.Skipped,
                                               ReportIssueType.Warning,
                                               String.Empty,
                                               m_sourceId, IssueGroup.Wi, warningMsg);

                m_WITId = wiHelper.WorkItemId;
                //compact current object
                CompactMe();
                return true;
            }
            else if (wiHelper.IsCurrentWorkItemValid() == true)
            {
                // work item is already there.. partially migrated
                partiallyMigrated = true;
            }

#if DEBUG
            CommonConstants.NoOfBugs++;
#endif
            // create the required data structures
            m_imWorkItem = new InMemoryWorkItem();
            string fldName;

            OAdEntityDef curEntityDef = CQWrapper.GetEntityDef(m_cqParams.cqSession, m_entityName);
            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Adding record for Entity {0}, Record {1}",
                                m_entityName, CQWrapper.GetEntityDisplayName(m_CQEntity));

            object[] fields = (object[])CQWrapper.GetEntityFieldNames(m_CQEntity);
            foreach (object fld in fields)
            {
                fldName = (string)fld;
                if (CQConstants.InternalFieldTypes.ContainsKey(fldName))
                {
                    // these are internal clearquest fields
                    // we dont want to migrate these
                    Logger.Write(LogSource.CQ, TraceLevel.Info, "Skipping Internal Field '{0}' while migrating data for entity {1}",
                                fldName, m_entityName);
                    continue;
                }
                {
                    // process this field only if it exists in the "from" side of Field Map
                    OAdFieldInfo fldInfo = CQWrapper.GetEntityFieldValue(m_CQEntity, fldName);
                    int cqFieldType = CQWrapper.GetFieldType(fldInfo);

                    switch (cqFieldType)
                    {
                        case CQConstants.FIELD_ID:
                        case CQConstants.FIELD_SHORT_STRING:
                        case CQConstants.FIELD_INT:
                            {
                                string fldValue = CQWrapper.GetFieldValue(fldInfo);
                                if (fldValue != null)
                                {
                                    m_imWorkItem.InitialView.Add(fldName, fldValue);
                                }
                            }
                            break;

                        case CQConstants.FIELD_MULTILINE_STRING:
                            {
                                string fldValue = CQWrapper.GetFieldValue(fldInfo);
                                if (currentEntityRecords.Entity == null)
                                {
                                    // build entity to get the list of allowed/suggested values
                                    currentEntityRecords.Entity = CQWrapper.BuildEntity(m_cqParams.cqSession, currentEntityRecords.EntityName);
                                }
                                object[] choices = (object[])CQWrapper.GetFieldChoiceList(currentEntityRecords.Entity, fldName);
                                if (choices != null && choices.Length > 0)
                                {
                                    // Multi Line String with List of Allowed/Suggested Values.. replace all '\n' with comma
                                    // fix for bug# 429098
                                    if (fldValue != null)
                                    {
                                        fldValue = fldValue.Replace("\n", ",");
                                    }
                                }

                                /* no conversion shall be required.. bug# 20219 - shall be rendered in HTML as it is
                                    // hack for Notes_Log & Description field.. Shall be converted to HTML (bug#429032)
                                    if (fldName.Equals("Notes_Log", StringComparison.OrdinalIgnoreCase) ||
                                        fldName.Equals("Description", StringComparison.OrdinalIgnoreCase))
                                    {
                                        fldValue = VSTSUtil.ConvertTextToHtml(fldValue);
                                    }
                                 */
                                m_imWorkItem.InitialView.Add(fldName, fldValue);
                            }
                            break;

                        case CQConstants.FIELD_DATE_TIME:
                            {
                                string fldValue = CQWrapper.GetFieldValue(fldInfo);
                                if (fldValue != null)
                                {
                                    // the time returned from CQ API is the local time..
                                    DateTime fldVal = DateTime.Parse(fldValue, CultureInfo.CurrentCulture);

                                    //convert it in UTC
                                    DateTime utcTime = CQConverterUtil.ConvertLocalToUTC(fldVal);
                                    Logger.Write(LogSource.CQ, TraceLevel.Verbose,
                                        "Field [{0}], CQ Time [{1}], UTC Time [{2}]",
                                        fldName, fldVal.ToString(), utcTime.ToString());

                                    m_imWorkItem.InitialView.Add(fldName, utcTime);
                                }
                                else
                                {
                                    Logger.Write(LogSource.CQ, TraceLevel.Info, "Got null value for field {0}", fldName);
                                }
                            }
                            break;

                        case CQConstants.FIELD_REFERENCE:
                            {
                                // get the current entity def handle
                                OAdEntityDef refEntityDef = CQWrapper.GetFieldReferenceEntityDef(curEntityDef, fldName);
                                string refEntityName = CQWrapper.GetEntityDefName(refEntityDef);

                                // special handling for users.. add the user field value also.. 
                                // we dont want to create a link in this case..
                                // just add the field value pair in IMWorkItem.. and
                                // user map will be applied while saving
                                if (TFStringComparer.WorkItemType.Equals(refEntityName, "users"))
                                {
                                    if (CQWrapper.GetFieldValueStatus(fldInfo) == (int)CQConstants.FieldStatus.HAS_VALUE)
                                    {
                                        // single value required
                                        string refFldVal = CQWrapper.GetFieldValue(fldInfo);
                                        m_imWorkItem.InitialView.Add(fldName, refFldVal);
                                    }
                                }
                                else if (m_cqParams.allowedEntities.ContainsKey(refEntityName))
                                {
                                    int valueStatus = CQWrapper.GetFieldValueStatus(fldInfo);
                                    Logger.WriteIf((valueStatus != (int)CQConstants.FieldStatus.HAS_VALUE), LogSource.CQ,
                                                    TraceLevel.Info, "No Value for Referenced field {0} in Entity {1}",
                                                    refEntityName, m_entityName);
                                    if (valueStatus == (int)CQConstants.FieldStatus.HAS_VALUE)
                                    {
                                        // single value required
                                        string refFldVal = CQWrapper.GetFieldValue(fldInfo);
                                        if (String.Equals (refFldVal, SourceId, StringComparison.Ordinal))
                                        {
                                            // reference to self.. cannot have a link on to self
                                            string warningMsg = UtilityMethods.Format(CQResource.CQ_SELF_REFERENCE, SourceId, EntityName, fldName);
                                            Logger.Write(LogSource.CQ, TraceLevel.Warning, warningMsg);
                                            PostMigrationReport.WriteIssue(m_MySchemaMap.entity, m_MySchemaMap.WIT,
                                                                            Stats.MigrationStatus.Warning,
                                                                            ReportIssueType.Warning,
                                                                            String.Empty,
                                                                            m_sourceId, IssueGroup.Wi, warningMsg
                                                                            );
                                        }
                                        else
                                        {
                                            m_referencedEntities.Add(new LinkRecord(refEntityName, refFldVal));
                                        }
                                    }
                                }
                            }
                            break;

                        case CQConstants.FIELD_REFERENCE_LIST:
                            {
                                // get the current entity def handle
                                OAdEntityDef refEntityDef = CQWrapper.GetFieldReferenceEntityDef(curEntityDef, fldName);
                                string refEntityName = CQWrapper.GetEntityDefName(refEntityDef);
                                // special handling for user list
                                // we dont want to create a link in this case..
                                // concatenate all the user names separated by comma
                                // NO USER MAP WILL BE APPLIED WHILE SAVING (bug#400276)
                                if (TFStringComparer.WorkItemType.Equals(refEntityName, "users"))
                                {
                                    if (CQWrapper.GetFieldValueStatus(fldInfo) == (int)CQConstants.FieldStatus.HAS_VALUE)
                                    {
                                        object[] refFldValues = CQWrapper.GetFieldValueAsList(fldInfo);
                                        StringBuilder userList = new StringBuilder();
                                        for (int valueIndex = 0; valueIndex < refFldValues.Length; valueIndex++)
                                        {
                                            object refFldObj = refFldValues[valueIndex];
                                            if (valueIndex > 0)
                                            {
                                                userList.Append(",");
                                            }

                                            userList.Append((string)refFldObj);
                                        }
                                        m_imWorkItem.InitialView.Add(fldName, userList.ToString());
                                    }
                                }
                                else if (m_cqParams.allowedEntities.ContainsKey(refEntityName))
                                {
                                    int valueStatus = CQWrapper.GetFieldValueStatus(fldInfo);
                                    Logger.WriteIf((valueStatus != (int)CQConstants.FieldStatus.HAS_VALUE), LogSource.CQ,
                                                    TraceLevel.Info, "No Value for Referenced field {0} in Entity {1}",
                                                    fldName, m_entityName);
                                    if (valueStatus == (int)CQConstants.FieldStatus.HAS_VALUE)
                                    {
                                        // value list expected
                                        object[] refFldValues = CQWrapper.GetFieldValueAsList(fldInfo);
                                        foreach (object refFldObj in refFldValues)
                                        {
                                            string refFldVal = (string)refFldObj;
                                            if (String.Equals (refFldVal, SourceId, StringComparison.Ordinal))
                                            {
                                                // reference to self.. cannot have a link on to self
                                                string warningMsg = UtilityMethods.Format(CQResource.CQ_SELF_REFERENCE, SourceId, EntityName, fldName);
                                                Logger.Write(LogSource.CQ, TraceLevel.Warning, warningMsg);
                                                PostMigrationReport.WriteIssue(m_MySchemaMap.entity, m_MySchemaMap.WIT,
                                                                               Stats.MigrationStatus.Warning,
                                                                                ReportIssueType.Warning,
                                                                                String.Empty,
                                                                                m_sourceId, IssueGroup.Wi, warningMsg);
                                            }
                                            else
                                            {
                                                m_referencedEntities.Add(new LinkRecord(refEntityName, refFldVal));
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case CQConstants.FIELD_ATTACHMENT_LIST:
                        case CQConstants.FIELD_STATE:
                        case CQConstants.FIELD_JOURNAL:
                        case CQConstants.FIELD_DBID:
                        case CQConstants.FIELD_STATETYPE:
                        case CQConstants.FIELD_RECORDTYPE:
                            Logger.Write(LogSource.CQ, TraceLevel.Info, "Skipping the Field migration for Internal Field Type '{0}'",
                                                    cqFieldType);
                            // not migrating these fields as they are CQ internal fields
                            continue;
                        default:
                            Logger.Write(LogSource.CQ, TraceLevel.Info, "Skipping the Field migration for Unkknown Field Type '{0}'",
                                                    cqFieldType);
                            break;
                    }
                }
            } // end of foreachfields

            // add the source id and db separately
            m_imWorkItem.InitialView.Add(CommonConstants.VSTSSrcIdField, m_sourceId);
            m_imWorkItem.InitialView.Add(CommonConstants.VSTSSrcDbField, m_cqParams.uniqueInstId);

            // use vstshelper to migrate the data
            wiHelper = (VSTSWorkItemHelper)m_MySchemaMap.vstsHelper;
            wiHelper.IsWIMigrated(checkList);

            // get attachments in the imworkitem
            ProcessAttachments();

            // history processing will use same imWorkItem for first history info
            // and create other history indexes
            int migratedHistory = 0;
            if (wiHelper.IsCurrentWorkItemValid())
            {
                migratedHistory = wiHelper.GetCurrentWorkItemHistoryCount();
                if (migratedHistory > 0)
                {
                    // We are going for incremental migration. And as we stuff first history item of a CQBug
                    // into InitialView itself, actual no. of migrated history is one more than the value of
                    // the "Migration Status" field. So increment by one.
                    ++migratedHistory;
                }
            }

            ArrayList historyItems = ProcessHistory(m_imWorkItem.InitialView, migratedHistory);

            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Dumping initial view for {0}", m_sourceId);
            foreach (object key in m_imWorkItem.InitialView.Keys)
            {
                Logger.Write(LogSource.CQ, TraceLevel.Verbose, "{0} - {1}", key, m_imWorkItem.InitialView[key]);
            }

            bool initialViewStatus = true;
            try
            {
                if (!partiallyMigrated)
                {
                    // if some history items or links are left to be migrated.. leave the bug as opened..
                    if (historyItems.Count > 0 || m_referencedEntities.Count > 0)
                    {
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Creating initial view of {0} .. {1} Histories, {2} Links pending",
                                    SourceId, historyItems.Count, m_referencedEntities.Count);
                        // create the record and keep it open for history editing
                        initialViewStatus = wiHelper.CreateInitialViewOfWorkItem(m_sourceId, m_imWorkItem, false);
                    }
                    else
                    {
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Creating initial view of {0}", SourceId);
                        // create all the entries in the record and set the status to done
                        initialViewStatus = wiHelper.CreateInitialViewOfWorkItem(m_sourceId, m_imWorkItem, true);
                    }
                }
            }
            catch (Exception ex)
            {
                // creation of work item failed
                string errMsg = UtilityMethods.Format(CQResource.CQ_WI_CREATION_FAILED, SourceId, ex.Message);
                CQConverter.ReportWorkItemFailure(errMsg, SourceId, m_MySchemaMap.entity, m_MySchemaMap.WIT,
                    m_cqParams.exitOnError);
                if (m_cqParams.exitOnError == true)
                {
                    throw new ConverterException(errMsg);
                }
                else
                {
                    // continue with another work item
                    // need to skip this work item..
                    m_WITId = -1;
                    CompactMe();
                    return false;
                }
            }
            finally
            {
            }

            // get back currituck id and store in this
            m_WITId = wiHelper.WorkItemId;

            // store the handle of work item to restore the state of work item helper back to 
            // working work item which may get changed because of processing links recursively
            object workItem = wiHelper.GetCurrentWorkItem();

            // before processing history, clean out attachments.. only if its already migrated
            if (wiHelper.GetCurrentWorkItemAttachmentsCount() == m_imWorkItem.Attachments.Count)
            {
                m_imWorkItem.Attachments.Clear();
            }

            // add all the links now so that they go as part of history
            bool refRecordStatus = true;
            foreach (LinkRecord linkRec in m_referencedEntities)
            {
                if (AddReferenceRecord(linkRec) == false)
                {
                    refRecordStatus = false; // once false always false
                }
            }

            // process duplicate records
            if (ProcessDuplicates(m_cqParams) == false)
            {
                refRecordStatus = false;
            }
            
            bool writeHistoryPassed = true;
            wiHelper.SetCurrentWorkItem(workItem);
            if (historyItems.Count > 0 || m_imWorkItem.Links.Count > 0 || m_imWorkItem.Attachments.Count > 0)
            {
                m_imWorkItem.HistoryItems = historyItems;

                try
                {
                    writeHistoryPassed = wiHelper.WriteHistoryItems(m_sourceId, m_imWorkItem, 
                                              refRecordStatus && initialViewStatus);
                    if (!writeHistoryPassed)
                    {
                        // Bug#59861: In the case of the partially migrated bug, 
                        // converter says all bugs migrated successfully in 
                        // summary, but in error section it says one bug  failed 
                        // due to attachment size issue. This issue has already 
                        // been written to the report. Just need to update the 
                        // statistics info.
                        PostMigrationReport.WriteIssue(m_MySchemaMap.entity, 
                                                   m_MySchemaMap.WIT,
                                                   Stats.MigrationStatus.Failed,
                                                   ReportIssueType.Info,
                                                   null, m_sourceId, IssueGroup.Wi, 
                                                   null);
                    }
                    // set the bug migration status to done only if there were no 
                    // problems with  initial view and any of the references
                    if ((!writeHistoryPassed || !refRecordStatus || !initialViewStatus) &&
                        m_cqParams.exitOnError)
                    {
                        // stop processing more records
                        CompactMe();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // creation of history failed
                    string errMsg = UtilityMethods.Format(CQResource.CQ_WI_MIG_FAILED, SourceId, ex.Message);
                    CQConverter.ReportWorkItemFailure(errMsg, SourceId, m_MySchemaMap.entity, m_MySchemaMap.WIT,
                        m_cqParams.exitOnError);
                    if (m_cqParams.exitOnError == true)
                    {
                        throw new ConverterException(errMsg);
                    }
                    else
                    {
                        // continue with another work item.. reporting this failure
                        CompactMe();
                        return false;
                    }
                } // end of catch
                finally
                {
                }

            } // end of history items processing

            // add to pass count
            ConverterMain.MigrationReport.Statistics.NumberOfItems++;

            // add to per work item type section
            if (writeHistoryPassed)
            {
                PostMigrationReport.WriteIssue(m_MySchemaMap.entity, m_MySchemaMap.WIT,
                                           Stats.MigrationStatus.Passed,
                                            ReportIssueType.Info,
                                            null, m_sourceId, IssueGroup.Wi, null);
            }
            //compact current object
            CompactMe();
            return true;
        } // end of Populate()

        /// <summary>
        /// Processes the history for current record.
        /// </summary>
        /// <param name="initialView">First history item structure</param>
        /// <param name="migratedHistory">No of history items already migrated</param>
        /// <returns>List of history items except first one</returns>
        private ArrayList ProcessHistory(Hashtable initialView, int migratedHistory)
        {
            int noOfHistory = 0;
            ArrayList historyItems = new ArrayList();
            try
            {
                Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Processing History for {0} : {1}", m_entityName, m_sourceId);

                // record all history items except the first one
                Hashtable currentHistory = null;
                OAdHistoryFields cqHistFields = CQWrapper.GetHistoryFields(m_CQEntity);
                int historyFldCount = CQWrapper.HistoryFieldsCount(cqHistFields);
                for (int histFldIndex = 0; histFldIndex < historyFldCount; histFldIndex++)
                {
                    object ob = (object)histFldIndex;
                    OAdHistoryField historyField = CQWrapper.HistoryFieldsItem(cqHistFields, ref ob);

                    int historyCount = CQWrapper.HistoryFieldHistoriesCount(historyField);
                    for (int histIndex = migratedHistory; histIndex < historyCount; histIndex++)
                    {
                        if (histIndex == 0)
                        {
                            // first history.. use the initial view to record history
                            currentHistory = initialView;
                        }
                        else
                        {
                            // create a new instance to record history
                            InMemoryHistoryItem imHistItem = new InMemoryHistoryItem();
                            historyItems.Add(imHistItem);
                            currentHistory = imHistItem.UpdatedView;
                        }

                        object obHistIndex = (object)histIndex;
                        OAdHistory aHistory = CQWrapper.HistoryFieldHistoriesItem(historyField, ref obHistIndex);
                        string[] parsedString = CQWrapper.HistoryValue(aHistory).Split('\t');
                        Debug.Assert(parsedString != null && parsedString.Length >= 6);
                        string date = parsedString[1];
                        string user = parsedString[2];
                        string action = parsedString[3];
                        string oldstate = parsedString[4];
                        string newstate = parsedString[5];

                        DateTime changedDate = DateTime.Parse(date, CultureInfo.CurrentCulture);

                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "History Item : [{0}] [{1}] [{2}] [{3}]",
                                                    changedDate, user, oldstate, newstate);

                        string historyval = UtilityMethods.Format(CQResource.CQ_HISTORY_STRING, changedDate.ToString(CultureInfo.CurrentCulture),
                                    user, action, oldstate, newstate);
                        currentHistory.Add("History", historyval);
                        currentHistory.Add("user_name", user);
                        currentHistory.Add("Reason", action);

                        if (!String.Equals ("N/A", newstate, StringComparison.Ordinal))
                        {
                            // add state only if state is valid
                            currentHistory.Add("State", newstate);
                        }
                        noOfHistory++;
#if DEBUG
                        CommonConstants.NoOfHistory++;
#endif
                    }
                }
            }
            finally
            {
            }
            return historyItems;
        } // end of ProcessHistory()

        /// <summary>
        /// Processes the attachments for current record
        /// </summary>
        private void ProcessAttachments()
        {
            int noOfAttachments = 0;
            try
            {
                OAdAttachmentFields allAttachments = CQWrapper.GetAttachmentFields(m_CQEntity);
                StringCollection filesAttached = new StringCollection();

                int attachmentsIndex = 0;
                for (attachmentsIndex = 0;
                     attachmentsIndex < CQWrapper.AttachmentsFieldsCount(allAttachments);
                     attachmentsIndex++)
                {
                    object ob = (object)attachmentsIndex;
                    OAdAttachmentField attachmentFld = CQWrapper.AttachmentsFieldsItem(allAttachments, ref ob);

                    // process all attachments
                    OAdAttachments attachments = CQWrapper.GetAttachments(attachmentFld);
                    int attachmentIndex;
                    for (attachmentIndex = 0;
                         attachmentIndex < CQWrapper.AttachmentsCount(attachments);
                         attachmentIndex++)
                    {
                        // there are some attachments

                        // create the dir for this bug dbid only if
                        string dirName = Path.Combine(CQConstants.AttachmentsDir, m_dbid.ToString());

                        if (attachmentIndex == 0)   // for the first time
                        {
                            // check if dir with curr dbid exist
                            if (Directory.Exists(dirName))
                            {
                                // if the dir exists, clean the contents
                                // as it is the system created dir
                                Logger.Write(LogSource.CQ, TraceLevel.Warning,
                                    "Removing folder {0} recursively for attachments of entity {1}",
                                    m_dbid, m_entityName);

                                try
                                {
                                    Directory.Delete(dirName, true);
                                }
                                catch (IOException ioe)
                                {
                                    Logger.Write(LogSource.CQ, TraceLevel.Warning, "Failed to delete folder {0} containing attachments for work item {1}",
                                        dirName, SourceId);
                                    Logger.WriteException(LogSource.CQ, ioe);
                                }
                                catch (UnauthorizedAccessException uae)
                                {
                                    Logger.Write(LogSource.CQ, TraceLevel.Warning, "Permission denied for deleting folder {0} containing attachments for work item {1}",
                                        dirName, SourceId);
                                    Logger.WriteException(LogSource.CQ, uae);
                                }
                            }

                            Logger.Write(LogSource.CQ, TraceLevel.Info,
                                "Creating folder {0} for attachments of entity {1}",
                                m_dbid, m_entityName);
                            UtilityMethods.CreateDirectory(dirName);
                        }

                        // for every attachments attachment create a separate 
                        // sub dir as (attchmentsIndex+attchmentIndex)
                        string currAttachmentDir = Path.Combine(dirName, attachmentsIndex.ToString(CultureInfo.InvariantCulture));
                        currAttachmentDir = Path.Combine(currAttachmentDir, attachmentIndex.ToString(CultureInfo.InvariantCulture));

                        // there cannot be any dir existing as we already cleaned up
                        Logger.Write(LogSource.CQ, TraceLevel.Info,
                            "Creating folder {0} for attachment {1} of entity {2}",
                            currAttachmentDir, attachmentIndex + 1, m_entityName);
                        UtilityMethods.CreateDirectory(currAttachmentDir);

                        object obIndex = (object)attachmentIndex;
                        OAdAttachment aAttachment = CQWrapper.AttachmentsItem(attachments, ref obIndex);
                        noOfAttachments++;
                        string attachFileName;
                        string attachDescription;
                        CQWrapper.GetAttachmentFileNameAndDescription(aAttachment, out attachFileName, out attachDescription);
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Adding attachment");
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "\tFile Name : {0}", attachFileName);
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "\tDescription : {0}", attachDescription);

                        // save the attachment as file on disk
                        string tempfile = Path.Combine(currAttachmentDir, attachFileName);
                        CQWrapper.LoadAttachment(aAttachment, tempfile);
                        m_imWorkItem.Attachments.Add(new InMemoryAttachment(tempfile, attachDescription, false));
                        filesAttached.Add(attachFileName);
#if DEBUG
                        CommonConstants.NoOfAttachments++;
                        CommonConstants.TotalAttachmentSize += CQWrapper.AttachmentFileSize(aAttachment);
#endif
                    } // end of processing attachment
                } // end of processing attachments
            }
            finally
            {
            }
        } // end of GetAttachments


        /// <summary>
        /// Populate and migrate the reference type record, if not already migrated
        /// also sets the required link information
        /// </summary>
        /// <param name="linkRec">Linked Record information</param>
        /// <returns>true if all referenced records are migrated successfully</returns>
        private bool AddReferenceRecord(LinkRecord linkRec)
        {
# if DEBUG
            CommonConstants.NoOfLinks++;
#endif
            bool status = true; // set initial value to true
            try
            {
                OAdEntity refEntity = CQWrapper.GetEntity(m_cqParams.cqSession, linkRec.EntityName, linkRec.FieldValue);
                string refEntityId = CQWrapper.GetEntityDisplayName(refEntity);

                int refdbid = CQWrapper.GetEntityDbId(refEntity);

                // check if this entity is already processed and exist in our cache
                CQEntity refEntityRecords = m_cqParams.entityRecords[linkRec.EntityName];
                CQEntityRec recInCache = refEntityRecords.FindEntityRec(linkRec.EntityName, refdbid);

                if (recInCache == null)
                {
                    // cannot find the referenced entity..
                    // load from CQ database and migrate..
                    CQEntityRec refEntityRec = new CQEntityRec(refdbid, linkRec.EntityName, m_cqParams);

                    // add to our cache data structure
                    refEntityRecords.AddRecord(refEntityRec);
                    status = refEntityRec.Populate();

                    // either whole record is migrated or atleast initial view is created successfully.. 
                    if (refEntityRec.WITId > 0)
                    {
                        // add the link info also for new record
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Creating link from {0} to {1}", SourceId, refEntityRec.SourceId);
                        m_imWorkItem.Links.Add(new InMemoryLinkItem(refEntityRec.WITId, UtilityMethods.Format(CQResource.CQ_LINK_COMMENT, SourceId, refEntityRec.SourceId)));
                    }
                    else
                    {
                        Logger.Write(LogSource.CQ, TraceLevel.Info, "Migration of referenced entity {0} failed, creating dummy link", linkRec.FieldValue);
                    }
                }
                else
                {
                    string warningMsg = UtilityMethods.Format(CQResource.CQ_REC_MIGRATED, CQWrapper.GetEntityDisplayName(refEntity));
                    Logger.Write(LogSource.CQ, TraceLevel.Warning, warningMsg);

                    // add the link info only with the existing record info
                    // to be processed for creating links
                    Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Creating link from {0} to {1}", SourceId, recInCache.SourceId);
                    m_imWorkItem.Links.Add(new InMemoryLinkItem(recInCache.WITId, UtilityMethods.Format(CQResource.CQ_LINK_COMMENT, SourceId, recInCache.SourceId)));
                }
            }
            catch (ConverterException conEx)
            {
                status = false;
                string errMsg = UtilityMethods.Format(CQResource.CQ_REF_REC_FAILED, linkRec.FieldValue, SourceId, conEx.Message);
                CQConverter.ReportWorkItemFailure(errMsg, linkRec.FieldValue, m_MySchemaMap.entity,
                    m_MySchemaMap.WIT, m_cqParams.exitOnError);
                if (m_cqParams.exitOnError == true)
                {
                    // throw the error back .. should not continue with the current record
                    throw;
                }
            }
            return status;
        } // end of AddReferenceRecord

        /// <summary>
        /// Nullify the CQ loaded data in current entity
        /// Deletes the temp folder (if exist) for attachments
        /// </summary>
        private void CompactMe()
        {
            // clean up the attachments folder as the migration is already done
            // check if dir with curr dbid exist
            string dirName = m_dbid.ToString(CultureInfo.InvariantCulture);
            if (Directory.Exists(dirName))
            {
                // if the dir exists, clean the contents
                // as it is the system created dir
                Logger.Write(LogSource.CQ, TraceLevel.Verbose,
                    "Removing folder {0} recursively for attachments of entity {1}",
                    m_dbid, m_entityName);

                try
                {
                    Directory.Delete(dirName, true);
                }
                catch (IOException ioe)
                {
                    Logger.Write(LogSource.CQ, TraceLevel.Warning, "Failed to delete folder {0} containing attachments for work item {1}",
                        dirName, SourceId);
                    Logger.WriteException(LogSource.CQ, ioe);
                }
                catch (UnauthorizedAccessException uae)
                {
                    Logger.Write(LogSource.CQ, TraceLevel.Warning, "Permission denied for deleting folder {0} containing attachments for work item {1}",
                        dirName, SourceId);
                    Logger.WriteException(LogSource.CQ, uae);
                }
            }

            m_imWorkItem = null;
            m_CQEntity = null;
            m_referencedEntities = null;
        }

        /// <summary>
        /// Process the bug if it is duplicate of some other bug
        /// </summary>
        /// <param name="convParams">Converter Parameters</param>
        /// <returns>True if all duplicate bugs migrated successfully, else false</returns>
        private bool ProcessDuplicates(CQConverterParams convParams)
        {
            bool retVal = true;
            string parentId = String.Empty;
            try
            {
                // migrate it as a related link
                if (CQWrapper.IsDuplicateEntity(m_CQEntity) == true)
                {
                    parentId = CQWrapper.GetOriginalEntityId(m_CQEntity);
                    OAdEntity parentEntity = CQWrapper.GetOriginalEntity(m_CQEntity);
                    if (parentEntity != null)
                    {
                        string parentEntityName = CQWrapper.GetEntityDefName(parentEntity);
                        if (convParams.allowedEntities.ContainsKey(parentEntityName))
                        {
# if DEBUG
                            CommonConstants.NoOfLinks++;
#endif
                            // duplicate of some allowed entities
                            string parentEntityId = CQWrapper.GetEntityDisplayName(parentEntity);

                            // check if the parent record is in currituck
                            ArrayList checkList = new ArrayList();
                            checkList.Add(new WorkItemNameValueRelation(CommonConstants.VSTSSrcIdField, parentEntityId));
                            checkList.Add(new WorkItemNameValueRelation(CommonConstants.VSTSSrcDbField, m_cqParams.uniqueInstId));
                            checkList.Add(new WorkItemNameValueRelation("System.WorkItemType", m_MySchemaMap.WIT));
                            checkList.Add(new WorkItemNameValueRelation("System.AreaPath", convParams.vstsConn.projectName,
                                            WorkItemNameValueRelation.RelationShipType.UNDER));
                            int parentCurrituckId = VSTSUtil.GetIdForWorkItem(VSTSUtil.FindWorkItem(convParams.vstsConn, checkList));
                            if (parentCurrituckId <= 0)
                            {
                                Logger.Write(LogSource.CQ, TraceLevel.Info,
                                    "Migrating record for Work Item {0} as {1} is duplicate of {0}",
                                    parentEntityId, this.SourceId);

                                // not found in currituck.. import that record also and proceed
                                // if this record happens to come in same run later, will be taken care by
                                // checking with cache before recreating it
                                int parentDbId = CQWrapper.GetEntityDbId(parentEntity);
                                CQEntityRec dupRecord = new CQEntityRec(parentDbId, parentEntityName, m_cqParams);

                                CQEntity parentEntityCache = convParams.entityRecords[parentEntityName];
                                parentEntityCache.AddRecord(dupRecord);

                                // now populate the whole record
                                retVal = dupRecord.Populate();
                                parentCurrituckId = dupRecord.WITId;
                            }
                            string duplicateLinkComment = UtilityMethods.Format(CQResource.CQ_DUPLICATE_LINK_COMMENT,
                                            SourceId, parentEntityId);
                            m_imWorkItem.Links.Add(new InMemoryLinkItem(parentCurrituckId, duplicateLinkComment));
                        }
                        else
                        {
                            string warningMsg = UtilityMethods.Format(CQResource.CQ_DUPLICATE_LINK_SKIP, SourceId, parentEntityName);
                            Logger.Write(LogSource.CQ, TraceLevel.Info, warningMsg);
                            PostMigrationReport.WriteIssue(m_MySchemaMap.entity, m_MySchemaMap.WIT,
                                                           Stats.MigrationStatus.Warning,
                                                            ReportIssueType.Warning,
                                                            String.Empty,
                                                            m_sourceId, IssueGroup.Wi, warningMsg);
                        }
                    }
                }
            }
            catch (ConverterException conEx)
            {
                retVal = false;
                string errMsg = UtilityMethods.Format(CQResource.CQ_DUP_REC_FAILED, parentId, m_sourceId, conEx.Message);
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                CQConverter.ReportWorkItemFailure(errMsg, parentId, m_MySchemaMap.entity, m_MySchemaMap.WIT,
                        m_cqParams.exitOnError);
                if (m_cqParams.exitOnError == true)
                {
                    // throw the error back .. should not continue with the current record
                    throw;
                }
            }
            return retVal;
        }   // end of ProcessDuplicates

        /// <summary>
        /// Returns CQ DBID of current record
        /// </summary>
        /// <returns>Entity DbId</returns>
        public int DBID
        {
            get { return m_dbid; }
        }

        /// <summary>
        /// Rerturns CQ Entity Name of current record
        /// </summary>
        /// <returns>Entity Name</returns>
        public string EntityName
        {
            get { return m_entityName; }
        }

        /// <summary>
        /// Returns Currituck id of current record
        /// </summary>
        /// <returns>Work Item Type Id</returns>
        public int WITId
        {
            get { return m_WITId; }
        }

        /// <summary>
        /// Returns currituck WIT Name of current record
        /// </summary>
        /// <returns>Work Item type name</returns>
        public string WITName
        {
            get { return m_MySchemaMap.WIT; }
        }

        /// <summary>
        /// Source id of current record. For stateless, its display name
        /// </summary>
        /// <returns></returns>
        public string SourceId
        {
            get { return m_sourceId; }
        }
    } // end of class CQEntityRec

    internal struct LinkRecord
    {
        private string m_entityName;
        private string m_fieldValue;

        public string EntityName
        {
            get { return m_entityName; }
        }

        public string FieldValue
        {
            get { return m_fieldValue; }
        }

        public LinkRecord(string entity, string val)
        {
            m_entityName = entity;
            m_fieldValue = val;
        }
    }
}
