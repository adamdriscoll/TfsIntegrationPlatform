// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    public static class ClearQuestConstants
    {
        public const string CqRecordHwm = "ClearQuestAdapter.RecordHwm";    // do not localize
        public const string CqLinkHwm = "ClearQuestAdapter.LinkHwm";        // do not localize

        public const string CqConnectionStringUrlPrefix = @"cq://";         // do not localize

        #region CUSTOM SETTING KEYS
        // login credential config
        public const string LoginCredentialSettingKey = "LoginCredentialConfigType";            // do not localize
        public const string LoginCredentialSettingUseTextUsernamePasswordPairInConfig = "UseTextUsernamePasswordPairInConfig"; // do not localize
        public const string LoginCredentialSettingUseStoredCredential = "UseStoredCredential";  // do not localize
        public const string UserNameKey = "UserName";           // do not localize
        public const string PasswordKey = "Password";           // do not localize
        public const string AdminUserNameKey = "AdminUserName"; // do not localize
        public const string AdminPasswordKey = "AdminPassword"; // do not localize
        
        // note field
        public const string NoteEntryFieldNameSettingKey = "NoteEntryFieldName";    // do not localize
        public const string NotesLogFieldNameSettingKey = "NotesLogFieldName";      // do not localize
        public const string NoteEntryFieldNameDefaultValue = "Note_Entry";          // do not localize
        public const string NotesLogFieldNameDefaultValue = "Notes_Log";            // do not localize

        // State field -- the field that stores the state information of Stateful Entity Types
        // This custom setting is in the format of "EntityDefName::FieldDefName"
        public const string EntityStateFieldSettingKey = "EntityStateField";        // do not localize
        public const string EntityStateFieldSettingDelimiter = "::";                // do not localize
        public const string EntityStateFieldDefaultValue = "State";                 // do not localize

        // Attachment sink field -- CQ record type allows multiple attachment fields, while systems like TFS do not
        // this custom setting is in the format of "EntityDefName::AttachmentSinkFieldDefName"
        public const string AttachmentSinkFieldSettingKey = "AttachmentSinkField";  // do not localize
        public const string AttachmentSinkFieldSettingDelimiter = "::";             // do not localize
        public const string AttachmentSinkFieldDefaultValue = "Attachments";        // do not localize

        public const string CqWebRecordUrlBaseSettingKey = "CQWebRecordUrlFormat";    // do not localize

        public const string EnableLastRevisionAutoCorrection = "EnableLastRevisionAutoCorrection";

        // A delimiter other than single quote may be needed around time values in CQ queries for certain backend databases
        public const string CQQueryTimeDelimiter = "CQQueryTimeDelimiter";
        public const string CQQueryDefaultTimeDelimiter = "'";

        // A DateTimeFormat other than the "ISO 8601" DateTime string format (yyyy-MM-dd HH:mm:ss)
        public const string CQQueryDateTimeFormat = "CQQueryDateTimeFormat";
        public const string CQQueryDefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public const string CQTimeOffsetFromServerHistoryTimesInMinutes = "CQTimeOffsetFromServerHistoryTimesInMinutes";
        
        #endregion    
    }
}
