// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class GlobalScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        public bool IsInScope(string scopeToCheck, string scope)
        {
            return true;
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
                return "Any string is acceptable - this scope is always evaluated to be true."; 
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            hint = MigrationToolkitResources.ScopeIsValid;
            return true;
        }

    }

    /// <summary>
    /// This is a changegroup scope interprete that requires an exact integer map. 
    /// </summary>
    public class ChangeGroupScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        public bool IsInScope(string scopeToCheck, string scope)
        {
            if (string.IsNullOrEmpty(scopeToCheck) || string.IsNullOrEmpty(scope))
            {
                return true;
            }

            return scopeToCheck.Equals(scope, StringComparison.InvariantCultureIgnoreCase);
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
                return "This is a changegroup scope interprete that requires an exact integer map."; 
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            hint = MigrationToolkitResources.ScopeIsValid;

            if (string.IsNullOrEmpty(ruleScopeToValidate))
            {
                return true;
            }

            int changegroupIntVal;
            bool isValid = int.TryParse(ruleScopeToValidate.Trim(), out changegroupIntVal);

            if (!isValid)
            {
                hint = string.Format(MigrationToolkitResources.InvalidScopeIntValueExpected, ruleScopeToValidate.Trim());
            }

            return isValid;
        }
    }

    /// <summary>
    /// This is a scope interpreter that requires an exact string match. 
    /// </summary>
    public class StringScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        public bool IsInScope(string scopeToCheck, string scope)
        {
            if (string.IsNullOrEmpty(scopeToCheck) || string.IsNullOrEmpty(scope))
            {
                return true;
            }

            if (scope.Equals("*"))
            {
                return true;
            }

            return scopeToCheck.Equals(scope, StringComparison.InvariantCultureIgnoreCase);
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
                return "This is a scope interpreter that requires an exact string match.";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            hint = MigrationToolkitResources.ScopeIsValid;

            // Any string is valid.
            return true;
        }
    }

    /// <summary>
    /// This is an integer range scope intepreter:
    /// 1. 0-123 - any integer from 0 to 123 inclusively.
    /// 2. 2 - an exact match of integer 2
    /// </summary>
    public class IntegerRangeScopeInterpreter : IApplicabilityScopeInterpreter
    {
        public static readonly char RangeSymbol = '-';

        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        public bool IsInScope(string scopeToCheck, string scope)
        {
            if (string.IsNullOrEmpty(scopeToCheck) || string.IsNullOrEmpty(scope))
            {
                return true;
            }

            try
            {
                int rangeSymbolIndex = scope.IndexOf(RangeSymbol);
                if (rangeSymbolIndex < 0)
                {
                    //exact match
                    return (int.Parse(scopeToCheck) == int.Parse(scope));
                }
                else
                {
                    int scopeStart = int.Parse(scope.Substring(0, rangeSymbolIndex));
                    int scopeEnd = int.Parse(scope.Substring(rangeSymbolIndex + 1));
                    int scopeToCheckNumber = int.Parse(scopeToCheck);
                    return ((scopeStart <= scopeToCheckNumber) && (scopeToCheckNumber <= scopeEnd));
                }
            }
            catch (FormatException)
            {
                return false;
            }
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
                return
@"This is an integer range scope intepreter:
  1. 0-123 - any integer from 0 to 123 inclusively.
  2. 2 - an exact match of integer 2
";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            bool isValid = false;
            while (true)
            {
                if (string.IsNullOrEmpty(ruleScopeToValidate))
                {

                    isValid = true;
                    break;
                }

                int rangeSymbolIndex = ruleScopeToValidate.IndexOf(RangeSymbol);
                if (rangeSymbolIndex < 0)
                {
                    //exact match
                    int exactMatchScopeIntVal;
                    isValid = int.TryParse(ruleScopeToValidate, out exactMatchScopeIntVal);
                    break;
                }
                else
                {
                    string scopeStartStr = ruleScopeToValidate.Substring(0, rangeSymbolIndex);
                    string scopeEndStr = ruleScopeToValidate.Substring(rangeSymbolIndex + 1);

                    int scopeStart;
                    isValid = int.TryParse(scopeStartStr, out scopeStart);
                    if (!isValid)
                    {
                        break;
                    }

                    int scopeEnd;
                    isValid = int.TryParse(scopeEndStr, out scopeEnd);
                    break;
                }
            }

            if (isValid)
            {
                hint = MigrationToolkitResources.ScopeIsValid;
            }
            else
            {
                hint = ScopeSyntaxHint;
            }

            return isValid;
        }

    }

    /// <summary>
    /// This is a scope that combines path and integer range. 
    /// Syntax: $/teamproject1/folder;112-200
    /// </summary>
    public class VCPathAndIntegerRangeScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;
        VCBasicPathScopeInterpreter m_basicPathScope = new VCBasicPathScopeInterpreter();
        IntegerRangeScopeInterpreter m_integerRangeScope = new IntegerRangeScopeInterpreter();

        /*
         * test cases:
         * $/a.txt, $/a.txt = true
         * $/a.txt, $/a.txt; = true
         * $/a.txt, $/a.txt;123 = false
         * $/a.txt;, $/a.txt = true
         * $/a.txt;, $/a.txt; = true
         * $/a.txt;, $/a.txt;123 = false
         * $/a.txt;123, $/a.txt = true
         * $/a.txt;123, $/a.txt; = true
         * $/a.txt;123, $/a.txt;123 = true
         */
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

            scopeToCheck = scopeToCheck.TrimEnd(';');
            scope = scope.TrimEnd(';');

            if (scope.LastIndexOf(';') < 0)
            {
                // Scope is path only, no integer range
                string scopeToCheckPath;
                if (scopeToCheck.LastIndexOf(';') < 0)
                {
                    scopeToCheckPath = scopeToCheck;
                }
                else
                {
                    scopeToCheckPath = scopeToCheck.Substring(0, scopeToCheck.LastIndexOf(';'));
                }
                return m_basicPathScope.IsInScope(scopeToCheckPath, scope);
            }
            else
            {
                if (scopeToCheck.LastIndexOf(';') < 0)
                {
                    // scope is more specific
                    return false;
                }
                string scopePath = scope.Substring(0, scope.LastIndexOf(';'));
                string scopeIntegerRange = scope.Substring(scope.LastIndexOf(';') + 1);
                string scopeToCheckPath = scopeToCheck.Substring(0, scopeToCheck.LastIndexOf(';'));
                string scopeToCheckIntegerRange = scopeToCheck.Substring(scopeToCheck.LastIndexOf(';') + 1);
                if (! m_basicPathScope.IsInScope(scopeToCheckPath, scopePath))
                {
                    return false;
                }
                else
                {
                    return m_integerRangeScope.IsInScope(scopeToCheckIntegerRange, scopeIntegerRange);
                }
            }
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
                return "A Version Controlled path and a version specific to the migration endpoint. For example $/teama/folder;12311";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            if (ruleScopeToValidate.LastIndexOf(';') < 0)
            {
                return m_basicPathScope.IsResolutionRuleScopeValid(ruleScopeToValidate, out hint);
            }
            else
            {
                string scopePath = ruleScopeToValidate.Substring(0, ruleScopeToValidate.LastIndexOf(';'));
                string scopeInteger = ruleScopeToValidate.Substring(ruleScopeToValidate.LastIndexOf(';') + 1);
                if (m_basicPathScope.IsResolutionRuleScopeValid(scopePath, out hint) && m_integerRangeScope.IsResolutionRuleScopeValid(scopeInteger, out hint))
                {
                    hint = ScopeSyntaxHint;
                    return true;
                }
                hint = ScopeSyntaxHint;
                return false;
            }
        }

    }

    /// <summary>
    /// This is a scope that combines path and a string postfix. 
    /// The first part is a path scope. The 2nd part requires a exact string match if specified.
    /// Syntax: $/teamproject1/folder;12-31-2001
    /// </summary>
    public class VCPathAndPostfixScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;
        VCBasicPathScopeInterpreter m_basicPathScope = new VCBasicPathScopeInterpreter();

        /*
         * test cases:
         * $/a.txt, $/a.txt = true
         * $/a.txt, $/a.txt; = true
         * $/a.txt, $/a.txt;12-31-2001 = false
         * $/a.txt;, $/a.txt = true
         * $/a.txt;, $/a.txt; = true
         * $/a.txt;, $/a.txt;12-31-2001 = false
         * $/a.txt;12-31-2001, $/a.txt = true
         * $/a.txt;12-31-2001, $/a.txt; = true
         * $/a.txt;12-31-2001, $/a.txt;12-31-2001 = true
         * $/a.txt;12-31-2001, $/a.txt;12-30-2001 = false
         */
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

            scopeToCheck = scopeToCheck.TrimEnd(';');
            scope = scope.TrimEnd(';');

            if (scope.LastIndexOf(';') < 0)
            {
                // Scope is path only, no postfix
                string scopeToCheckPath;
                if (scopeToCheck.LastIndexOf(';') < 0)
                {
                    scopeToCheckPath = scopeToCheck;
                }
                else
                {
                    scopeToCheckPath = scopeToCheck.Substring(0, scopeToCheck.LastIndexOf(';'));
                }
                return m_basicPathScope.IsInScope(scopeToCheckPath, scope);
            }
            else
            {
                if (scopeToCheck.LastIndexOf(';') < 0)
                {
                    // scope is more specific
                    return false;
                }
                string scopePath = scope.Substring(0, scope.LastIndexOf(';'));
                string scopePostfix = scope.Substring(scope.LastIndexOf(';') + 1);
                string scopeToCheckPath = scopeToCheck.Substring(0, scopeToCheck.LastIndexOf(';'));
                string scopeToCheckPostfix = scopeToCheck.Substring(scopeToCheck.LastIndexOf(';') + 1);
                if (!m_basicPathScope.IsInScope(scopeToCheckPath, scopePath))
                {
                    return false;
                }
                else
                {
                    return string.Equals(scopePostfix, scopeToCheckPostfix, StringComparison.Ordinal);
                }
            }
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
                return "A Version Controlled path and a version specific to the migration endpoint. For example $/teama/folder;12311";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            if (ruleScopeToValidate.LastIndexOf(';') < 0)
            {
                return m_basicPathScope.IsResolutionRuleScopeValid(ruleScopeToValidate, out hint);
            }
            else
            {
                string scopePath = ruleScopeToValidate.Substring(0, ruleScopeToValidate.LastIndexOf(';'));
                if (m_basicPathScope.IsResolutionRuleScopeValid(scopePath, out hint))
                {
                    hint = ScopeSyntaxHint;
                    return true;
                }
                hint = ScopeSyntaxHint;
                return false;
            }
        }

    }

    public class LabelScopeInterpreter : IApplicabilityScopeInterpreter
    {
        IComparer<ConflictResolutionRule> m_ruleScopeComparer;

        public bool IsInScope(string scopeToCheck, string scope)
        {
            // Label conflicts are always resolved individually
            return false;
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
                return "Any string is acceptable - this scope is always evaluated to be false.";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            hint = MigrationToolkitResources.ScopeIsValid;
            return true;
        }

    }
}
