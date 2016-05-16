// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    /// <summary>
    /// Scope interpreter for ConflictType UnhandledChangeType.
    /// Expects comma-delimited ChangeTypes
    /// </summary>
    public class ChangeTypeScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        #region IApplicabilityScopeInterpreter Members

        /// <summary>
        /// Scope must be an exact match, e.g. Rename, SourceRename
        /// </summary>
        /// <param name="scopeToCheck"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public bool IsInScope(string scopeToCheck, string scope)
        {
            return scopeToCheck.Equals(scope);
        }

        /// <summary>
        /// Default StringScopeComparer
        /// </summary>
        public IComparer<ConflictResolutionRule> RuleScopeComparer
        {
            get
            {
                if (null == m_ruleScopeComparer)
                {
                    m_ruleScopeComparer = new StringScopeComparer();
                }
                return m_ruleScopeComparer;
            }
        }

        public string ScopeSyntaxHint
        {
            get
            {
                return "An existing combination of comma-delimited ChangeTypes.";
            }
        }

        /// <summary>
        /// Parses scope into bitwise ChangeType enum.  All tokens must be an enum.
        /// </summary>
        /// <param name="ruleScopeToValidate"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            string[] changeTypes = ruleScopeToValidate.Split(',');
            foreach (string changeType in changeTypes)
            {
                try
                {
                    int i;
                    if (int.TryParse(changeType, out i))
                    {
                        throw new Exception();
                    }
                    else
                    {
                        Enum.Parse(typeof(ChangeType), changeType);
                    }
                }
                catch
                {
                    hint = string.Format("The ChangeType '{0}' is invalid.", changeType);
                    return false;
                }
            }
            hint = "Valid ChangeType.";
            return true;
        }

        #endregion
    }
}
