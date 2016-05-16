// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class WITEditEditConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            if (rule.ActionRefNameGuid.Equals(new WITEditEditConflictTakeSourceChangesAction().ReferenceName))
            {
                if (null != conflict.ConflictedChangeAction && TakeSource(conflict))
                {
                    return new ConflictResolutionResult(true, ConflictResolutionType.Other);
                }
            }
            else if (rule.ActionRefNameGuid.Equals(new WITEditEditConflictTakeTargetChangesAction().ReferenceName))
            {
                if (null != conflict.ConflictedChangeAction && TakeTarget(conflict))
                {
                    return new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConflictedChangeAction);
                }
            }
            else if (rule.ActionRefNameGuid.Equals(new WITEditEditConflictIgnoreByFieldChangeAction().ReferenceName))
            {
                if (null != conflict.ConflictedChangeAction)
                {
                    bool canIgnore = TryIgnoreNonFieldConflict(conflict);
                    if (canIgnore)
                    {
                        return new ConflictResolutionResult(true, ConflictResolutionType.Other);
                    }
                }
            }
            
            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }
        
        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        private void SkipChangeWithZeroFieldUpdates(RTChangeAction action)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(action.ActionData);
            
            if (!ChangeContainsFieldUpdates(doc))
            {
                action.ChangeGroupReference.Load();
                action.ChangeGroup.Status = (int)ChangeStatus.Skipped;
            }
        }

        private bool ChangeContainsFieldUpdates(XmlDocument xmlDocument)
        {
            var cols = xmlDocument.DocumentElement.SelectNodes("/WorkItemChanges/Columns/Column");
            if (null == cols || cols.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void DropFields(XmlDocument xmlDocument, string[] dropFields)
        {
            foreach (var dropField in dropFields)
            {
                if (string.IsNullOrEmpty(dropField))
                {
                    continue;
                }

                XmlNode field = xmlDocument.DocumentElement.SelectSingleNode(string.Format("//Column[@ReferenceName='{0}']", dropField.Trim()));
                if (null != field)
                {
                    field.ParentNode.RemoveChild(field);
                }
            }
        }

        private bool TakeTarget(MigrationConflict conflict)
        {
            // look up for target-side conflicted change action id
            long targetChangeActionId;
            bool retVal = WITEditEditConflictType.TryGetConflictedTargetChangeActionId(conflict.ConflictDetails, out targetChangeActionId);
            if (!retVal)
            {
                // backward compatibility:
                // old-style edit/edit conflict details does not include target change action id
                // in that case, we can't find a change action to complete the anlaysis
                return false;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Debug.Assert(conflict.ConflictedChangeAction != null, "Edit/Edit conflict ConflictedCangeAction is NULL");

                // find the target-side change action (should be a delta table entry)
                var changeActionQuery = context.RTChangeActionSet.Where(a => a.ChangeActionId == targetChangeActionId);
                if (changeActionQuery.Count() != 1)
                {
                    return false;
                }
                RTChangeAction targetChangeAction = changeActionQuery.First();

                // Extract all fields updated in the target change action
                XmlDocument targetSideChanges = new XmlDocument();
                targetSideChanges.LoadXml(targetChangeAction.ActionData);
                string[] targetSideChangedFields = ExtractFieldRefNames(targetSideChanges);
                
                // drop the target-side updated fields from the source-side change
                // example: 
                // target side change includes Field1, Field2, Field3
                // source side change includes Field1, Field2, Field5, Field 6
                // By taking target, we want to migrate Field1, Field2, Field3 to source side
                // Then migrate Field5, Field 6 to target side, i.e. dropping Field1, Field2
                DropFields(conflict.ConflictedChangeAction.MigrationActionDescription, targetSideChangedFields);
                
                // update result for source change
                if (!ChangeContainsFieldUpdates(conflict.ConflictedChangeAction.MigrationActionDescription))
                {
                    conflict.ConflictedChangeAction.ChangeGroup.Status = ChangeStatus.Skipped;
                    conflict.ConflictedChangeAction.ChangeGroup.Save();
                }

                // since we will return ConflictResolutionType.UpdatedConflictedChangeAction, conflict manager will update the conflicted change action for us
                return true;
            }
        }

        private string[] ExtractFieldRefNames(XmlDocument targetSideChanges)
        {
            XmlNodeList fields = targetSideChanges.DocumentElement.SelectNodes("//Column");
            string[] retVal = new string[fields.Count];

            for(int i = 0; i < fields.Count; ++i)
            {
                XmlNode field = fields[i];
                var refAttr = field.Attributes["ReferenceName"];
                if (null != refAttr)
                {
                    retVal[i] = refAttr.Value;
                }
                else
                {
                    retVal[i] = string.Empty;
                }
            }

            return retVal;
        }

        private bool TakeSource(MigrationConflict conflict)
        {
            // find the target-side change action (should be a delta table entry)
            // SIDE NOTE: 
            //  upon detection of a wit edit/edit conflict, we create
            //  1. an edit/edit conflict for the source side change group/action
            //  2. a chainonconflictconflict for the target side's (using the source-side conflict id as the scopehint)
            // look up for target-side conflicted change action id
            long targetChangeActionId;
            bool retVal = WITEditEditConflictType.TryGetConflictedTargetChangeActionId(conflict.ConflictDetails, out targetChangeActionId);
            if (!retVal)
            {
                // backward compatibility:
                // old-style edit/edit conflict details does not include target change action id
                // in that case, we can't find a change action to complete the anlaysis
                return false;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Debug.Assert(conflict.ConflictedChangeAction != null, "Edit/Edit conflict ConflictedCangeAction is NULL");

                // Extract all fields updated in the target change action
                string[] sourceSideChangedFields = ExtractFieldRefNames(conflict.ConflictedChangeAction.MigrationActionDescription);

                // find the target-side change action (should be a delta table entry)
                var changeActionQuery = context.RTChangeActionSet.Where(a => a.ChangeActionId == targetChangeActionId);
                if (changeActionQuery.Count() != 1)
                {
                    return false;
                }
                RTChangeAction targetChangeAction = changeActionQuery.First();

                // drop the source-side updated fields from the target-side change
                // example: 
                // source side change includes Field1, Field2, Field3
                // target side change includes Field1, Field2, Field5, Field 6
                // By taking source, we want to migrate Field1, Field2, Field3 to target side
                // Then migrate Field5, Field 6 to source side, i.e. dropping Field1, Field2
                XmlDocument targetSideChanges = new XmlDocument();
                targetSideChanges.LoadXml(targetChangeAction.ActionData);
                DropFields(targetSideChanges, sourceSideChangedFields);
                
                // update result for target change
                targetChangeAction.ActionData = targetSideChanges.OuterXml;
                SkipChangeWithZeroFieldUpdates(targetChangeAction);

                context.TrySaveChanges();
                return true;
            }
        }

        private bool TryIgnoreNonFieldConflict(MigrationConflict conflict)
        {
            long targetChangeActionId;
            bool retVal = WITEditEditConflictType.TryGetConflictedTargetChangeActionId(conflict.ConflictDetails, out targetChangeActionId);

            if (!retVal)
            {
                // backward compatibility:
                // old-style edit/edit conflict details does not include target change action id
                // in that case, we can't find a change action to complete the anlaysis
                return false;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                // find the target-side change action (should be a delta table entry)
                var targetChangeActionQuery = context.RTChangeActionSet.Where(a => a.ChangeActionId == targetChangeActionId);
                if (targetChangeActionQuery.Count() != 1)
                {
                    return false;
                }
                RTChangeAction targetChangeAction = targetChangeActionQuery.First();

                XmlDocument targetSideChanges = new XmlDocument();
                targetSideChanges.LoadXml(targetChangeAction.ActionData);

                // if there are edits on the same field, we *cannot* ignore the conflict
                return !(EditOnSameField(conflict.ConflictedChangeAction.MigrationActionDescription, targetSideChanges));
            }
        }

        private bool EditOnSameField(
            XmlDocument sourceSideChanges, 
            XmlDocument targetSideChanges)
        {
            var srcActionFieldChanges = WorkItemField.ExtractFieldChangeDetails(sourceSideChanges);
            var tgtActionFieldChanges = WorkItemField.ExtractFieldChangeDetails(targetSideChanges);

            return ContainsConflictFieldChange(srcActionFieldChanges, tgtActionFieldChanges);
        }

        private bool ContainsConflictFieldChange(
            ReadOnlyCollection<WorkItemField> srcActionFieldChanges, 
            ReadOnlyCollection<WorkItemField> tgtActionFieldChanges)
        {
            foreach (WorkItemField srcField in srcActionFieldChanges)
            {
                foreach (WorkItemField tgtField in tgtActionFieldChanges)
                {
                    if (string.Equals(srcField.FieldName, tgtField.FieldName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // we always recognize an edit/edit conflict when the same/corresponding field is touched on both side,
                        // REGARDLESS of the changed field value
                        return true;

                        //if (srcField.FieldValue == tgtField.FieldValue)
                        //{
                        //    continue;
                        //}

                        //if (string.IsNullOrEmpty(srcField.FieldValue) || string.IsNullOrEmpty(tgtField.FieldValue))
                        //{
                        //    return true;
                        //}

                        //if (!string.Equals(srcField.FieldValue, tgtField.FieldValue, StringComparison.InvariantCultureIgnoreCase))
                        //{
                        //    return true;
                        //}
                    }
                }
            }

            return false;
        }

        #endregion

        private struct RuleScope
        {
            public string SourceItemId { get; set; }
            public string SourceItemRevision { get; set; }
            public string TargetItemId { get; set; }
            public string TargetItemRevision { get; set; }
        }
    }
}
