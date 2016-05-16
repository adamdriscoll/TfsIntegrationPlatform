// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    public class ClearQuestMigrationContext
    {
        internal ClearQuestMigrationContext(
            ClearQuestOleServer.Session userSession, 
            MigrationSource migrationSource)
        {
            PerEntityTypeStateField = new Dictionary<string, string>();
            PerEntityTypeAttachmentSinkField = new Dictionary<string, string>();

            // try extract context info from the custom settings in the config
            foreach (var setting in migrationSource.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals(ClearQuestConstants.NoteEntryFieldNameSettingKey))
                {
                    NoteEntryFieldName = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.NotesLogFieldNameSettingKey))
                {
                    NotesLogFieldName = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.CQQueryTimeDelimiter))
                {
                    CQQueryTimeDelimiter = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.CQQueryDateTimeFormat))
                {
                    CQQueryDateTimeFormat = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.CQTimeOffsetFromServerHistoryTimesInMinutes))
                {
                    CQTimeOffsetFromServerHistoryTimesInMinutes = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.EntityStateFieldSettingKey))
                {
                  if (!string.IsNullOrEmpty(setting.SettingValue))
                  {
                    string[] splits = setting.SettingValue.Split(new string[] { ClearQuestConstants.EntityStateFieldSettingDelimiter },
                                                               StringSplitOptions.RemoveEmptyEntries);

                    if (splits.Length == 2
                        && !string.IsNullOrEmpty(splits[0])
                        && !string.IsNullOrEmpty(splits[1])
                        && !PerEntityTypeStateField.ContainsKey(splits[0]))
                    {
                      PerEntityTypeStateField.Add(splits[0], splits[1]);
                    }
                  }
                }
                else if (setting.SettingKey.Equals(ClearQuestConstants.AttachmentSinkFieldSettingKey))
                {
                  if (!string.IsNullOrEmpty(setting.SettingValue))
                  {
                    string[] splits = setting.SettingValue.Split(new string[] { ClearQuestConstants.AttachmentSinkFieldSettingDelimiter },
                                                               StringSplitOptions.RemoveEmptyEntries);

                    if (splits.Length == 2
                        && !string.IsNullOrEmpty(splits[0])
                        && !string.IsNullOrEmpty(splits[1])
                        && !PerEntityTypeAttachmentSinkField.ContainsKey(splits[0]))
                    {
                      PerEntityTypeAttachmentSinkField.Add(splits[0], splits[1]);
                    }
                  }
                }
            }

            // fall back to default values if no setting found
            // Setting NoteEntryFieldName="" or NotesLogFieldName="" can be used to disable the setting of these fields
            if (null == NoteEntryFieldName)
            {
                NoteEntryFieldName = ClearQuestConstants.NoteEntryFieldNameDefaultValue;
            }

            if (null == NotesLogFieldName)
            {
                NotesLogFieldName = ClearQuestConstants.NotesLogFieldNameDefaultValue;
            }

            if (string.IsNullOrEmpty(CQQueryTimeDelimiter))
            {
                CQQueryTimeDelimiter = ClearQuestConstants.CQQueryDefaultTimeDelimiter;
            }

            if (string.IsNullOrEmpty(CQQueryDateTimeFormat))
            {
              CQQueryDateTimeFormat = ClearQuestConstants.CQQueryDefaultDateTimeFormat;
            }

            UserSession = userSession;
        }

        /// <summary>
        /// Gets note_entry field to write the other-side revision details to
        /// </summary>
        public string NoteEntryFieldName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets notes_log field to extract this-side (CQ) revision details if any
        /// </summary>
        public string NotesLogFieldName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the delimter to use in CQ queries that use a time value
        /// </summary>
        public string CQQueryTimeDelimiter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the offset in minutes of the time zone for the migration machine from the time zone of the ClearQuest
        /// server to be used when the action_timestamp values in the ClearQuest server are in local times.
        /// This was always true until ClearQuest 7.0.1 which introduced the option to configure all timestamps to use 
        /// UTC.  If the server is configured to use UTC, this configuration setting should not be included.
        /// If the server uses local times:
        ///     If the migration machine and the ClearQuest server are in the same time zone, then a value of 0 should be entered
        ///     If the migration machine and the ClearQuest server are in the different time zones, the difference in minutes
        ///     should be entered using a negative value if the migration machine has an earlier time than the ClearQuest server,
        ///     and a positive value if the migration machine has a later time than the ClearQuest server
        /// </summary>
        public string CQTimeOffsetFromServerHistoryTimesInMinutes
        {
          get;
          private set;
        }

        /// <summary>
        /// Gets the DateTimeFormat to use in the CQ queries that use a time value
        /// </summary>
        public string CQQueryDateTimeFormat
        {
            get;
            private set;
        }

        internal DateTime CurrentHWMBaseLine
        {
            get;
            set;
        }

        internal ClearQuestOleServer.Session UserSession
        {
            get;
            private set;
        }

        internal string GetStateField(string entityDefName)
        {
            if (PerEntityTypeStateField.ContainsKey(entityDefName))
            {
                return PerEntityTypeStateField[entityDefName];
            }
            else
            {
                return ClearQuestConstants.EntityStateFieldDefaultValue;
            }
        }

        internal string GetAttachmentSinkField(string entityDefName)
        {
            if (PerEntityTypeAttachmentSinkField.ContainsKey(entityDefName))
            {
                return PerEntityTypeAttachmentSinkField[entityDefName];
            }
            else
            {
                return ClearQuestConstants.AttachmentSinkFieldDefaultValue;
            }
        }

        private Dictionary<string, string> PerEntityTypeStateField
        {
            get;
            set;
        }

        private Dictionary<string, string> PerEntityTypeAttachmentSinkField
        {
            get;
            set;
        }
    }
}
