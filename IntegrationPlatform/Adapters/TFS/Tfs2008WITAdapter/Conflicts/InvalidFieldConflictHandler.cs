// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Xml;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    public class InvalidFieldConflictHandler : IConflictHandler
    {

        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
            {
                return false;
            }

            if (rule.ActionRefNameGuid.Equals(new InvalidFieldConflictUseFieldMapAction().ReferenceName))
            {
                return CanResolveByFieldMap(conflict, rule);
            }

            if (rule.ActionRefNameGuid.Equals(new InvalidFieldConflictDropFieldAction().ReferenceName))
            {
                return CanResolveByDroppingField(conflict, rule);
            }

            return true;
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Other);
            }
            else if (rule.ActionRefNameGuid.Equals(new InvalidFieldConflictUseFieldMapAction().ReferenceName))
            {
                return ResolveByFieldMap(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new InvalidFieldConflictDropFieldAction().ReferenceName))
            {
                return ResolveByDroppingField(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new UpdatedConfigurationResolutionAction().ReferenceName))
            {
                return Toolkit.Utility.ResolveConflictByUpdateConfig(rule);
            }
            else if (rule.ActionRefNameGuid.Equals(new SkipConflictedActionResolutionAction().ReferenceName))
            {
                return SkipConflictedActionResolutionAction.SkipConflict(conflict, true);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion


        private bool CanResolveByFieldMap(
            MigrationConflict conflict,
            ConflictResolutionRule rule)
        {
            if (conflict.ConflictedChangeAction == null)
            {
                throw new InvalidOperationException();
            }

            if (conflict.ConflictedChangeAction.MigrationActionDescription == null)
            {
                throw new InvalidOperationException();
            }

            var actionDataKeys = new InvalidFieldConflictUseFieldMapAction().ActionDataKeys;
            if (actionDataKeys.Count != rule.DataField.Length)
            {
                TraceManager.TraceInformation(TfsWITAdapterResources.ErrorResolutionRuleContainsInvalidData,
                    rule.RuleReferenceName);
                return false;
            }
            foreach (DataField df in rule.DataField)
            {
                if (!actionDataKeys.Contains(df.FieldName))
                {
                    TraceManager.TraceInformation(TfsWITAdapterResources.ErrorResolutionRuleContainsInvalidDataField,
                        rule.RuleReferenceName, df.FieldName);
                    return false;
                }
            }

            string mapFromValue = rule.DataFieldDictionary[InvalidFieldConflictUseFieldMapAction.DATAKEY_MAP_FROM];
            InvalidFieldConflictType conflictType = conflict.ConflictType as InvalidFieldConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }
            InvalidFieldConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            return TFStringComparer.WorkItemFieldReferenceName.Equals(mapFromValue, conflictDetails.SourceFieldRefName);
        }

        private bool CanResolveByDroppingField(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (conflict.ConflictedChangeAction == null)
            {
                throw new InvalidOperationException();
            }

            if (conflict.ConflictedChangeAction.MigrationActionDescription == null)
            {
                throw new InvalidOperationException();
            }

            var actionDataKeys = new InvalidFieldConflictDropFieldAction().ActionDataKeys;
            if (actionDataKeys.Count != rule.DataField.Length)
            {
                TraceManager.TraceInformation(TfsWITAdapterResources.ErrorResolutionRuleContainsInvalidData,
                    rule.RuleReferenceName);
                return false;
            }
            foreach (DataField df in rule.DataField)
            {
                if (!actionDataKeys.Contains(df.FieldName))
                {
                    TraceManager.TraceInformation(TfsWITAdapterResources.ErrorResolutionRuleContainsInvalidDataField,
                        rule.RuleReferenceName, df.FieldName);
                    return false;
                }
            }

            string invalidFieldName = rule.DataFieldDictionary[InvalidFieldConflictDropFieldAction.DATAKEY_INVALID_FIELD];
            InvalidFieldConflictType conflictType = conflict.ConflictType as InvalidFieldConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }
            InvalidFieldConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            return TFStringComparer.WorkItemFieldReferenceName.Equals(invalidFieldName, conflictDetails.SourceFieldRefName);
        }

        private ConflictResolutionResult ResolveByFieldMap(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            InvalidFieldConflictType conflictType = conflict.ConflictType as InvalidFieldConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            string mapFromValue = rule.DataFieldDictionary[InvalidFieldConflictUseFieldMapAction.DATAKEY_MAP_FROM];
            string mapToValue = rule.DataFieldDictionary[InvalidFieldConflictUseFieldMapAction.DATAKEY_MAP_TO];

            if (string.IsNullOrEmpty(mapToValue))
            {
                return DropField(conflict, mapFromValue);
            }

            InvalidFieldConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            //
            // apply field map to the Action's Description document
            //
            XmlDocument desc = conflict.ConflictedChangeAction.MigrationActionDescription;
            XmlNode column = desc.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", conflictDetails.SourceFieldRefName));

            if (column == null ||
                !TFStringComparer.WorkItemFieldReferenceName.Equals(mapFromValue, column.Attributes["ReferenceName"].Value))
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
            column.Attributes["ReferenceName"].Value = mapToValue;

            //note: changes to "MigrationConflict conflict" is saved by the conflict manager automatically
            return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
        }

        private ConflictResolutionResult ResolveByDroppingField(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            InvalidFieldConflictType conflictType = conflict.ConflictType as InvalidFieldConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            string invalidFieldName = rule.DataFieldDictionary[InvalidFieldConflictDropFieldAction.DATAKEY_INVALID_FIELD];

            return DropField(conflict, invalidFieldName);
        }

        private static ConflictResolutionResult DropField(MigrationConflict conflict, string fieldToDrop)
        {
            //
            // apply field map to the Action's Description document
            //
            XmlDocument desc = conflict.ConflictedChangeAction.MigrationActionDescription;
            XmlNode column = desc.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", fieldToDrop));

            if (column == null ||
                !TFStringComparer.WorkItemFieldReferenceName.Equals(fieldToDrop, column.Attributes["ReferenceName"].Value))
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
            XmlNode columnsNode = column.ParentNode;
            columnsNode.RemoveChild(column);

            //note: changes to "MigrationConflict conflict" is saved by the conflict manager automatically
            return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
        }
    }
}
