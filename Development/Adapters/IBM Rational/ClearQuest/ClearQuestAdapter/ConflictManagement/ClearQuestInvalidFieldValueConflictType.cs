// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestInvalidFieldValueConflictType : ConflictType
    {
        public const string ConflictDetailsKey_RecordType = "RecordType";
        public const string ConflictDetailsKey_FieldName = "FieldName";
        public const string ConflictDetailsKey_FieldValue = "FieldValue";
        public const string ConflictDetailsKey_Reason = "Reason";

        public ClearQuestInvalidFieldValueConflictType()
            : base(new ClearQuestInvalidFieldValueConflictHandler())
        { }

        internal static Microsoft.TeamFoundation.Migration.Toolkit.MigrationConflict CreateConflict(
            CQTextParser.RecordValidationResult rslt, IMigrationAction action)
        {
            var newConflict = new MigrationConflict(
                new ClearQuestInvalidFieldValueConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(rslt, action),
                CreateScopeHint(rslt, action));
            newConflict.ConflictedChangeAction = action;
            return newConflict;
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{34C5F743-6130-43F2-811F-032034946A08}"); }
        }

        public override string FriendlyName
        {
            get
            {
                return ClearQuestResource.ClearQuest_Conflict_InvalidFieldValue_Name;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_InvalidFieldValueConflictType";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new UseValueMapConflictResolutionAction());
            AddSupportedResolutionAction(new DropFieldConflictResolutionAction());
            AddSupportedResolutionAction(new UpdatedConfigurationResolutionAction());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_RecordType);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_FieldName);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_FieldValue);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_Reason);
        }

        private static string CreateScopeHint(CQTextParser.RecordValidationResult rslt, IMigrationAction action)
        {
            string recordType = UtilityMethods.ExtractRecordType(action);
            Debug.Assert(!string.IsNullOrEmpty(rslt.InvalidFieldName), "invalid field name is empty");
            return string.Format("/{0}/{1}", recordType, rslt.InvalidFieldName);
        }

        private static string CreateConflictDetails(CQTextParser.RecordValidationResult rslt, IMigrationAction action)
        {
            ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();

            string recordType = UtilityMethods.ExtractRecordType(action);
            detailsProperties.Properties.Add(ConflictDetailsKey_RecordType, recordType);
            detailsProperties.Properties.Add(ConflictDetailsKey_FieldName, rslt.InvalidFieldName ?? string.Empty);
            detailsProperties.Properties.Add(ConflictDetailsKey_FieldValue, rslt.InvalidFieldValue ?? string.Empty);
            detailsProperties.Properties.Add(ConflictDetailsKey_Reason, rslt.ResultDescription.ToString());

            return detailsProperties.ToString();
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);

                if (!string.IsNullOrEmpty(properties[ConflictDetailsKey_RecordType]))
                {
                    string conflictDescription;
                    switch (properties[ConflictDetailsKey_Reason])
                    {
                        case "MissingValueInMandatoryField":
                            conflictDescription = "Source work item '{0}' is missing value in mandatory field '{1}'.";
                            break;
                        case "InvalidValueOfChoiceList":
                            conflictDescription = "Source work item '{0}' contains the invalid value of '{2}' in field '{1}'.";
                            break;
                        default:
                            conflictDescription = "Source work item '{0}' contains invalid field change on '{1}' in value '{2}' because '{3}'.";
                            break;
                    }
                    return string.Format(conflictDescription,
                        properties[ConflictDetailsKey_RecordType],
                        properties[ConflictDetailsKey_FieldName],
                        properties[ConflictDetailsKey_FieldValue],
                        properties[ConflictDetailsKey_Reason]);
                }
                else
                {
                    // no expected data, just return raw details string
                    return dtls;
                }
            }
            catch (Exception)
            {
                // old style conflict details, just return raw details string
                return dtls;
            }
        }
    }
}
