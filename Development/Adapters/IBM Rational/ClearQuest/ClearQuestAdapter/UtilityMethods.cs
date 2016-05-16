// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal static class UtilityMethods
    {
        private static string MigrationItemDelimiter = ":";

        /// <summary>
        /// Utility method to do string.Format(CultureInfo.InvariantCulture) call
        /// </summary>
        /// <param name="format">The format string</param>
        /// <param name="parameters">The parameters</param>
        /// <returns>Formatted string</returns>
        public static string Format(string format, params object[] parameters)
        {
            Debug.Assert(!string.IsNullOrEmpty(format), "format is null");
            Debug.Assert(parameters != null, "parameters is null - no need to call Format()");

            return string.Format(CultureInfo.InvariantCulture, format, parameters);
        }

        /// <summary>
        /// Converts given time to UTC time
        /// </summary>
        /// <param name="localTime">Local time</param>
        /// <returns>Converter time in UTC</returns>
        public static DateTime ConvertLocalToUTC(DateTime localTime)
        {
            return TimeZone.CurrentTimeZone.ToUniversalTime(localTime);
        }

        public static string CreateCQRecordMigrationItemId(
            string entityDefName,
            string entityDispName)
        {
            return entityDefName + MigrationItemDelimiter + entityDispName;
        }

        public static string[] ParseCQRecordMigrationItemId(string recordMigrationItemId)
        {
            if (string.IsNullOrEmpty(recordMigrationItemId))
            {
                throw new ArgumentException(recordMigrationItemId, "recordMigrationItemId");
            }

            string[] splits = recordMigrationItemId.Split(new string[] { MigrationItemDelimiter }, 
                                                          StringSplitOptions.RemoveEmptyEntries);

            if (splits.Length < 2)
            {
                throw new ArgumentException(recordMigrationItemId, "recordMigrationItemId");
            }

            return splits;
        }

        public static bool HandleGeneralException(
            Exception ex, 
            ErrorManager errorManager,
            ConflictManager conflictManager)
        {
            if (null != errorManager)
            {
                errorManager.TryHandleException(ex, conflictManager);
                return false;
            }
            else
            {
                TraceManager.TraceException(ex);
                return false;
            }
        }

        public static bool HandleCQComCallException(
            ClearQuestCOMCallException cqComCallEx,
            ErrorManager errorManager,
            ConflictManager conflictManager)
        {
            Exception ex = (cqComCallEx.InnerException == null)
                           ? cqComCallEx
                           : cqComCallEx.InnerException;

            return HandleGeneralException(ex, errorManager, conflictManager);
        }


        public static bool HandleCOMDllNotFoundException(
            ClearQuestCOMDllNotFoundException cqComNotFoundEx,
            ErrorManager errorManager,
            ConflictManager conflictManager)
        {
            return HandleGeneralException(cqComNotFoundEx, errorManager, conflictManager);
        }


        internal static ConflictResolutionResult HandleInsufficientPriviledgeException(
            ClearQuestInsufficientPrivilegeException privEx,
            ConflictManager conflictManager)
        {
            if (null != conflictManager)
            {
                MigrationConflict conflict = ClearQuestInsufficentPrivilegeConflictType.CreateConflict(
                                                privEx.UserName, privEx.UserPrivilegeValue);
                List<MigrationAction> outActions;
                return conflictManager.TryResolveNewConflict(conflictManager.SourceId, conflict, out outActions);
            }
            else
            {
                TraceManager.TraceException(privEx);
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
        }

        internal static string ExtractRecordType(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "WorkItemType");
        }

        internal static string ExtractSourceWorkItemId(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "WorkItemID");
        }

        internal static string ExtractSourceWorkItemRevision(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "Revision");
        }

        internal static string ExtractTargetWorkItemId(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "TargetWorkItemID");
        }

        internal static string ExtractAuthor(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "Author");
        }

        internal static string ExtractChangeDate(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "ChangeDate");
        }

        internal static string ExtractTargetWorkItemRevision(IMigrationAction action)
        {
            return ExtractDocRootAttributeValue(action, "TargetRevision");
        }

        private static string ExtractDocRootAttributeValue(IMigrationAction action, string attrName)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlAttribute attrNode = action.MigrationActionDescription.DocumentElement.Attributes[attrName];
            return attrNode == null ? string.Empty : attrNode.Value;
        }

        internal static string ExtractAttachmentName(IMigrationAction action)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlElement rootNode = action.MigrationActionDescription.DocumentElement;
            XmlNode attachmentNode = rootNode.FirstChild;
            return attachmentNode.Attributes["Name"].Value;
        }

        internal static string ExtractAttachmentLength(IMigrationAction action)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlElement rootNode = action.MigrationActionDescription.DocumentElement;
            XmlNode attachmentNode = rootNode.FirstChild;
            return attachmentNode.Attributes["Length"].Value;
        }

        internal static string ExtractAttachmentComment(IMigrationAction action)
        {
            Debug.Assert(null != action.MigrationActionDescription.DocumentElement, "MigrationAction description is null");
            XmlElement rootNode = action.MigrationActionDescription.DocumentElement;
            XmlNode attachmentNode = rootNode.FirstChild;
            return attachmentNode.FirstChild.InnerText;
        }

        internal static XmlNode ExtractSingleFieldNodeFromMigrationDescription(
            XmlDocument migrationActionDescription,
            string fieldName)
        {
            return migrationActionDescription.SelectSingleNode(
                string.Format(@"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", fieldName));
        }

        internal static string ExtractSingleFieldValue(XmlNode fieldNode)
        {
            string retval = fieldNode.FirstChild.InnerText;
            if (!string.IsNullOrEmpty(retval))
            {
                if (retval.Equals(ClearQuestSetFieldValueConflictTypeDetails.NullValueString, StringComparison.OrdinalIgnoreCase))
                {
                    retval = null;
                }
            }

            return retval;
        }
    }
}
