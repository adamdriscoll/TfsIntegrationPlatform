// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop
{
    internal static class CQWrapper
    {
        #region Connection Methods
        internal static Session CreateSession()
        {
            Session cqSession = null;
            try
            {
                cqSession = new SessionClass();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            catch (IOException ex)
            {
                InteropErrorHandler.HandleIOException(ex);
            }
            return cqSession;
        }

        internal static AdminSession CreateAdminSession()
        {
            AdminSession cqAdminSession = null;
            try
            {
                cqAdminSession = new AdminSession();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            catch (IOException ex)
            {
                InteropErrorHandler.HandleIOException(ex);
            }
            return cqAdminSession;
        }

        #endregion

        #region Session Methods
        internal static OAdEntity BuildEntity(Session cqSession, string entitydef_name)
        {
            OAdEntity entity = null;
            try
            {
                entity = (OAdEntity)cqSession.BuildEntity(entitydef_name);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return entity;
        }

        internal static string DeleteEntity(Session cqSession, OAdEntity cqEntity, string deleteActionName)
        {
            string retVal = string.Empty;
            try
            {
                retVal = cqSession.DeleteEntity(cqEntity, deleteActionName);
            }
            catch (COMException ex)
            {
                retVal = ex.ToString();
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static OAdResultset BuildResultSet(Session cqSession, OAdQuerydef qryDef)
        {
            OAdResultset resultSet = null;
            try
            {
                resultSet = (OAdResultset)cqSession.BuildResultSet(qryDef);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return resultSet;
        }

        internal static OAdResultset BuildResultSet(Session cqSession, string sqlQry)
        {
            OAdResultset resultSet = null;
            try
            {
                resultSet = (OAdResultset)cqSession.BuildSQLQuery(sqlQry);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return resultSet;
        }

        internal static void MarkEntityAsDuplicate(
            Session cqSession,
            OAdEntity childRecord,
            OAdEntity hostRecord,
            string dupActionName)
        {
            try
            {
                cqSession.MarkEntityAsDuplicate(childRecord, hostRecord, dupActionName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static void UnmarkEntityAsDuplicate(
            Session cqSession,
            OAdEntity childRecord,
            string dupActionName)
        {
            try
            {
                cqSession.UnmarkEntityAsDuplicate(childRecord, dupActionName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static OAdEntityDef GetEntityDef(Session cqSession, string entityDefName)
        {
            OAdEntityDef entityDef = null;
            try
            {
                entityDef = (OAdEntityDef)cqSession.GetEntityDef(entityDefName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return entityDef;
        }

        internal static OAdEntity GetEntityByDbId(Session cqSession, string entityName, int dbid)
        {
            OAdEntity entity = null;
            try
            {
                entity = (OAdEntity)cqSession.GetEntityByDbId(entityName, dbid);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return entity;
        }

        internal static OAdEntity GetEntity(Session cqSession, string entityDefName, string displayName)
        {
            OAdEntity entity = null;
            try
            {
                /* Uncomment for debugging
                TraceManager.TraceInformation("ClearQuest Adapter: Calling GetEntity on session '{0}' with entityDefName='{1}', displayName='{2}'", 
                    cqSession.ToString(), entityDefName, displayName); 
                */
                entity = (OAdEntity)cqSession.GetEntity(entityDefName, displayName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return entity;
        }

        internal static object GetSubmitEntityDefNames(Session cqSession)
        {
            object returnval = null;
            try
            {
                returnval = cqSession.GetSubmitEntityDefNames();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return returnval;
        }

        internal static string GetFieldValue(OAdFieldInfo fldInfo)
        {
            string returnval = null;
            try
            {
                returnval = fldInfo.GetValue();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return returnval;
        }

        internal static int GetFieldValueStatus(OAdFieldInfo fldInfo)
        {
            int returnval = 0;
            try
            {
                returnval = fldInfo.GetValueStatus();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return returnval;
        }

        internal static object[] GetFieldValueAsList(OAdFieldInfo fldInfo)
        {
            object[] returnval = null;
            try
            {
                returnval = (object[])fldInfo.GetValueAsList();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return returnval;
        }

        internal static int GetRequiredness(OAdFieldInfo fldInfo)
        {
            int retVal = int.MinValue;
            try
            {
                retVal = fldInfo.GetRequiredness();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static IEnumerable<string> GetQueryList(WORKSPACE ws, short queryType)
        {
            try
            {
                var queryList = (object[])ws.GetQueryList(queryType);
                if (queryList == null)
                {
                    return new List<string>(0);
                }
                else
                {
                    return queryList.Cast<string>();
                }
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return null;
        }

        internal static IEnumerable<IOAdDatabaseDesc> GetAccessibleDatabases(Session session, string master_db_name, string user_login_name, string db_set_name)
        {
            try
            {
                var databaseList = (object[])session.GetAccessibleDatabases(master_db_name, user_login_name, db_set_name);
                return databaseList.Cast<IOAdDatabaseDesc>();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return null;
        }

        internal static WORKSPACE GetWorkSpace(Session cqSession)
        {
            WORKSPACE returnval = null;
            try
            {
                returnval = (WORKSPACE)cqSession.GetWorkSpace();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return returnval;
        }

        internal static void UserLogon(
            Session cqSession,
            string username,
            string pwd,
            string userdb,
            int sessiontype,
            string dbset)
        {
            try
            {
                cqSession.UserLogon(username, pwd, userdb, sessiontype, dbset);
            }
            catch (COMException ex)
            {
                // Failed to login to UserDB '{0}' of DBSet '{1}' with Session Type '{2}' as User '{3}'.
                throw new ClearQuestLoginException(
                    string.Format(ClearQuestResource.ClearQuest_Error_LoginFailure, userdb, dbset, sessiontype.ToString(), username),
                    ex);
            }
        }

        internal static bool HasUserPrivilege(
            Session cqSession,
            int userPrivilegeMaskTypeFlag)
        {
            try
            {
                return cqSession.HasUserPrivilege(userPrivilegeMaskTypeFlag);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }

            return false;
        }

        internal static string GetUserLoginName(
            Session cqSession)
        {
            try
            {
                return cqSession.GetUserLoginName();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }

            return string.Empty;
        }

        internal static object[] GetAllUsers(
            Session cqSession,
            int extend_option)
        {
            object[] returnval = null;
            try
            {
                returnval = (object[])cqSession.GetAllUsers(extend_option);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return returnval;
        }

        #endregion

        #region AdminSession Methods
        internal static OAdUser GetUser(AdminSession cqAdminSession, string userName)
        {
            OAdUser user = null;
            try
            {
                user = (OAdUser)cqAdminSession.GetUser(userName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return user;
        }

        internal static OAdUsers GetUsers(AdminSession cqAdminSession)
        {
            OAdUsers users = null;
            try
            {
                users = (OAdUsers)cqAdminSession.Users;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return users;
        }

        internal static void AdminLogon(
            AdminSession cqAdminSession,
            string user,
            string pwd,
            string dbset)
        {
            try
            {
                cqAdminSession.Logon(user, pwd, dbset);
            }
            catch (COMException ex)
            {
                // TODO [teyang]: error handling

                //// for Login credentials throw the exact error as received from COM interface
                //string errMsg = UtilityMethods.Format(CQResource.CQ_CONNECTION_ERROR, ex.Message, configFile);

                //Logger.WriteException(LogSource.CQ, ex);
                //Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                //ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty /* no item */,
                //    null, "Config", ReportIssueType.Critical);

                //throw new ConverterException(errMsg, ex);
                TraceManager.TraceException(ex);
            }
        }
        #endregion

        #region Entity Methods
        internal static OAdAttachmentFields GetAttachmentFields(OAdEntity cqEntity)
        {
            OAdAttachmentFields attFields = null;
            try
            {
                attFields = (OAdAttachmentFields)cqEntity.AttachmentFields;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return attFields;
        }

        internal static OAdHistoryFields GetHistoryFields(OAdEntity cqEntity)
        {
            OAdHistoryFields histFields = null;
            try
            {
                histFields = (OAdHistoryFields)cqEntity.HistoryFields;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return histFields;
        }

        internal static string GetEntityDisplayName(OAdEntity cqEntity)
        {
            string dispName = null;
            try
            {
                dispName = cqEntity.GetDisplayName();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return dispName;
        }

        internal static string GetEntityDefName(OAdEntity cqEntity)
        {
            string entityDefName = null;
            try
            {
                entityDefName = cqEntity.GetEntityDefName();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return entityDefName;
        }

        internal static object GetFieldChoiceList(OAdEntity cqEntity, string fldName)
        {
            object choices = null;
            try
            {
                choices = cqEntity.GetFieldChoiceList(fldName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return choices;
        }

        internal static int GetFieldChoiceType(OAdEntity cqEntity, string fldName)
        {
            int choice = 0;
            try
            {
                choice = cqEntity.GetFieldChoiceType(fldName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return choice;
        }

        internal static object GetEntityFieldNames(OAdEntity cqEntity)
        {
            object fldNames = null;
            try
            {
                fldNames = cqEntity.GetFieldNames();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return fldNames;
        }

        internal static int GetEntityFieldRequiredness(OAdEntity cqEntity, string fldName)
        {
            int retval = 0;
            try
            {
                retval = cqEntity.GetFieldRequiredness(fldName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdFieldInfo GetEntityFieldValue(OAdEntity cqEntity, string fldName)
        {
            OAdFieldInfo retval = null;
            try
            {
                retval = (OAdFieldInfo)cqEntity.GetFieldValue(fldName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdEntity GetOriginalEntity(OAdEntity cqEntity)
        {
            OAdEntity retval = null;
            try
            {
                retval = (OAdEntity)cqEntity.GetOriginal();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string GetOriginalEntityId(OAdEntity cqEntity)
        {
            string retval = null;
            try
            {
                retval = cqEntity.GetOriginalId();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static bool IsDuplicateEntity(OAdEntity cqEntity)
        {
            bool retval = false;
            try
            {
                retval = cqEntity.IsDuplicate();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static bool HasDuplicates(OAdEntity cqEntity)
        {
            bool retval = false;
            try
            {
                retval = cqEntity.HasDuplicates();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static object GetDuplicates(OAdEntity cqEntity)
        {
            object retval = null;
            try
            {
                retval = cqEntity.GetDuplicates();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int GetEntityDbId(OAdEntity cqEntity)
        {
            int retVal = 0;
            try
            {
                retVal = cqEntity.GetDbId();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static void EditEntity(Session cqSession, OAdEntity cqEntity, string actionName)
        {
            try
            {
                cqSession.EditEntity(cqEntity, actionName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static string SetFieldValue(OAdEntity cqEntity, string fieldName, object newValue)
        {
            string retVal = string.Empty;
            try
            {
                retVal = cqEntity.SetFieldValue(fieldName, newValue);
            }
            catch (COMException ex)
            {
                retVal = ex.ToString();
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static string AddFieldValue(OAdEntity cqEntity, string fieldName, object newValue)
        {
            string retVal = string.Empty;
            try
            {
                retVal = cqEntity.AddFieldValue(fieldName, newValue);
            }
            catch (COMException ex)
            {
                retVal = ex.ToString();
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static string Validate(OAdEntity cqEntity)
        {
            string retVal = string.Empty;
            try
            {
                retVal = cqEntity.Validate();
            }
            catch (COMException ex)
            {
                retVal = ex.ToString();
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static string Commmit(OAdEntity cqEntity)
        {
            string retVal = string.Empty;
            try
            {
                retVal = ((IOAdEntity)cqEntity).Commit();
            }
            catch (COMException ex)
            {
                retVal = ex.ToString();
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static void Revert(OAdEntity cqEntity)
        {
            try
            {
                cqEntity.Revert();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static string AddAttachmentFieldValue(OAdEntity cqEntity, string attFieldName, string fileName, string description)
        {
            string retVal = string.Empty;
            try
            {
                retVal = cqEntity.AddAttachmentFieldValue(attFieldName, fileName, description);
            }
            catch (COMException ex)
            {
                retVal = ex.ToString();
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }
        #endregion

        #region Attachment Related Methods
        internal static int AttachmentsFieldsCount(OAdAttachmentFields attFields)
        {
            int retval = 0;
            try
            {
                retval = attFields.Count;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdAttachmentField AttachmentsFieldsItem(OAdAttachmentFields attFields, ref object item)
        {
            OAdAttachmentField retval = null;
            try
            {
                retval = (OAdAttachmentField)attFields.item(ref item);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string GetAttachmentFieldName(OAdAttachmentField attField)
        {
            string retVal = string.Empty;
            try
            {
                retVal = attField.fieldname;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static OAdAttachments GetAttachments(OAdAttachmentField attField)
        {
            OAdAttachments retval = null;
            try
            {
                retval = (OAdAttachments)attField.Attachments;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int AttachmentsCount(OAdAttachments attchments)
        {
            int retval = 0;
            try
            {
                retval = attchments.Count;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdAttachment AttachmentsItem(OAdAttachments attchments, ref object item)
        {
            OAdAttachment retval = null;
            try
            {
                retval = (OAdAttachment)attchments.item(ref item);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static void LoadAttachment(OAdAttachment attachment, string file)
        {
            try
            {
                attachment.Load(file);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static void GetAttachmentMetadata(OAdAttachment attachment,
            out string fileName,
            out string description,
            out string displayName,
            out int fileSize)
        {
            fileName = null;
            description = null;
            displayName = null;
            fileSize = 0;
            try
            {
                fileName = attachment.filename;
                description = attachment.Description;
                displayName = attachment.DisplayName;
                fileSize = attachment.FileSize;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        #endregion

        #region History Related Methods
        internal static int HistoryFieldsCount(OAdHistoryFields histFields)
        {
            int retval = 0;
            try
            {
                retval = histFields.Count;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdHistoryField HistoryFieldsItem(OAdHistoryFields histFields, ref object item)
        {
            OAdHistoryField retval = null;
            try
            {
                retval = (OAdHistoryField)histFields.item(ref item);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int HistoryFieldHistoriesCount(OAdHistoryField histField)
        {
            int retval = 0;
            try
            {
                retval = ((OAdHistories)histField.Histories).Count;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdHistory HistoryFieldHistoriesItem(OAdHistoryField histField, ref object item)
        {
            OAdHistory retval = null;
            try
            {
                retval = (OAdHistory)((OAdHistories)histField.Histories).item(ref item);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string GetHistoryFieldName(OAdHistoryField histField)
        {
            string retval = string.Empty;
            try
            {
                retval = histField.fieldname;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string HistoryValue(OAdHistory hist)
        {
            string retval = null;
            try
            {
                retval = hist.value;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }
        #endregion

        #region EntityDef Methods
        internal static object DoesTransitionExist(OAdEntityDef entityDef, string srcState, string destState)
        {
            object retval = null;
            try
            {
                retval = entityDef.DoesTransitionExist(srcState, destState);
                if (retval == null)
                {
                    throw new ClearQuestInvalidConfigurationException(String.Format(
                        "Unable to change State for ClearQuest '{0}'; transaction from State '{1}' to '{2}' is not allowed.  Consider changing the allowed States and State transitions on both sides of the sync to match.", 
                        entityDef.GetName(), srcState, destState));
                }
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static object GetActionDefNames(OAdEntityDef entityDef)
        {
            object retval = null;
            try
            {
                retval = entityDef.GetActionDefNames();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int GetActionDefType(OAdEntityDef entityDef, string actionDefName)
        {
            int retval = 0;
            try
            {
                retval = entityDef.GetActionDefType(actionDefName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string GetActionDestStateName(OAdEntityDef entityDef, string actionDefName)
        {
            string retval = null;
            try
            {
                retval = entityDef.GetActionDestStateName(actionDefName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static object GetFieldDefNames(OAdEntityDef entityDef)
        {
            object retval = null;
            try
            {
                retval = entityDef.GetFieldDefNames();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int GetFieldDefType(OAdEntityDef entityDef, string fieldDefName)
        {
            int retval = 0;
            try
            {
                retval = entityDef.GetFieldDefType(fieldDefName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static OAdEntityDef GetFieldReferenceEntityDef(OAdEntityDef entityDef, string fieldName)
        {
            OAdEntityDef retval = null;
            try
            {
                retval = (OAdEntityDef)entityDef.GetFieldReferenceEntityDef(fieldName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string GetEntityDefName(OAdEntityDef entityDef)
        {
            string retval = null;
            try
            {
                retval = entityDef.GetName();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static object GetStateDefNames(OAdEntityDef entityDef)
        {
            object retval = null;
            try
            {
                retval = entityDef.GetStateDefNames();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int GetEntityDefType(OAdEntityDef entityDef)
        {
            int retval = 0;
            try
            {
                retval = entityDef.GetType();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        #endregion

        #region Workspace Methods
        internal static OAdQuerydef GetQueryDef(WORKSPACE ws, string queryName)
        {
            OAdQuerydef retval = null;
            try
            {
                retval = (OAdQuerydef)ws.GetQueryDef(queryName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        #endregion

        #region FieldInfo Methods
        internal static int GetFieldType(OAdFieldInfo fldInfo)
        {
            int retval = 0;
            try
            {
                retval = fldInfo.GetType();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        #endregion

        #region QueryDef Methods
        internal static void BuildField(OAdQuerydef queryDef, string fieldName)
        {
            try
            {
                queryDef.BuildField(fieldName);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static IOAdQueryFilterNode CreateTopNode(OAdQuerydef queryDef, int bool_op)
        {
            IOAdQueryFilterNode retVal = null;
            try
            {
                retVal = queryDef.CreateTopNode(bool_op) as IOAdQueryFilterNode;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retVal;
        }

        internal static string GetPrimaryEntityDefName(OAdQuerydef queryDef)
        {
            string retval = null;
            try
            {
                retval = queryDef.GetPrimaryEntityDefName();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        #endregion

        #region ResultSet Methods
        internal static void EnableRecordCount(OAdResultset resultSet)
        {
            try
            {
                resultSet.EnableRecordCount();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static void ExecuteResultSet(OAdResultset resultSet)
        {
            try
            {
                resultSet.Execute();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
        }

        internal static int ResultSetMoveNext(OAdResultset resultSet)
        {
            int retval = 0;
            try
            {
                retval = resultSet.MoveNext();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int GetResultSetColumnCount(OAdResultset resultSet)
        {
            int retval = 0;
            try
            {
                retval = resultSet.GetNumberOfColumns();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static int GetRecordCount(OAdResultset resultSet)
        {
            int retval = 0;
            try
            {
                retval = resultSet.RecordCount;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static string GetColumnLabel(OAdResultset resultSet, int index)
        {
            string retval = null;
            try
            {
                retval = resultSet.GetColumnLabel(index);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }

        internal static object GetColumnValue(OAdResultset resultSet, int index)
        {
            object retval = null;
            try
            {
                retval = resultSet.GetColumnValue(index);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }
        #endregion

        #region Link Methods
        internal static object GetChildEntity(OAdLink link)
        {
            object retval = null;
            try
            {
                retval = link.GetChildEntity();
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return retval;
        }
        #endregion

        #region User Methods
        internal static OAdUser GetUser(OAdUsers users, ref object userObj)
        {
            OAdUser aUser = null;
            try
            {
                aUser = (OAdUser)users.item(ref userObj);
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return aUser;
        }

        internal static bool IsSuperUser(OAdUser user)
        {
            try
            {
                return user.SuperUser;
            }
            catch (COMException ex)
            {
                InteropErrorHandler.HandleCQException(ex);
            }
            return false;
        }
        #endregion
    }
}
