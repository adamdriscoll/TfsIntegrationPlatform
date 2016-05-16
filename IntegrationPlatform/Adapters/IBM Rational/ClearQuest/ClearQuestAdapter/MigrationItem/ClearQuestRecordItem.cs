// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Globalization;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem
{
    /// <summary>
    /// Class MgrtRecordItem that models the migraiton item of 
    /// 1. CQ record content (field values) at a particular time
    /// 2. CQ record history 
    /// </summary>
    /// <remarks>
    /// Note that in ClearQuest, record history only keeps tracks of the state transition and 
    /// the action being taken for each update. Thus, we get the leaf version of the *complete* 
    /// CQ record content when we detect a record has been updated since last sync point.
    /// Note also that the CQ record history is simply a string containing basic description
    /// of an update. 
    /// This class models both record content and record history.
    /// </remarks>
    [Serializable]
    public sealed class ClearQuestRecordItem : IMigrationItem
    {
        private static IFieldSkipAlgorithm s_skipAlgorithm;
        internal const string NewRecordVersion = "1"; // do not localize
        internal const int NewRecordVersionValue = 1;
        internal const string RecordUpdateVersion = "Update"; // do not localize

        private Dictionary<string, OAdEntity> m_perEntityTypeTestEntities = new Dictionary<string, OAdEntity>();

        internal static string GetMigrationRecordId(
            string entityDefName,
            string entityDispName)
        {
            return UtilityMethods.CreateCQRecordMigrationItemId(entityDefName, entityDispName);
        }

        static ClearQuestRecordItem()
        {
            s_skipAlgorithm = new InternalFieldSkipLogic();
        }

        /// <summary>
        /// Default c'tor for serialization.
        /// </summary>
        public ClearQuestRecordItem()
        {
        }

        public ClearQuestRecordItem(OAdEntity aRecord, string version)
        {
            Initialize(CQWrapper.GetEntityDefName(aRecord),
                       CQWrapper.GetEntityDisplayName(aRecord),
                       string.Empty,
                       false);

            Version = version;
        }

        public ClearQuestRecordItem(
            OAdEntity aRecord,
            OAdHistory aHistory,
            string historyFieldName,
            string historyIndex)
        {
            Initialize(CQWrapper.GetEntityDefName(aRecord),
                       CQWrapper.GetEntityDisplayName(aRecord),
                       CQWrapper.HistoryValue(aHistory),
                       true);

            Version = historyIndex;
            HistoryFieldName = historyFieldName;
        }

        /// <summary>
        /// Gets flag to tell if this MgrtRecordItem is about 
        /// - a CQ Record History; or
        /// - a CQ Record Content
        /// </summary>
        public bool IsRecordHistory
        {
            get;
            set;
        }

        /// <summary>
        /// The record history string. One line string returned by CQ API.
        /// </summary>
        public string HistoryValue
        {
            get;
            set;
        }

        /// <summary>
        /// CQ History field name
        /// </summary>
        public string HistoryFieldName
        {
            get;
            set;
        }

        /// <summary>
        /// Entity Type Definition Name
        /// </summary>
        public string EntityDefName
        {
            get;
            set;
        }

        /// <summary>
        /// Entity/Record display name
        /// </summary>
        /// <remarks>
        /// In CQ, for stateful=>id else dbid
        /// </remarks>
        public string EntityDispName
        {
            get;
            set;
        }

        /// <summary>
        /// Version
        /// </summary>
        public string Version
        {
            get;
            set;
        }

        /// <summary>
        /// Current CQ session for accessing the record
        /// </summary>
        [XmlIgnore]
        public Session CQSession
        {
            get;
            set;
        }

        #region IMigrationItem Members

        /// <summary>
        /// Download the XML document describing this CQ record
        /// </summary>
        /// <remarks>
        /// EntityDisplayName, EntityDefName and CQSession properties are needed to retrieve record details
        /// </remarks>
        /// <param name="localPath"></param>
        public void Download(string localPath)
        {
            if (null == CQSession)
            {
                throw new InvalidOperationException("CQSession == NULL");
            }
            
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }

            if (IsRecordHistory)
            {
                // download the record history
                OAdEntity record = CQWrapper.GetEntity(CQSession, EntityDefName, EntityDispName);
                XmlDocument recordHistDesc = CreateRecordHistoryDesc(record, HistoryValue, HistoryFieldName + "::" + Version, false);
                recordHistDesc.Save(localPath);
            }
            else
            {
                // download the *complete* record content (field + value)
                OAdEntity record = CQWrapper.GetEntity(CQSession, EntityDefName, EntityDispName);
                XmlDocument recordDesc = CreateRecordDesc(record, Version, null, false);
                recordDesc.Save(localPath);
            }
        }

        public string DisplayName
        {
            get;
            set;
        }

        #endregion

        private string ChangeGroupName
        {
            get
            {
                if (IsRecordHistory)
                {
                    return EntityDefName + ":" + EntityDispName + ":" + HistoryFieldName + ":" + Version;
                }
                else
                {
                    return EntityDefName + ":" + EntityDispName + ":" + Version;
                }
            }
        }

        private string MigrationRecordId
        {
            get
            {
                return GetMigrationRecordId(EntityDefName, EntityDispName);
            }
        }

        internal ChangeGroup CreateChangeGroup(
            ChangeGroupService changeGroupService,
            ClearQuestMigrationContext migrationContext,
            bool isLastRevOfThisSyncCycle)
        {
            ChangeGroup changeGroup = changeGroupService.CreateChangeGroupForDeltaTable(ChangeGroupName);
            OAdEntity record = CQWrapper.GetEntity(CQSession, EntityDefName, EntityDispName);

            if (IsRecordHistory)
            {
                XmlDocument recordHistDesc = CreateRecordHistoryDesc(record, HistoryValue, HistoryFieldName + "::" + Version, isLastRevOfThisSyncCycle);

                changeGroup.CreateAction(WellKnownChangeActionId.Edit,
                                         this,
                                         MigrationRecordId,
                                         "",
                                         Version,
                                         "",
                                         WellKnownContentType.WorkItem.ReferenceName,
                                         recordHistDesc);
            }
            else
            {
                XmlDocument recordDesc = CreateRecordDesc(record, Version, migrationContext, isLastRevOfThisSyncCycle);

                if (Version.Equals(NewRecordVersion, StringComparison.InvariantCulture))
                {
                    changeGroup.CreateAction(WellKnownChangeActionId.Add,
                                              this,
                                              MigrationRecordId,
                                              "",
                                              Version,
                                              "",
                                              WellKnownContentType.WorkItem.ReferenceName,
                                              recordDesc);
                }
                else
                {
                    changeGroup.CreateAction(WellKnownChangeActionId.Edit,
                                              this,
                                              MigrationRecordId,
                                              "",
                                              Version,
                                              "",
                                              WellKnownContentType.WorkItem.ReferenceName,
                                              recordDesc);
                }
            }
        

            return changeGroup;
        }

        private void Initialize(
            string entityDefName,
            string entityDispName,
            string history,
            bool isRecordHistory)
        {
            EntityDefName = entityDefName;
            EntityDispName = entityDispName;
            DisplayName = GetMigrationRecordId(entityDefName, entityDispName);
            HistoryValue = history;
            IsRecordHistory = isRecordHistory;
        }
    
        public XmlDocument CreateRecordDesc(
            OAdEntity record, 
            string versionStr,
            ClearQuestMigrationContext migrationContext,
            bool isLastRevOfThisSyncCycle)
        {
            string lastAuthor;
            DateTime lastChangeDate;

            FindLastRevDtls(record, out lastAuthor, out lastChangeDate);

            ClearQuestRecordDescription recordDesc = new ClearQuestRecordDescription();
            recordDesc.CreateHeader(lastAuthor, lastChangeDate, MigrationRecordId, EntityDefName, versionStr, isLastRevOfThisSyncCycle);

            object[] fieldNames = (object[])CQWrapper.GetEntityFieldNames(record);
            foreach (object fldName in fieldNames)
            {
                string fieldName = (string)fldName;
                if (s_skipAlgorithm.SkipField(fieldName))
                {
                    TraceManager.TraceInformation("Skipping Field '{0}' while migrating data for Entity '{1}'",
                                                  fieldName, EntityDefName);
                    continue;
                }

                OAdFieldInfo fldInfo = CQWrapper.GetEntityFieldValue(record, fieldName);
                int cqFieldType = CQWrapper.GetFieldType(fldInfo);

                switch (cqFieldType)
                {
                    case CQConstants.FIELD_INT:
                    case CQConstants.FIELD_ID:
                    case CQConstants.FIELD_SHORT_STRING:
                    case CQConstants.FIELD_STATE:
                    case CQConstants.FIELD_DBID:
                    case CQConstants.FIELD_STATETYPE:
                    case CQConstants.FIELD_RECORDTYPE:
                        {
                            string fldValue = CQWrapper.GetFieldValue(fldInfo);
                            recordDesc.AddField(fieldName, string.Empty, fldValue ?? String.Empty);
                        }
                        break;

                    case CQConstants.FIELD_MULTILINE_STRING:
                        {
                            string fldValue = CQWrapper.GetFieldValue(fldInfo);

                            if (migrationContext == null
                                || !CQStringComparer.FieldName.Equals(migrationContext.NotesLogFieldName, fieldName))
                            {
                                // non-log field
                                try
                                {
                                    var aTestEntity = CreateTestEntity(record, migrationContext);

                                    object[] choices = (object[])CQWrapper.GetFieldChoiceList(aTestEntity, fieldName);
                                    if (choices != null && choices.Length > 0)
                                    {
                                        // Multi Line String with List of Allowed/Suggested Values.. replace all '\n' with comma
                                        // fix for bug# 429098
                                        if (fldValue != null)
                                        {
                                            fldValue = fldValue.Replace("\n", ",");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // NOTE:
                                    // This feature of testing the multiline string choices create a dummy CQ record
                                    // The API call CreateTestEntity requires WRITE permission to CQ
                                    // If the migration account doesn't have the permission, we will simply use
                                    // the field's current value
                                    TraceManager.TraceInformation(
                                        "Skipping retrieval of Allowed Values for field '{0}' - Write permission is needed. Error: {1}",
                                        fieldName, ex.Message);
                                }

                                recordDesc.AddField(fieldName, string.Empty, fldValue);
                            }
                            else if (fldValue != null)
                            {
                                // log field

                                StringBuilder sb = new StringBuilder();
                                List<CQNotesLog> notes = CQNotesLog.Parse(fldValue);
                                foreach (CQNotesLog note in notes)
                                {
                                    if (string.IsNullOrEmpty(note.Content)
                                        || note.Content.Contains(Constants.PlatformCommentSuffixMarker))
                                    {
                                        // skip empty logs or those generated by this adapter
                                        continue;
                                    }

                                    if (note.Header.ChangeDate.CompareTo(migrationContext.CurrentHWMBaseLine) <= 0)
                                    {
                                        // skip the logs before the hwm
                                        continue;
                                    }

                                    sb.AppendFormat("{0} {1} {2}\n",
                                                    CQNotesLogHeader.NotesLogHeaderIdentifier,
                                                    note.HeaderString,
                                                    CQNotesLogHeader.NotesLogHeaderIdentifier);
                                    sb.AppendLine(note.Content);
                                }

                                string extractedLog = sb.ToString();

                                if (!string.IsNullOrEmpty(extractedLog))
                                {
                                    recordDesc.AddField(fieldName, string.Empty, extractedLog);
                                }
                            }
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
                                DateTime utcTime = UtilityMethods.ConvertLocalToUTC(fldVal);
                                TraceManager.TraceInformation("Field [{0}], CQ Time [{1}], UTC Time [{2}]",
                                                              fieldName, fldVal.ToString(), utcTime.ToString());

                                recordDesc.AddField(fieldName, string.Empty, utcTime.ToString());
                            }
                            else
                            {
                                recordDesc.AddField(fieldName, string.Empty, string.Empty);
                            }
                        }
                        break;

                    case CQConstants.FIELD_REFERENCE:
                        {
                            // get the current entity def handle
                            OAdEntityDef curEntityDef = CQWrapper.GetEntityDef(CQSession, EntityDefName);
                            OAdEntityDef refEntityDef = CQWrapper.GetFieldReferenceEntityDef(curEntityDef, fieldName);
                            string refEntityName = CQWrapper.GetEntityDefName(refEntityDef);

                            if (CQWrapper.GetFieldValueStatus(fldInfo) == (int)CQConstants.FieldStatus.HAS_VALUE)
                            {
                                // single value required
                                string refFldVal = CQWrapper.GetFieldValue(fldInfo);
                                recordDesc.AddField(fieldName, string.Empty, refFldVal ?? string.Empty);
                            }
                            else
                            {
                                recordDesc.AddField(fieldName, string.Empty, string.Empty);
                            }
                        }
                        break;

                    case CQConstants.FIELD_REFERENCE_LIST:
                        {
                            // get the current entity def handle
                            OAdEntityDef curEntityDef = CQWrapper.GetEntityDef(CQSession, EntityDefName);
                            OAdEntityDef refEntityDef = CQWrapper.GetFieldReferenceEntityDef(curEntityDef, fieldName);
                            string refEntityName = CQWrapper.GetEntityDefName(refEntityDef);
                            
                            object[] refFldValues = CQWrapper.GetFieldValueAsList(fldInfo);
                            if (refFldValues != null)
                            {
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
                                recordDesc.AddField(fieldName, string.Empty, userList.ToString());
                            }
                            else
                            {
                                recordDesc.AddField(fieldName, string.Empty, string.Empty);
                            }
                        }
                        break;

                    case CQConstants.FIELD_ATTACHMENT_LIST:
                    case CQConstants.FIELD_JOURNAL:
                        TraceManager.TraceInformation("Skipping the Field migration for Internal Field Type '{0}'",
                                                      cqFieldType);
                        // not migrating these fields as they are CQ internal fields
                        continue;
                    default:
                        TraceManager.TraceInformation("Skipping the Field migration for Unkknown Field Type '{0}'",
                                                      cqFieldType);
                        break;
                } // end of switch cqFieldType

            } // end of foreach fieldNames


            return recordDesc.DescriptionDocument;
        }

        private OAdEntity CreateTestEntity(OAdEntity record, ClearQuestMigrationContext migrationContext)
        {
            string entityName = CQWrapper.GetEntityDefName(record);

            if (m_perEntityTypeTestEntities.ContainsKey(entityName))
            {
                return m_perEntityTypeTestEntities[entityName];
            }

            var aTestEntity = CQWrapper.BuildEntity(migrationContext.UserSession, entityName);
            m_perEntityTypeTestEntities.Add(entityName, aTestEntity);

            return aTestEntity;
        }

        internal static void FindLastRevDtls(OAdEntity record, out string lastAuthor, out DateTime lastChangeDate)
        {
            lastAuthor = string.Empty;
            lastChangeDate = DateTime.MinValue;

            OAdHistoryFields cqHistFields = CQWrapper.GetHistoryFields(record);
            int historyFldCount = CQWrapper.HistoryFieldsCount(cqHistFields);

            for (int histFldIndex = 0; histFldIndex < historyFldCount; histFldIndex++)
            {
                object ob = (object)histFldIndex;
                OAdHistoryField historyField = CQWrapper.HistoryFieldsItem(cqHistFields, ref ob);

                int historyCount = CQWrapper.HistoryFieldHistoriesCount(historyField);

                // pick the last history for each historyfield
                object obHistIndex = (object)(historyCount-1);
                OAdHistory aHistory = CQWrapper.HistoryFieldHistoriesItem(historyField, ref obHistIndex);

                CQHistory cqHistory = new CQHistory(aHistory);

                // CQ API returns local time
                DateTime changedDate = CQUtilityMethods.TryParseCQDate(cqHistory.Date);

                if (changedDate.CompareTo(lastChangeDate) > 0)
                {
                    // found a later change
                    lastChangeDate = changedDate;
                    lastAuthor = cqHistory.User;
                }
            }

            if (lastChangeDate.CompareTo(DateTime.MinValue) == 0)
            {
                lastChangeDate = DateTime.Now;
                lastAuthor = Environment.UserDomainName + "\\" + Environment.UserName;
            }
        }

        public XmlDocument CreateRecordHistoryDesc(
            OAdEntity record, 
            string historyValue, 
            string versionStr,
            bool isLastRevOfThisSyncCycle)
        {
            CQHistory cqHistory = new CQHistory(historyValue);

            ClearQuestRecordDescription recordHistDesc = new ClearQuestRecordDescription();
            recordHistDesc.CreateHeader(cqHistory.User, 
                                        DateTime.Parse(cqHistory.Date, CultureInfo.CurrentCulture), 
                                        MigrationRecordId, 
                                        EntityDefName,
                                        versionStr,
                                        isLastRevOfThisSyncCycle);

            recordHistDesc.AddField(CQConstants.HistoryFieldName, string.Empty, historyValue);
            return recordHistDesc.DescriptionDocument;
        }
    }
}
