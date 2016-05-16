// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010VCAdapter
{
    class UnhandledChangeTypeConflictHandler : IConflictHandler
    {
        private IEnumerable<Microsoft.TeamFoundation.VersionControl.Client.ChangeType> m_validChangeTypes;
        public UnhandledChangeTypeConflictHandler(IEnumerable<Microsoft.TeamFoundation.VersionControl.Client.ChangeType> validChangeTypes)
        {
            m_validChangeTypes = validChangeTypes;
        }

        #region IConflictHandler Members

        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            if (rule.ActionRefNameGuid.Equals(new UnhandledChangeTypeMapAction().ReferenceName))
            {
                string mapFrom = rule.ApplicabilityScope;
                string mapTo = rule.DataField[0].FieldValue;
                ConflictResolutionResult result;
                if (ValidateRuleData(mapTo, mapFrom))
                {
                    result = new ConflictResolutionResult(true, ConflictResolutionType.Other);
                    result.Comment = mapTo;
                }
                else
                {
                    result = new ConflictResolutionResult(false, ConflictResolutionType.Other);
                    result.Comment = "MapTo field is invalid.";
                }
                return result;
            }
            else if (rule.ActionRefNameGuid.Equals(new UnhandledChangeTypeSkipAction().ReferenceName))
            {
                return new ConflictResolutionResult(true, ConflictResolutionType.Other); // do not set comment as MapTo
            }
            else
            {
                return new ConflictResolutionResult(false, ConflictResolutionType.UnknownResolutionAction);
            }
        }

        private bool ValidateRuleData(string mapTo, string mapFrom)
        {
            Microsoft.TeamFoundation.VersionControl.Client.ChangeType changeType;
            if (String2ChangeType(mapTo, out changeType))
            {
                Microsoft.TeamFoundation.VersionControl.Client.ChangeType source;
                if (String2ChangeType(mapFrom, out source))
                {
                    return (changeType & source) == changeType;
                }
                else
                {
                    Debug.Fail("Should not happen.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool String2ChangeType(string str, out Microsoft.TeamFoundation.VersionControl.Client.ChangeType ret)
        {
            ret = Microsoft.TeamFoundation.VersionControl.Client.ChangeType.None;

            if (string.IsNullOrEmpty(str))
            {
                return false;
            } 
            
            string[] changeTypes = str.Split(',');
            try
            {
                foreach (string changeType in changeTypes)
                {
                    ret |= (Microsoft.TeamFoundation.VersionControl.Client.ChangeType)Enum.Parse(typeof(Microsoft.TeamFoundation.VersionControl.Client.ChangeType), changeType);
                }
                ret &= ~Microsoft.TeamFoundation.VersionControl.Client.ChangeType.None;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion
    }
}
