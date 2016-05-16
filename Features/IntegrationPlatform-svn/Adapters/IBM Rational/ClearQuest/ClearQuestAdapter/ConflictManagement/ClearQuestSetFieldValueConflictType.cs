// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestSetFieldValueConflictType : ConflictType
    {
        public ClearQuestSetFieldValueConflictType()
            : base(new ClearQuestSetFieldValueConflictHandler())
        { }

        public static MigrationConflict CreateConflict(
            string sourceItemId,
            string sourceItemRevision,
            string fieldName,
            string fieldValue,
            string errorString)
        {
            return new MigrationConflict(
                new ClearQuestSetFieldValueConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(sourceItemId, sourceItemRevision, fieldName, fieldValue, errorString),
                CreateScopeHint(sourceItemId, sourceItemRevision, fieldName));
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{3826CA84-E59D-47cc-864B-00E065125DAC}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_SetFieldConflict_Name; }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_ClearQuestSetFieldValueConflictType";
            }
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            var conflictDetails = DeserializeConflictDetails(dtls);

            return string.Format(CultureInfo.InvariantCulture,
                                 ClearQuestResource.ClearQuest_Conflict_SetFieldConflict_DtlsFormat,
                                 conflictDetails.SourceItemId,
                                 conflictDetails.SourceItemRevision,
                                 conflictDetails.FieldName,
                                 conflictDetails.FieldValue ?? ClearQuestSetFieldValueConflictTypeDetails.NullValueString,
                                 conflictDetails.ErrorString);
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new ClearQuestConflictResolutionUseValueMap());
            AddSupportedResolutionAction(new ClearQuestConflictResolutionDropValueSetting());
            AddSupportedResolutionAction(new ClearQuestConflictResolutionUseRegexValueReplacement());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        internal ClearQuestSetFieldValueConflictTypeDetails GetConflictDetails(MigrationConflict conflict)
        {
            if (!conflict.ConflictType.ReferenceName.Equals(this.ReferenceName))
            {
                throw new InvalidOperationException();
            }

            return DeserializeConflictDetails(conflict.ConflictDetails);
        }

        private static ClearQuestSetFieldValueConflictTypeDetails DeserializeConflictDetails(string conflictDetailsStr)
        {
            if (string.IsNullOrEmpty(conflictDetailsStr))
            {
                throw new ArgumentNullException("conflictDetailsStr");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ClearQuestSetFieldValueConflictTypeDetails));

            using (StringReader strReader = new StringReader(conflictDetailsStr))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                return serializer.Deserialize(xmlReader) as ClearQuestSetFieldValueConflictTypeDetails;
            }
        }

        private static string CreateConflictDetails(
            string sourceItemId,
            string sourceItemRevision,
            string fieldName,
            string fieldValue,
            string errorString)
        {
            ClearQuestSetFieldValueConflictTypeDetails dtls = new ClearQuestSetFieldValueConflictTypeDetails(
                                                                    sourceItemId, sourceItemRevision, 
                                                                    fieldName, fieldValue, errorString);

            XmlSerializer serializer = new XmlSerializer(typeof(ClearQuestSetFieldValueConflictTypeDetails));
            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, dtls);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        private static string CreateScopeHint(
            string sourceItemId,
            string sourceItemRevision,
            string fieldName)
        {
            return string.Format("/{0}/{1}/{2}", sourceItemId, sourceItemRevision, fieldName);
        }
    }

    [Serializable]
    public class ClearQuestSetFieldValueConflictTypeDetails
    {
        public const string NullValueString = "@@NULL@@"; // do not localize


        public ClearQuestSetFieldValueConflictTypeDetails()
        { }

        public ClearQuestSetFieldValueConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
             string fieldName,
            string fieldValue,
            string errorString)
        {
            SourceItemId = sourceItemId;
            SourceItemRevision = sourceItemRevision;

            FieldName = fieldName;
            FieldValue = fieldValue ?? NullValueString;
            ErrorString = errorString ?? string.Empty;
        }

        public string SourceItemId { get; set; }
        public string SourceItemRevision { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string ErrorString { get; set; }
    }
}
