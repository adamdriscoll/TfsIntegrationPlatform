// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Clear Quest API Wrapper class. 
// All the CQ calls should be routed through this wrapper

#region Using directives

using System;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using System.Runtime.InteropServices; // for COMException
using System.Diagnostics;
using Microsoft.TeamFoundation.Converters.Reporting;
using System.IO;
#endregion

/*
 * This class defines wrapper methods for all CQ calls to cqold.dll
 * Any new calls required int he CQConverter code shall be added here and 
 * called using CQWrapper interface. Similar exception handling shall
 * also be added
 */
namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
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
                HandleCQException(ex);
            }
            catch (IOException ex)
            {
                HandleIOException(ex);
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
                HandleCQException(ex);
            }
            catch (IOException ex)
            {
                HandleIOException(ex);
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
                HandleCQException(ex);
            }
            return entity;
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
                HandleCQException(ex);
            }
            return resultSet;
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
            return entity;
        }

        internal static OAdEntity GetEntity(Session cqSession, string entityName, string displayName)
        {
            OAdEntity entity = null;
            try
            {
                entity = (OAdEntity)cqSession.GetEntity(entityName, displayName);
            }
            catch (COMException ex)
            {
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
            return returnval;
        }

        internal static object[] GetFieldValueAsList(OAdFieldInfo fldInfo)
        {
            object[] returnval = null;
            try
            {
                returnval = (object[]) fldInfo.GetValueAsList();
            }
            catch (COMException ex)
            {
                HandleCQException(ex);
            }
            return returnval;
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
                HandleCQException(ex);
            }
            return returnval;
        }

        internal static void UserLogon(Session cqSession, string username, string pwd, string userdb, int sessiontype, string dbset, string configFile)
        {
            try
            {
                cqSession.UserLogon(username, pwd, userdb, sessiontype, dbset);
            }
            catch (COMException ex)
            {
                // for Login credentials throw the exact error as received from COM interface
                string errMsg = UtilityMethods.Format(CQResource.CQ_CONNECTION_ERROR, ex.Message, configFile);

                Logger.WriteException(LogSource.CQ, ex);
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty /* no item */,
                    null, "Config", ReportIssueType.Critical);

                throw new ConverterException(errMsg, ex);
            }
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
            return users;
        }

        internal static void AdminLogon(AdminSession cqAdminSession, string user, string pwd, string dbset, string configFile)
        {
            try
            {
                cqAdminSession.Logon(user, pwd, dbset);
            }
            catch (COMException ex)
            {
                // for Login credentials throw the exact error as received from COM interface
                string errMsg = UtilityMethods.Format(CQResource.CQ_CONNECTION_ERROR, ex.Message, configFile);

                Logger.WriteException(LogSource.CQ, ex);
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty /* no item */,
                    null, "Config", ReportIssueType.Critical);

                throw new ConverterException(errMsg, ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
            return retval;
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
        }

        internal static void GetAttachmentFileNameAndDescription(OAdAttachment attachment, 
            out string fileName, out string description)
        {
            fileName = null;
            description = null;
            try
            {
                fileName = attachment.filename;
                description = attachment.Description;
            }
            catch (COMException ex)
            {
                HandleCQException(ex);
            }
        }

        internal static int AttachmentFileSize(OAdAttachment attachment)
        {
            int size = 0;
            try
            {
                size = attachment.FileSize;
            }
            catch (COMException ex)
            {
                HandleCQException(ex);
            }
            return size;
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
            }
            catch (COMException ex)
            {
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
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
                HandleCQException(ex);
            }
            return false;
        }
        #endregion
        
        /// <summary>
        /// Generic handler for all CQ calls.
        /// Except for user logon and admin logon case.
        /// </summary>
        /// <param name="cEx"></param>
        private static void HandleCQException(COMException cEx)
        {
            string errMsg = UtilityMethods.Format(CQResource.CQ_COM_ERROR, cEx.Message);
            Logger.WriteException(LogSource.CQ, cEx);
            throw new ConverterException(errMsg, cEx);
        }
        
        // if the COM dll is not found by .NET, it throws IOException
        private static void HandleIOException(IOException cEx)
        {
            string errMsg = UtilityMethods.Format(CQResource.CQ_COM_ERROR, cEx.Message);
            Logger.WriteException(LogSource.CQ, cEx);
            throw new ConverterException(errMsg, cEx);
        }
    }
}
