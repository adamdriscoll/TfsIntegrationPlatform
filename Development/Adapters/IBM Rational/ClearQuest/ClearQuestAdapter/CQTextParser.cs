// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal static class CQTextParser
    {
        internal enum ValidationResult
        {
            Unknown = 0,
            MissingValueInMandatoryField = 1,
            InvalidValueOfChoiceList = 2,
            InvalidReferenceFieldValue = 3,
        }

        internal abstract class RecordValidationResult
        {
            public abstract ValidationResult ResultDescription { get; }
            public abstract string InvalidFieldName { get; }
            public virtual string InvalidFieldValue
            {
                get
                {
                    return string.Empty;
                }
            }
        }

        internal class MissingValueInMandatoryField : RecordValidationResult
        {
            private string m_missingFieldName;

            public MissingValueInMandatoryField(string missingFieldName)
            {
                m_missingFieldName = missingFieldName;
            }

            public override ValidationResult ResultDescription
            {
                get 
                { 
                    return ValidationResult.MissingValueInMandatoryField; 
                }
            }

            public override string InvalidFieldName
            {
                get
                {
                    return m_missingFieldName;
                }
            }
        }

        internal class InvalidReferenceFieldValue : RecordValidationResult
        {
            private string m_referenceFieldName;
            private string m_fieldValue;

            public InvalidReferenceFieldValue(string referenceFieldName, string fieldValue)
            {
                m_referenceFieldName = referenceFieldName;
                m_fieldValue = fieldValue;
            }

            public override ValidationResult ResultDescription
            {
                get
                {
                    return ValidationResult.InvalidReferenceFieldValue;
                }
            }

            public override string InvalidFieldName
            {
                get
                {
                    return m_referenceFieldName;
                }
            }

            public override string InvalidFieldValue
            {
                get
                {
                    return m_fieldValue;
                }
            }
        }

        internal class InvalidValueOfChoiceList : RecordValidationResult
        {
            private string m_invalidFieldName;
            private string m_invalidFieldValue;

            public InvalidValueOfChoiceList(string invalidFieldName, string invalidFieldValue)
            {
                m_invalidFieldName = invalidFieldName;
                m_invalidFieldValue = invalidFieldValue;
            }

            public override ValidationResult ResultDescription
            {
                get 
                { 
                    return ValidationResult.InvalidValueOfChoiceList; 
                }
            }

            public override string InvalidFieldName
            {
                get 
                { 
                    return m_invalidFieldName; 
                }
            }

            public override string InvalidFieldValue
            {
                get 
                { 
                    return m_invalidFieldValue; 
                }
            }
        }

        internal static class RecordValidationTextParser
        {
            //These fields have invalid values:
            //Headline...
            //   The field "Headline" is mandatory; a value must be specified.
            //Severity...
            //   The field "Severity" has values not permitted by its choice list:
            //   "1"
            //Owner...
            //   The field "Owner" has the value "User A", but this does not identify an existing instance of record type "users".

            const string InvalidFieldValueHeader = "These fields have invalid values:";
            const string FieldLineSurfix = "...";
            const string DetailsLinePrefix = "The field";
            const string MissingMandatoryFieldValueKey = "mandatory";
            const string InvalidValueInChoiceListKey = "values not permitted by its choice list";
            const string ValueNotIdentifyingReferencedRecord = "this does not identify an existing instance of record type";
            const string InvalidReferenceFieldValuedetailsKey = @"has the value """;

            public static bool TryParse(string text, out IEnumerable<RecordValidationResult> validationResults)
            {
                bool retVal = true;
                var resultList = new List<RecordValidationResult>();

                if (text.StartsWith(InvalidFieldValueHeader))
                {
                    using (System.IO.StringReader reader = new System.IO.StringReader(text))
                    {
                        string line;
                        string fieldName = null;
                        string fieldValue = null;
                        ValidationResult result = ValidationResult.Unknown;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            line = line.Trim();
                            if (line.StartsWith(InvalidFieldValueHeader))
                            {
                                continue;
                            }
                            else if (line.EndsWith(FieldLineSurfix))
                            {
                                fieldName = line.Substring(0, line.LastIndexOf(FieldLineSurfix));
                            }
                            else if (line.StartsWith(DetailsLinePrefix))
                            {
                                if (line.Contains(MissingMandatoryFieldValueKey))
                                {
                                    result = ValidationResult.MissingValueInMandatoryField;
                                    Debug.Assert(!string.IsNullOrEmpty(fieldName), "fieldName is empty");
                                    resultList.Add(new MissingValueInMandatoryField(fieldName));

                                    // reset
                                    result = ValidationResult.Unknown;
                                    fieldName = null;
                                    fieldValue = null;
                                }
                                else if (line.Contains(InvalidValueInChoiceListKey))
                                {
                                    result = ValidationResult.InvalidValueOfChoiceList;
                                }
                                else if (line.Contains(ValueNotIdentifyingReferencedRecord))
                                {
                                    result = ValidationResult.InvalidReferenceFieldValue;

                                    fieldValue = string.Empty;
                                    var indexOfDetailsKey = line.IndexOf(InvalidReferenceFieldValuedetailsKey);
                                    if (indexOfDetailsKey > 0 && indexOfDetailsKey < line.Length - 1)
                                    {
                                        fieldValue = line.Substring(indexOfDetailsKey + InvalidReferenceFieldValuedetailsKey.Length);
                                        fieldValue = fieldValue.Substring(0, fieldValue.IndexOf("\""));
                                    }

                                    Debug.Assert(!string.IsNullOrEmpty(fieldName), "fieldName is empty");
                                    resultList.Add(new InvalidReferenceFieldValue(fieldName, fieldValue));

                                    // reset
                                    result = ValidationResult.Unknown;
                                    fieldName = null;
                                    fieldValue = null;
                                }
                                else
                                {
                                    Debug.Assert(false, "Unknown record validation result");
                                    retVal = false;
                                    break;
                                }
                            }
                            else if (line.StartsWith("\""))
                            {
                                if (line.Length <= 2)
                                {
                                    fieldValue = string.Empty;
                                }
                                else
                                {
                                    Debug.Assert(line.EndsWith("\""),
                                        "Unknown line in record valiation result, starting with \"");
                                    fieldValue = line.Substring(1, line.Length - 2);
                                }

                                Debug.Assert(!string.IsNullOrEmpty(fieldName), "fieldName is empty");
                                Debug.Assert(null != fieldValue, "fieldValue is NULL");
                                Debug.Assert(result == ValidationResult.InvalidValueOfChoiceList,
                                    "ValidationResult is unexpected");

                                resultList.Add(new InvalidValueOfChoiceList(fieldName, fieldValue));

                                result = ValidationResult.Unknown;
                                fieldName = null;
                                fieldValue = null;
                            }
                        }
                    }
                }
                else
                {
                    retVal = false;
                }

                validationResults = resultList.AsEnumerable();
                return retVal;
            }
        }
    }
}
