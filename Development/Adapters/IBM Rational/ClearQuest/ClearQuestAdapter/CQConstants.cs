// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal static class CQConstants
    {
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

        // CQ-specific field name
        public const string HistoryFieldName = "history";

        // CQ _master_db_name
        public const string MasterDBName = "MASTR";

        public enum UserPrivilegeMaskType
        {
            _DYNAMIC_LIST_ADMIN = 1,    // Can create and manage dynamic lists. 
            _PUBLIC_FOLDER_ADMIN = 2,   // Can create / delete / read / write public folders used for queries, reports, and charts. 
            _SECURITY_ADMIN = 3,        // Can access and manage secure records and fields. 
                                        //   Also can edit the context group list field for a security context record and view all records. 
            _RAW_SQL_WRITER = 4,        // Can create, edit, and use a SQL query using a raw SQL string. 
            _ALL_USERS_VISIBLE = 5,     // Can view information for all users and groups from user databases. 
            _MULTI_SITE_ADMIN = 6,      // MultiSite administrator privilege. 
            _SUPER_USER = 7,            // Can perform all Active User, Schema Designer, User Administrator, Security Administrator, 
                                        //   Public Folder Administrator, Dynamic List Administrator, and SQL Editor tasks. 
                                        //   Can also create and delete databases and schemas and edit Rational ClearQuest Web settings. 
                                        //   The admin user account has Super User privilege. 
                                        //   *Note: This value became available in version 2002.05.00. 
            _APP_BUILDER = 8,           // Can create and modify schemas. Add record types, define and modify fields, create and modify 
                                        //   states and actions, add hooks to the schema, and update existing databases. Create, modify, 
                                        //   and save public queries, charts, and reports. Cannot perform User Administrator tasks. 
                                        //   *Note: This value became available in version 2002.05.00. 
            _USER_ADMIN = 9,            // Can create users and user groups and assign and modify their user-access privileges. 
                                        //   *Note: This value became available in version 2002.05.00. 

        }

        public enum ExtendedNameOption
        {
            _NAME_NOT_EXTENDED = 1,         // Not_Extended 
            _NAME_EXTENDED = 2,             // Extended 
            _NAME_EXTEND_WHEN_NEEDED = 3,   // Extend_When_Needed 
        }

        public enum BoolOp : int
        {
            _BOOL_OP_AND = 1,   // Boolean AND operator 
            _BOOL_OP_OR = 2,    // Boolean OR operator 
        }

        public enum CompOp : int
        {
            _COMP_OP_EQ = 1,    // Equality operator (=) 
            _COMP_OP_NEQ = 2,   // Inequality operator (<>) 
            _COMP_OP_LT = 3,    // Less-than operator (<) 
            _COMP_OP_LTE = 4,   // Less-than or Equal operator (<=) 
            _COMP_OP_GT = 5,    // Greater-than operator (>) 
            _COMP_OP_GTE = 6,   // Greater-than or Equal operator (>=) 
            _COMP_OP_LIKE = 7,  // Like operator (value is a substring of the string in the given field) 
            _COMP_OP_NOT_LIKE = 8,      // Not-like operator (value is not a substring of the string in the given field) 
            _COMP_OP_BETWEEN = 9,       // Between operator (value is between the specified delimiter values) 
            _COMP_OP_NOT_BETWEEN = 10,  // Not-between operator (value is not between specified delimiter values) 
            _COMP_OP_IS_NULL = 11,      // Is-NULL operator (field does not contain a value) 
            _COMP_OP_IS_NOT_NULL = 12,  // Is-not-NULL operator (field contains a value) 
            _COMP_OP_IN = 13,           // In operator (value is in the specified set) 
            _COMP_OP_NOT_IN = 14,       // Not-in operator (value is not in the specified set) 
        }
    }
}