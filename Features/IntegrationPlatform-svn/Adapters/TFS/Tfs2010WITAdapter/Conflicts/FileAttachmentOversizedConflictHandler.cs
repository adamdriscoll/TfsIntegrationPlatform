// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class FileAttachmentOversizedConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
            {
                return false;
            }

            if (rule.ActionRefNameGuid.Equals(new FileAttachmentOversizedConflictDropAttachmentAction().ReferenceName))
            {
                return CanResolveByDroppingAttachment(conflict, rule);
            }

            return true;
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(conflict, rule, out actions);
            }

            else if (rule.ActionRefNameGuid.Equals(new FileAttachmentOversizedConflictDropAttachmentAction().ReferenceName))
            {
                return ResolveByDroppingAttachment(conflict, rule, out actions);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion

        private bool CanResolveByIncreasingMaxAttchSetting(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            throw new NotImplementedException();
        }

        private ConflictResolutionResult ResolveByIncreasingMaxAttchSetting(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            throw new NotImplementedException();
        }

        private ConflictResolutionResult ResolveByDroppingAttachment(
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.SuppressedConflictedChangeAction);
        }

        private ConflictResolutionResult ManualResolve(
            MigrationConflict conflict, 
            ConflictResolutionRule rule, 
            out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }

        private bool CanResolveByDroppingAttachment(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            if (conflict.ConflictedChangeAction == null)
            {
                throw new InvalidOperationException();
            }

            if (conflict.ConflictedChangeAction.MigrationActionDescription == null)
            {
                throw new InvalidOperationException();
            }

            var actionDataKeys = new FileAttachmentOversizedConflictDropAttachmentAction().ActionDataKeys;
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
                    TraceManager.TraceInformation(
                        TfsWITAdapterResources.ErrorResolutionRuleContainsInvalidDataField,
                        rule.RuleReferenceName, df.FieldName);
                    return false;
                }
            }

            string minFileDropSizeStr = rule.DataFieldDictionary[FileAttachmentOversizedConflictDropAttachmentAction.DATAKEY_MIN_FILE_SIZE_TO_DROP];
            int minFileDropSize;
            if (!int.TryParse(minFileDropSizeStr, out minFileDropSize))
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorResolutionRuleContainsInvalidFieldValue,
                                             FileAttachmentOversizedConflictDropAttachmentAction.DATAKEY_MIN_FILE_SIZE_TO_DROP,
                                             rule.RuleReferenceName,
                                             minFileDropSizeStr);
            }

            string fileLengthStr = conflict.ConflictedChangeAction.MigrationActionDescription.DocumentElement.FirstChild.Attributes["Length"].Value;
            int fileLength;
            if (!int.TryParse(fileLengthStr, out fileLength))
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorMissingInformationInActionDescription,
                                             "Length", conflict.ConflictedChangeAction.ActionId);
            }

            return fileLength >= minFileDropSize;
        }

    }
}
