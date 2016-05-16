// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCBasicPathScopeInterpreter: IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        public bool IsInScope(string scopeToCheck, string scope)
        {
            if (string.IsNullOrEmpty(scopeToCheck))
            {
                return false;
            }
            if (string.IsNullOrEmpty(scope))
            {
                return false;
            }

            return IsSubItem(scopeToCheck, scope);

        }

        /// <summary>
        /// Check if the item is the sub item of the parent path. 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool IsSubItem(String item, String parent)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            return item.StartsWith(parent, StringComparison.OrdinalIgnoreCase) &&
                   (item.Length == parent.Length || parent[parent.Length - 1] == '\\' || item[parent.Length] == '\\'
                   || parent[parent.Length - 1] == '/' || item[parent.Length] == '/');
        }

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
                return "A Version Controlled path specific to the migration endpoint.";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            hint = ScopeSyntaxHint;
            return true;
        }

    }

    public class StringScopeComparer : IComparer<ConflictResolutionRule>
    {
        public int Compare(ConflictResolutionRule x, ConflictResolutionRule y)
        {
            if (null == x)
            {
                throw new ArgumentNullException("x");
            }
            if (null == y)
            {
                throw new ArgumentNullException("y");
            }
            return (x.ApplicabilityScope.Length - y.ApplicabilityScope.Length);
        }
    }
}
