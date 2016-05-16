// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Contains the constants referenced in ClearQuest schema

#region Using directives

using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using WIFieldDefinition = Microsoft.TeamFoundation.WorkItemTracking.Client.FieldDefinition;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;

#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    internal static class CQConstants
    {
        // Mapping of Clear Quest Field types with that of Currituck
        public static readonly FieldType[] WITFieldTypes =
                        new FieldType[MAX_FIELD_TYPES + 1] {
                                    FieldType.HTML,     // Unused
                                    FieldType.String,   // Short String
                                    FieldType.PlainText,// Multiline string
                                    FieldType.Integer,  // int
                                    FieldType.DateTime, // date time
                                    FieldType.String,   // REFERENCE
                                    FieldType.String,   // REFERENCE_LIST
                                    FieldType.HTML,     // ATTACHMENT_LIST
                                    FieldType.String,   // ID
                                    FieldType.HTML,     // STATE
                                    FieldType.HTML,     // JOURNAL
                                    FieldType.HTML,     // DBID
                                    FieldType.HTML,     // STATETYPE
                                    FieldType.HTML      // RECORDTYPE
                                    };

        // list of CQ Internal fields. skipped for migration
        public static Dictionary<string, bool> m_internalFieldTypes;
        public static Dictionary<string, bool> InternalFieldTypes
        {
            get
            {
                if (m_internalFieldTypes == null)
                {
                    string[] internalFields = {
                        "id",
                        "old_id",
                        "old_internal_id",
                        "entitydb_id",
                        "entity_fielddefid",
                        "entitydef_name",
                        "entitydef_id",
                        "Resolution_Statetype",
                        "Note_Entry",
                        "dbid",
                        "is_active",
                        "version",
                        "lock_version",
                        "locked_by",
                        "ratl_mastership",
                        "history",
                        "ratl_keysite",
                        "is_duplicate",
                        "unduplicate_state",
                        "record_type"
                    };

                    m_internalFieldTypes = new Dictionary<string, bool>(TFStringComparer.Ordinal);
                    foreach (string field in internalFields)
                    {
                        m_internalFieldTypes.Add(field, false);
                    }
                }

                return m_internalFieldTypes;
            }
        }

        // list of ref names used in the constants
        internal const string DescriptionField = "System.Description";
        internal const string ConversationField = "System.Conversation";
        internal const string CreatedByField = "System.CreatedBy";
        internal const string CreatedDateField = "System.CreatedDate";
        internal const string ChangedByField = "System.ChangedBy";
        internal const string ChangedDateField = "System.ChangedDate";
        internal const string AssignedToField = "System.AssignedTo";
        internal const string IdField = "System.Id";
        internal const string StateField = "System.State";
        internal const string ReasonField = "System.Reason";
        internal const string TitleField = "System.Title";
        internal const string HistoryField = "System.History";


        // list of fields present in Fixed Form.. skipped for generating them in WITD Form section
        public static Dictionary<string, bool> m_fixedFormFields;
        public static Dictionary<string, bool> FixedFormFields
        {
            get
            {
                if (m_fixedFormFields == null)
                {
                    string[] fixedFormFields = {
                        DescriptionField,
                        ConversationField,
                        CreatedByField,
                        CreatedDateField,
                        ChangedByField,
                        ChangedDateField
                    };

                    m_fixedFormFields = new Dictionary<string, bool>(TFStringComparer.WIConverterFieldRefName);
                    foreach (string field in fixedFormFields)
                    {
                        m_fixedFormFields.Add(field, false);
                    }
                }

                return m_fixedFormFields;
            }
        }

        private static Dictionary<string, bool> m_CQInternalFields;
        public static Dictionary<string, bool> CQInternalFields
        {
            get
            {
                if (m_CQInternalFields == null)
                {
                    m_CQInternalFields = new Dictionary<string, bool>(TFStringComparer.WorkItemFieldFriendlyName);
                    m_CQInternalFields.Add(CommonConstants.VSTSSrcIdField, false);
                    m_CQInternalFields.Add(CommonConstants.VSTSSrcDbField, false);
                    m_CQInternalFields.Add(IdFieldName, false);
                    m_CQInternalFields.Add(VstsConn.store.FieldDefinitions[StateField].Name, false);
                    m_CQInternalFields.Add(ReasonFieldName, false);
                }

                return m_CQInternalFields;
            }
        }

        private static string m_idFieldName;
        internal static string IdFieldName
        {
            get
            {
                if (m_idFieldName == null)
                {
                    m_idFieldName = VstsConn.store.FieldDefinitions[IdField].Name;
                }

                return m_idFieldName;
            }
        }

        private static string m_reasonFieldName;
        internal static string ReasonFieldName
        {
            get
            {
                if (m_reasonFieldName == null)
                {
                    m_reasonFieldName = VstsConn.store.FieldDefinitions[ReasonField].Name;
                }

                return m_reasonFieldName;
            }
        }

        private static WIFieldDefinition[] m_currituckCoreFields;
        public static WIFieldDefinition[] CurrituckCoreFields
        {
            get
            {
                if (m_currituckCoreFields == null)
                {
                    string[] currituckCoreFieldRefNames = {
                        AssignedToField,
                        ChangedByField,
                        CreatedDateField,
                        ChangedDateField,
                        DescriptionField,
                        TitleField,
                        StateField,
                        HistoryField
                    };

                    m_currituckCoreFields = new WIFieldDefinition[currituckCoreFieldRefNames.Length];
                    for (int index = 0; index < currituckCoreFieldRefNames.Length; index++)
                    {
                        m_currituckCoreFields[index] = VstsConn.store.FieldDefinitions[currituckCoreFieldRefNames[index]];
                    }
                }

                return m_currituckCoreFields;
            }
        }

        // no of user fields.. will be added along with field map referreing to UserMap
        // and default value to current user
        public static int NoOfUserFldsInCoreFields = 2;

        private static Hashtable m_suggestedMap;
        public static Hashtable SuggestedMap
        {
            get
            {
                if (m_suggestedMap == null)
                {
                    // populate suggested map
                    m_suggestedMap = new Hashtable(TFStringComparer.WIConverterFieldRefName);
                    m_suggestedMap.Add("Headline", VstsConn.store.FieldDefinitions["System.Title"].Name);
                    m_suggestedMap.Add("Description", VstsConn.store.FieldDefinitions["System.Description"].Name);
                    m_suggestedMap.Add("Submit_Date", VstsConn.store.FieldDefinitions["System.CreatedDate"].Name);
                    m_suggestedMap.Add("Owner", VstsConn.store.FieldDefinitions["System.AssignedTo"].Name);
                    m_suggestedMap.Add("State", VstsConn.store.FieldDefinitions["System.State"].Name);
                }

                return m_suggestedMap;
            }
        }

        // predefined internal field map.. imposed on evey schema during migrate
        public static Dictionary<string, string> m_internalMap;
        public static Dictionary<string, string> InternalMap
        {
            get
            {
                if (m_internalMap == null)
                {
                    // populate internal map
                    m_internalMap = new Dictionary<string, string>(TFStringComparer.WIConverterFieldRefName);
                    m_internalMap.Add("user_name", VstsConn.store.FieldDefinitions["System.ChangedBy"].Name);
                    m_internalMap.Add(CommonConstants.VSTSSrcIdField, CommonConstants.VSTSSrcIdField);
                    m_internalMap.Add(CommonConstants.VSTSSrcDbField, CommonConstants.VSTSSrcDbField);
                    m_internalMap.Add("action_timestamp", VstsConn.store.FieldDefinitions["System.ChangedDate"].Name);
                    m_internalMap.Add("Migration Status", "Migration Status");
                    m_internalMap.Add("State", VstsConn.store.FieldDefinitions["System.State"].Name);
                    m_internalMap.Add("History", VstsConn.store.FieldDefinitions["System.History"].Name);
                    m_internalMap.Add("Reason", VstsConn.store.FieldDefinitions["System.Reason"].Name);
                }

                return m_internalMap;
            }
        }

        // no of user fields.. will be added along with field map referreing to UserMap
        // and default value to current user
        public static int NoOfUserFldsInInternalMap = 1;

        public const int FIELD_SHORT_STRING = 1;
        public const int FIELD_MULTILINE_STRING = 2;
        public const int FIELD_INT = 3;
        public const int FIELD_DATE_TIME = 4;
        public const int FIELD_REFERENCE = 5;
        public const int FIELD_REFERENCE_LIST = 6;
        public const int FIELD_ATTACHMENT_LIST = 7;
        public const int FIELD_ID = 8;
        public const int FIELD_STATE = 9;
        public const int FIELD_JOURNAL = 10;
        public const int FIELD_DBID = 11;
        public const int FIELD_STATETYPE = 12;
        public const int FIELD_RECORDTYPE = 13;
        public const int MAX_FIELD_TYPES = 13;

        // action type constants
        public const int ACTION_SUBMIT = 1;
        public const int ACTION_MODIFY = 2;
        public const int ACTION_CHANGE = 3;
        public const int ACTION_DUPLICATE = 4;
        public const int ACTION_UNDUPLICATE = 5;
        public const int ACTION_IMPORT = 6;
        public const int ACTION_DELETE = 7;
        public const int ACTION_BASE = 8;
        public const int ACTION_RECORD_SCRIPT_ALIAS = 9;
        public const int MAX_ACTIONS = 10; // total 9 actions.. 1..9

        // field requiredness constants
        public const int MANDATORY = 1;
        public const int OPTIONAL = 2;
        public const int READONLY = 3;
        public const int USEHOOK = 4;

        // field choice type constants
        public const int CLOSED_CHOICE = 1;
        public const int OPEN_CHOICE = 2;

        // record type constant
        public const int STATE_BASED = 1;
        public const int STATE_LESS = 2;
        public const int STATE_OR_STATELESS = 3;

        // data fetch status
        public const int SUCCESS = 1;
        public const int NO_DATA = 2;
        public const int MAX_ROWS_EXCEEDED = 3;

        // session connection type
        public enum SessionType
        {
            SHARED = 1,
            PRIVATE,
            ADMIN,
            SHARED_METADATA
        };

        // status of the field
        public enum FieldStatus
        {
            HAS_NO_VALUE = 1,   // The field has no value set. 
            HAS_VALUE,          // The field has a value. 
            VALUE_NOT_AVAILABLE // The current state of the field prevents it from returning a value. 
        };

        // fixed file names
        public const string SchemaMapFile = "SchemaMap.xml";
        public const string FieldMapFileSuffix = "FieldMap.xml";
        public const string UserMapFile = "UserMap.xml";
        public const string LinkMapFile = "LinkMap.xml";

        // value which has to be put into the refer section for user fields
        public const string UserMapXMLValue = "UserMap";

        public const string SourceFieldLabel = "ClearQuest ID";
        private static string m_attachmentDir;
        internal static string AttachmentsDir
        {
            get
            {
                if (m_attachmentDir == null)
                {
                    m_attachmentDir = Path.Combine(CommonConstants.TempPath, "CQConverter");
                }
                return m_attachmentDir;
            }
        }

        internal static VSTSConnection VstsConn;
    }
}