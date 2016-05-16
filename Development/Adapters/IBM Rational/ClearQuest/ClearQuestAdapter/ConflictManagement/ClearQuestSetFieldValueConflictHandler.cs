// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    class ClearQuestSetFieldValueConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            ClearQuestSetFieldValueConflictType conflictType = conflict.ConflictType as ClearQuestSetFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
            {
                return false;
            }

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return true;
            }
            else if (rule.ActionRefNameGuid.Equals(new ClearQuestConflictResolutionUseValueMap()))
            {
                return CanResolveByValueMap(conflict, rule);
            }
            else if (rule.ActionRefNameGuid.Equals(new ClearQuestConflictResolutionDropValueSetting()))
            {
                return CanResolveByDropValueSetting(conflict, rule);
            }
            else if (rule.ActionRefNameGuid.Equals(new ClearQuestConflictResolutionUseRegexValueReplacement()))
            {
                return CanResolveByUseRegexValueReplacement(conflict, rule);
            }

            return false;
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            ClearQuestSetFieldValueConflictType conflictType = conflict.ConflictType as ClearQuestSetFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new ClearQuestConflictResolutionUseValueMap()))
            {
                return UseValueMap(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new ClearQuestConflictResolutionDropValueSetting()))
            {
                return DropValueSetting(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new ClearQuestConflictResolutionUseRegexValueReplacement()))
            {
                return UseRegexValueReplacement(conflict, rule, out actions);
            }
            else
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
        
        private bool CanResolveByUseRegexValueReplacement(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            // deserialize conflict details
            var conflictDetails = ((ClearQuestSetFieldValueConflictType)conflict.ConflictType).GetConflictDetails(conflict);
            
            // extract the field name and regex specified in the rule
            string regexPattern = rule.DataFieldDictionary[ClearQuestConflictResolutionUseRegexValueReplacement.DATAKEY_REGEX_PATTERN];
            string fieldName = rule.DataFieldDictionary[ClearQuestConflictResolutionUseRegexValueReplacement.DATAKEY_FIELD_NAME];

            if (!CQStringComparer.FieldName.Equals(conflictDetails.FieldName, fieldName))
            {
                // field name of the rule does not match that in the conflict
                return false;
            }

            // look up the field in the update document
            XmlNode column = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                conflict.ConflictedChangeAction.MigrationActionDescription, conflictDetails.FieldName);
            
            if (null == column)
            {
                // field no longer exists in the update document
                return false;
            }

            string currFieldValue = column.FirstChild.InnerText ?? ClearQuestSetFieldValueConflictTypeDetails.NullValueString;
            return Regex.IsMatch(currFieldValue, regexPattern, RegexOptions.Multiline);
        }

        private bool CanResolveByDropValueSetting(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            // deserialize conflict details
            var conflictDetails = ((ClearQuestSetFieldValueConflictType)conflict.ConflictType).GetConflictDetails(conflict);

            // extract the field name specified in the rule
            string dropFieldName = rule.DataFieldDictionary[ClearQuestConflictResolutionDropValueSetting.DATAKEY_DROP_FIELD];

            if (!CQStringComparer.FieldName.Equals(conflictDetails.FieldName, dropFieldName))
            {
                // field name of the rule does not match that in the conflict
                return false;
            }

            return true;
        }

        private bool CanResolveByValueMap(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            // deserialize conflict details
            var conflictDetails = ((ClearQuestSetFieldValueConflictType)conflict.ConflictType).GetConflictDetails(conflict);

            // extract data in the rule
            string mapFromValue = rule.DataFieldDictionary[ClearQuestConflictResolutionUseValueMap.DATAKEY_MAP_FROM];
            string fieldName = rule.DataFieldDictionary[ClearQuestConflictResolutionUseValueMap.DATAKEY_FIELD_NAME];

            if (!CQStringComparer.FieldName.Equals(conflictDetails.FieldName, fieldName))
            {
                // field name of the rule does not match that in the conflict
                return false;
            }

            // look up the field in the update document
            XmlNode column = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                conflict.ConflictedChangeAction.MigrationActionDescription, conflictDetails.FieldName);
            
            if (null == column)
            {
                // field no longer exists in the update document
                return false;
            }

            string currValue = column.FirstChild.InnerText ?? ClearQuestSetFieldValueConflictTypeDetails.NullValueString;
            
            return string.Equals(currValue, mapFromValue); 
        }


        private ConflictResolutionResult ManualResolve(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }

        private ConflictResolutionResult UseRegexValueReplacement(
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;
            ConflictResolutionResult result = new ConflictResolutionResult(false, ConflictResolutionType.Other);

            // deserialize conflict details
            var conflictDetails = ((ClearQuestSetFieldValueConflictType)conflict.ConflictType).GetConflictDetails(conflict);

            // extract the field name and regex specified in the rule
            string regexPattern = rule.DataFieldDictionary[ClearQuestConflictResolutionUseRegexValueReplacement.DATAKEY_REGEX_PATTERN];
            string fieldName = rule.DataFieldDictionary[ClearQuestConflictResolutionUseRegexValueReplacement.DATAKEY_FIELD_NAME];
            string replacement = rule.DataFieldDictionary[ClearQuestConflictResolutionUseRegexValueReplacement.DATAKEY_REPLACEMENT];

            if (!CQStringComparer.FieldName.Equals(conflictDetails.FieldName, fieldName))
            {
                // field name of the rule does not match that in the conflict
                return result;
            }

            // look up the field in the update document
            XmlNode column = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                conflict.ConflictedChangeAction.MigrationActionDescription, conflictDetails.FieldName);

            if (null == column)
            {
                // field no longer exists in the update document
                return result;
            }

            // apply Regex.Replace
            string currFieldValue = column.FirstChild.InnerText ?? ClearQuestSetFieldValueConflictTypeDetails.NullValueString;
            column.FirstChild.InnerText = Regex.Replace(currFieldValue, regexPattern, replacement, RegexOptions.Multiline);

            result.Resolved = true;
            result.ResolutionType = ConflictResolutionType.UpdatedConflictedChangeAction;

            return result;
        }

        private ConflictResolutionResult DropValueSetting(
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;
            ConflictResolutionResult result = new ConflictResolutionResult(false, ConflictResolutionType.Other);

            // deserialize conflict details
            var conflictDetails = ((ClearQuestSetFieldValueConflictType)conflict.ConflictType).GetConflictDetails(conflict);

            // extract the field name specified in the rule
            string dropFieldName = rule.DataFieldDictionary[ClearQuestConflictResolutionDropValueSetting.DATAKEY_DROP_FIELD];

            if (!CQStringComparer.FieldName.Equals(conflictDetails.FieldName, dropFieldName))
            {
                // field name of the rule does not match that in the conflict
                return result;
            }

            // look up the field in the update document
            XmlNode column = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                conflict.ConflictedChangeAction.MigrationActionDescription, conflictDetails.FieldName);
            if (null == column)
            {
                // field no longer exists in the update document
                return result;
            }

            result.Resolved = true;
            result.ResolutionType = ConflictResolutionType.UpdatedConflictedChangeAction;

            return result;
        }

        private ConflictResolutionResult UseValueMap(
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;
            ConflictResolutionResult result = new ConflictResolutionResult(false, ConflictResolutionType.Other);

            // deserialize conflict details
            var conflictDetails = ((ClearQuestSetFieldValueConflictType)conflict.ConflictType).GetConflictDetails(conflict);

            // extract data in the rule
            string mapFromValue = rule.DataFieldDictionary[ClearQuestConflictResolutionUseValueMap.DATAKEY_MAP_FROM];
            string mapToValue = rule.DataFieldDictionary[ClearQuestConflictResolutionUseValueMap.DATAKEY_MAP_TO];
            string fieldName = rule.DataFieldDictionary[ClearQuestConflictResolutionUseValueMap.DATAKEY_FIELD_NAME];

            if (!CQStringComparer.FieldName.Equals(conflictDetails.FieldName, fieldName))
            {
                // field name of the rule does not match that in the conflict
                return result;
            }

            // look up the field in the update document
            XmlNode column = UtilityMethods.ExtractSingleFieldNodeFromMigrationDescription(
                                conflict.ConflictedChangeAction.MigrationActionDescription, conflictDetails.FieldName);

            if (null == column)
            {
                // field no longer exists in the update document
                return result;
            }

            string currValue = column.FirstChild.InnerText ?? ClearQuestSetFieldValueConflictTypeDetails.NullValueString;
            if (!string.Equals(currValue, mapFromValue))
            {
                return result;
            }

            column.FirstChild.InnerText = mapToValue;

            result.Resolved = true;
            result.ResolutionType = ConflictResolutionType.UpdatedConflictedChangeAction;

            return result;
        }
    }
}
