// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// A utility class that makes sure, after submitting a migration action,
    /// the migrated work item is in valid status.
    /// 
    /// This algorithm may sacrifice the transformation result to content
    /// correctness.
    /// </summary>
    class FieldValueCorrectionAlgorithm
    {
        const int MaxNumOfTries = 10; 

        public void TryCorrectFieldValue(Field f, IMigrationAction action)
        {
            if (TFStringComparer.WorkItemFieldReferenceName.Equals(f.ReferenceName, CoreFieldReferenceNames.State))
            {
                return;
            }

            int numOfTries = 0;

            switch (f.Status)
            {
                case FieldStatus.Valid:
                    break;
                case FieldStatus.InvalidEmpty:      // field value is empty
                case FieldStatus.InvalidListValue:  // field value is not in the list value
                case FieldStatus.InvalidOldValue:   // field value is equal to old value
                case FieldStatus.InvalidEmptyOrOldValue:    // field value is empty or equal to old value
                case FieldStatus.InvalidNotEmptyOrOldValue: // field value is not empty or equal to old value
                case FieldStatus.InvalidValueInOtherField:  // fied value is used in other field
                case FieldStatus.InvalidType:
                case FieldStatus.InvalidDate:
                    // NOTE: 
                    // when assigning value to fields that have their value copied in state transition
                    // TFS OM neglects the value assignment, 
                    // i.e. f.Value = newValue changes the field status if it was InvalidEmpty, but does not assign
                    // newValue to f.Value. As a result, f.Value remains to be empty string
                    FieldStatus prevStatus = f.Status;

                    object valueBackup = f.Value;
                    object newValue = null;
                    while (numOfTries < MaxNumOfTries)
                    {
                        newValue = GetNewDefaultValue(f, numOfTries);
                        f.Value = newValue;
                        if (f.IsValid) break;
                        ++numOfTries;
                    }

                    if (!f.IsValid 
                        && f.Status != FieldStatus.InvalidEmpty 
                        && f.Status != FieldStatus.InvalidEmptyOrOldValue)
                    {
                        // last try with NULL (empty value)
                        f.Value = null;
                    }

                    if (!f.IsValid)
                    {
                        // restore the backup value
                        f.Value = valueBackup;
                    }

                    if ((prevStatus == FieldStatus.InvalidEmpty || prevStatus == FieldStatus.InvalidEmptyOrOldValue)
                        && null != newValue)
                    {
                        UpdateActionDescription(f, action, newValue);
                    }
                    else
                    {
                        UpdateActionDescription(f, action);
                    }
                    break;
                case FieldStatus.InvalidNotEmpty:       // field value is not null
                case FieldStatus.InvalidComputedField:  // computed field is edited
                    TraceManager.TraceInformation("Setting null for field '{0}' - current field status '{1}'", f.ReferenceName, f.Status.ToString());
                    f.Value = null;
                    UpdateActionDescription(f, action);
                    break;               
                case FieldStatus.InvalidNotOldValue:    // field value is not equal to old value (disallowed in rule)
                    TraceManager.TraceInformation("Setting original value for field '{0}' - current field status '{1}'", f.ReferenceName, f.Status.ToString());
                    f.Value = f.OriginalValue;
                    UpdateActionDescription(f, action);
                    break;               
                 case FieldStatus.InvalidFormat:
                    // OM does not expose enough information for us to deal with FORMAT mapping
                    break;
                case FieldStatus.InvalidValueNotInOtherField:
                    // OM does not expose enough information for us to deal with FORMAT mapping
                    break;
                case FieldStatus.InvalidUnknown:
                    // OM does not expose enough information for us to deal with FORMAT mapping
                    break;
                case FieldStatus.InvalidTooLong:
                    TraceManager.TraceInformation("Limiting string field length for field '{0}' - current field status '{1}'", f.ReferenceName, f.Status.ToString());
                    string valueString = f.Value.ToString();
                    f.Value = valueString.Substring(0, (int)StringDataLengths.StringFieldLength - 1);
                    UpdateActionDescription(f, action);
                    break;
                case FieldStatus.InvalidPath:
                    // cannot happen: path is either faulted in or raised as a conflict when fault-in creation is disabled
                    break;
                case FieldStatus.InvalidCharacters:
                    // do nothing to preserve content correctness
                    break;
            }
        }

        private object GetNewDefaultValue(Field f, int numOfTries)
        {
            TraceManager.TraceInformation("Getting new default value for field '{0}' - current field status '{1}'", f.ReferenceName, f.Status.ToString());
            if (f.AllowedValues != null && f.AllowedValues.Count > 0)
            {
                int index = (numOfTries >= f.AllowedValues.Count 
                            ? f.AllowedValues.Count - 1 
                            : numOfTries);

                return f.AllowedValues[index];
            }
            else if (f.FieldDefinition.AllowedValues != null && f.FieldDefinition.AllowedValues.Count > 0)
            {
                int index = (numOfTries >= f.FieldDefinition.AllowedValues.Count 
                            ? f.FieldDefinition.AllowedValues.Count - 1 
                            : numOfTries);
                return f.FieldDefinition.AllowedValues[index];
            }
            else
            {
                switch (f.FieldDefinition.FieldType)
                {
                    case FieldType.DateTime:
                        return DateTime.Now;
                    case FieldType.Double:
                    case FieldType.Integer:
                        return 0 + numOfTries;
                    case FieldType.History:
                    case FieldType.Html:
                    case FieldType.PlainText:
                    case FieldType.String:
                        return "Default Value";
                    case FieldType.TreePath:
                        // won't happen
                    default:
                        return null;
                }
            }
        }

        private void UpdateActionDescription(Field f, IMigrationAction action)
        {
            TraceManager.TraceInformation("Correcting field '{0}' with new value '{1}'", f.ReferenceName, f.Value ?? "null");

            XmlNode fieldCol = action.MigrationActionDescription.SelectSingleNode(
                string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']", f.ReferenceName));

            if (fieldCol == null)
            {
                // field column does not exist in the update document
                XmlNode columnsNode = action.MigrationActionDescription.SelectSingleNode("/WorkItemChanges/Columns");
                var newFieldCol = TfsMigrationWorkItem.CreateFieldColumn(action.MigrationActionDescription, f);
                columnsNode.AppendChild(newFieldCol);
            }
            else
            {
                object translatedValue = TfsMigrationWorkItem.TranslateFieldValue(f);
                string updatedFieldValue = (translatedValue == null ? string.Empty : translatedValue.ToString());
                fieldCol.FirstChild.InnerText = updatedFieldValue;
            }
        }

        private void UpdateActionDescription(Field f, IMigrationAction action, object newValue)
        {
            TraceManager.TraceInformation("Correcting field '{0}' with new value '{1}'", f.ReferenceName, newValue ?? "null");

            XmlNode fieldCol = action.MigrationActionDescription.SelectSingleNode(
                string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']", f.ReferenceName));

            if (fieldCol == null)
            {
                // field column does not exist in the update document
                XmlNode columnsNode = action.MigrationActionDescription.SelectSingleNode("/WorkItemChanges/Columns");
                var newFieldCol = (newValue == null)
                    ? TfsMigrationWorkItem.CreateFieldColumn(action.MigrationActionDescription, f)
                    : TfsMigrationWorkItem.CreateFieldColumn(action.MigrationActionDescription, f, newValue);
                columnsNode.AppendChild(newFieldCol);
            }
            else
            {
                object translatedValue = (newValue == null) 
                    ? TfsMigrationWorkItem.TranslateFieldValue(f)
                    : TfsMigrationWorkItem.TranslateFieldValue(f, newValue);
                string updatedFieldValue = (translatedValue == null ? string.Empty : translatedValue.ToString());
                fieldCol.FirstChild.InnerText = updatedFieldValue;
            }
        }
    }
}
