// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Xml;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Toolkit = Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class InvalidFieldValueConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
            {
                return false;
            }

            if (rule.ActionRefNameGuid.Equals(new InvalidFieldValueConflictUseValueMapAction().ReferenceName))
            {
                return CanResolveByValueMap(conflict, rule);
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
            if (rule.ActionRefNameGuid.Equals(new InvalidFieldValueConflictUseValueMapAction().ReferenceName))
            {
                return ResolveByValueMap(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new InvalidFieldConflictDropFieldAction().ReferenceName))
            {
                return ResolveByDroppingField(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Other);
            }
            else if (rule.ActionRefNameGuid.Equals(new UpdatedConfigurationResolutionAction().ReferenceName))
            {
                return Toolkit.Utility.ResolveConflictByUpdateConfig(rule);
            }
            else if (rule.ActionRefNameGuid.Equals(new SkipConflictedActionResolutionAction().ReferenceName))
            {
                conflict.ConflictedChangeAction.ChangeGroup.Status = ChangeStatus.Skipped;
                conflict.ConflictedChangeAction.State = ActionState.Skipped;
                return new ConflictResolutionResult(true, ConflictResolutionType.SkipConflictedChangeAction);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion

        private ConflictResolutionResult ResolveByDroppingField(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            InvalidFieldValueConflictType conflictType = conflict.ConflictType as InvalidFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            string invalidFieldName = rule.DataFieldDictionary[InvalidFieldConflictDropFieldAction.DATAKEY_INVALID_FIELD];
            InvalidFieldValueConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            //
            // apply field map to the Action's Description document
            //
            XmlDocument desc = conflict.ConflictedChangeAction.MigrationActionDescription;
            XmlNode column = desc.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", invalidFieldName));

            if (column == null ||
                !TFStringComparer.WorkItemFieldReferenceName.Equals(invalidFieldName, column.Attributes["ReferenceName"].Value))
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
            XmlNode columnsNode = column.ParentNode;
            columnsNode.RemoveChild(column);

            //note: changes to "MigrationConflict conflict" is saved by the conflict manager automatically
            return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
        }

        private ConflictResolutionResult ResolveByValueMap(
            MigrationConflict conflict,
            ConflictResolutionRule rule,
            out List<MigrationAction> actions)
        {
            actions = null;

            InvalidFieldValueConflictType conflictType = conflict.ConflictType as InvalidFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            string mapFromValue = rule.DataFieldDictionary[InvalidFieldValueConflictUseValueMapAction.DATAKEY_MAP_FROM];
            string mapToValue = rule.DataFieldDictionary[InvalidFieldValueConflictUseValueMapAction.DATAKEY_MAP_TO];

            InvalidFieldValueConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            //
            // apply value map to the Action's Description document
            //
            XmlDocument desc = conflict.ConflictedChangeAction.MigrationActionDescription;
            XmlNode column = desc.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", conflictDetails.TargetFieldRefName));

            if (column == null)
            {
                // defer to migration time to resolve the conflict, mark it as resolved for now
                return new ConflictResolutionResult(true, ConflictResolutionType.Other);
            }
            else if (!mapFromValue.Equals(column.FirstChild.InnerText, StringComparison.InvariantCulture))
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
            column.FirstChild.InnerText = mapToValue;

            //note: changes to "MigrationConflict conflict" is saved by the conflict manager automatically
            return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
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
            InvalidFieldValueConflictType conflictType = conflict.ConflictType as InvalidFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }
            InvalidFieldValueConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            return TFStringComparer.WorkItemFieldReferenceName.Equals(invalidFieldName, conflictDetails.TargetFieldRefName);
        }

        private bool CanResolveByValueMap(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (conflict.ConflictedChangeAction == null)
            {
                throw new InvalidOperationException();
            }

            if (conflict.ConflictedChangeAction.MigrationActionDescription == null)
            {
                throw new InvalidOperationException();
            }

            var actionDataKeys = new InvalidFieldValueConflictUseValueMapAction().ActionDataKeys;
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

            string mapFromValue = rule.DataFieldDictionary[InvalidFieldValueConflictUseValueMapAction.DATAKEY_MAP_FROM];

            InvalidFieldValueConflictType conflictType = conflict.ConflictType as InvalidFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }
            InvalidFieldValueConflictTypeDetails conflictDetails = conflictType.GetConflictDetails(conflict);

            return mapFromValue.Equals(conflictDetails.TargetFieldCurrentValue);
        }

    }
}
