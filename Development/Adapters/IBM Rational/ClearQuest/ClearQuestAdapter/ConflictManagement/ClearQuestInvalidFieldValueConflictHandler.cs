// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.ComponentModel.Design;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    public class ClearQuestInvalidFieldValueConflictHandler : ClearQuestConflictHandlerBase
    {
        public override ConflictResolutionResult Resolve(
            IServiceContainer serviceContainer, 
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new UseValueMapConflictResolutionAction().ReferenceName))
            {
                return ResolveByValueMap(conflict, rule, out actions); 
            }
            else if (rule.ActionRefNameGuid.Equals(new DropFieldConflictResolutionAction().ReferenceName))
            {
                return ResolveByDroppingField(conflict, rule, out actions);
            }
            else if (rule.ActionRefNameGuid.Equals(new UpdatedConfigurationResolutionAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConfiguration);
            }
            else if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return base.ManualResolve(conflict, rule, out actions);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.UnknownResolutionAction);
        }

        private ConflictResolutionResult ResolveByDroppingField(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            ClearQuestInvalidFieldValueConflictType conflictType = conflict.ConflictType as ClearQuestInvalidFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            string invalidFieldName = rule.DataFieldDictionary[DropFieldConflictResolutionAction.ActionDataKey_FieldName];

            if (string.IsNullOrEmpty(invalidFieldName))
            {
                var result = new ConflictResolutionResult(false, ConflictResolutionType.Other);
                result.Comment = string.Format(ClearQuestResource.ClearQuest_Conflict_MissingResolutionData, 
                    DropFieldConflictResolutionAction.ActionDataKey_FieldName);
                return result;
            }
            //
            // apply field map to the Action's Description document
            //
            XmlDocument desc = conflict.ConflictedChangeAction.MigrationActionDescription;
            XmlNode column = desc.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", invalidFieldName));

            if (column == null || invalidFieldName != column.Attributes["ReferenceName"].Value)
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
            XmlNode columnsNode = column.ParentNode;
            columnsNode.RemoveChild(column);

            //note: changes to "MigrationConflict conflict" is saved by the conflict manager automatically
            return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
        }

        private ConflictResolutionResult ResolveByValueMap(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            ClearQuestInvalidFieldValueConflictType conflictType = conflict.ConflictType as ClearQuestInvalidFieldValueConflictType;
            if (null == conflictType)
            {
                throw new InvalidOperationException();
            }

            string mapFromValue = rule.DataFieldDictionary[UseValueMapConflictResolutionAction.ActionDataKey_MapFromValue];
            string mapToValue = rule.DataFieldDictionary[UseValueMapConflictResolutionAction.ActionDataKey_MapToValue];
            string targetFieldName = rule.DataFieldDictionary[UseValueMapConflictResolutionAction.ActionDataKey_TargetFieldName];

            //
            // apply value map to the Action's Description document
            //
            XmlDocument desc = conflict.ConflictedChangeAction.MigrationActionDescription;
            XmlNode column = desc.SelectSingleNode(string.Format(
                @"/WorkItemChanges/Columns/Column[@ReferenceName=""{0}""]", targetFieldName));

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
    }
}
