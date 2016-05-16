// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Provides the default conflict resolution rule scope interpreter.
    /// </summary>
    /// <remarks>The standard syntax of the scope string is a Unix-like path.  For
    /// example: /root/a/b/c.</remarks>
    public class BasicPathScopeInterpreter : IApplicabilityScopeInterpreter
    {
        private BasicPathScopeComparer m_ruleScopeComparer;

        /// <summary>
        /// Determines whether a given path falls within the specified scope.
        /// </summary>
        /// <param name="scopeToCheck">The scope value to check.</param>
        /// <param name="scope">The scope constraint being checked.  Specify a value of
        /// string.Empty if there is no scope constraint.</param>
        /// <returns><c>true</c> if the <paramref name="scopeToCheck"/> is determined to fall within <paramref name="scope"/>;
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>In general, <paramref name="scope"/> is considered to be within the scope of <paramref name="scopeToCheck"/>
        /// when both values are well formed and <paramref name="scope"/> is an empty string, or each element in
        /// <paramref name="scope"/> matches the corresponding element in <paramref name="scopeToCheck"/> (based on position).</remarks>
        /// <exception cref="ArgumentException"><paramref name="scopeToCheck"/> or <paramref name="scope"/> is <c>null</c>.</exception>
        /// <example>The following example shows how to determine if a path is within the scope of another path.
        /// <code>
        /// string scopeToCheck = @"/a/b/c";
        /// string scope = @"/a/b";
        /// BasicPathScopeInterpreter comparer = new BasicPathScopeInterpreter();
        /// 
        /// if (comparer.IsInScope(scopeToCheck, scope))
        /// {
        ///   // It's in scope, do something...
        /// }
        /// </code>
        /// </example>
        public bool IsInScope(string scopeToCheck, string scope)
        {
            if (!IsScopeStringWellFormed(scopeToCheck.Trim()))
            {
                throw new ArgumentException(string.Format(
                    "{0} is not a well-formed basic scope string.", "scopeToCheck"));
            }

            if (!IsScopeStringWellFormed(scope.Trim()))
            {
                throw new ArgumentException(string.Format(
                    "{0} is not a well-formed basic scope string.", "scope"));
            }

            scopeToCheck = scopeToCheck.Trim();
            scope = scope.Trim();

            if (string.IsNullOrEmpty(scope) || scope.Equals(PathSeparator, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(scopeToCheck))
            {
                return false;
            }

            string[] scopeToCheckPath = scopeToCheck.Trim().Split(PathSeparator.ToCharArray());
            string[] scopePath = scope.Trim().Split(PathSeparator.ToCharArray());

            if (scopePath.Length > scopeToCheckPath.Length)
            {
                return false;
            }

            bool isInScope = true;
            for (int i = 0; i < scopePath.Length; ++i)
            {
                if (!scopeToCheckPath[i].Equals(scopePath[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    isInScope = false;
                    break;
                }
            }

            return isInScope;
        }

        /// <summary>
        /// Gets the rule scope comparer for this instance.
        /// </summary>
        /// <remarks>This method is used in conjunction with sorting methods for arrays and collections.   It provides a way
        /// to customize the sort order of a collection.</remarks>
        /// <example>This example shows how to sort a set of conflict resolution rules based on the 
        /// returned <see cref="RuleScopeComparer"/>
        /// <code>
        /// resultRules.Sort(conflictType.ScopeInterpreter.RuleScopeComparer);
        /// </code>
        /// </example>
        /// <seealso cref="IComparable{T}"/>
        public IComparer<ConflictResolutionRule> RuleScopeComparer
        {
            get
            {
                if (null == m_ruleScopeComparer)
                {
                    m_ruleScopeComparer = new BasicPathScopeComparer();
                }
                return m_ruleScopeComparer;
            }
        }


        public string ScopeSyntaxHint
        {
            get 
            {
                return "UNIX-style path, e.g. /a/b/c"; 
            }
        }

        /// <summary>
        /// Determines whether the specified scope string is considered to be well formed.
        /// </summary>
        /// <param name="scopeString">The scope string being checked.</param>
        /// <returns><c>true</c> if the specified scope string is well foromed; otherwise, <c>false</c>.</returns>
        /// <remarks>A scope string is considered to be well formed if it is an empty string or it starts with
        /// the defined path separator (by default this is the forward slash (/)).</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="scopeString"/> is <c>null</c>.</exception>
        static internal bool IsScopeStringWellFormed(string scopeString)
        {
            if (scopeString == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(scopeString))
            {
                return true;
            }

            if (!scopeString.Trim().StartsWith(PathSeparator))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the string separator used to separate path strings within a basic path.
        /// </summary>
        /// <remarks>By default, this value is a forward slash (/).  For example, a typical path
        /// follows the format "/root/a/b/c".</remarks>
        public static string PathSeparator
        {
            get
            {
                return "/";
            }
        }

        public bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint)
        {
            if (IsScopeStringWellFormed(ruleScopeToValidate))
            {
                hint = MigrationToolkitResources.ScopeIsValid;
                return true;
            }
            else
            {
                hint = ScopeSyntaxHint;
                return false;
            }
        }
    }

    /// <summary>
    /// Provides the default scope comparer for all conflict types.
    /// </summary>
    public class BasicPathScopeComparer : IComparer<ConflictResolutionRule>
    {
        #region IComparer<IResolutionRule> Members

        /// <summary>
        /// Compares the scopes of two conflict resolution rules and returns a value indicating whether one is less than, 
        /// equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first <see cref="ConflictResolutionRule"/> to compare.</param>
        /// <param name="y">The second <see cref="ConflictResolutionRule"/> to compare.</param>
        /// <returns>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term><description>Condition</description>
        /// </listheader>
        /// <item>
        /// <term>Less than zero</term><description><paramref name="x"/> is less than <paramref name="y"/>.</description>
        /// </item>
        /// <item>
        /// <term>Zero</term><description><paramref name="x"/> equals <paramref name="y"/>.</description>
        /// </item>
        /// <item>
        /// <term>Greater than zero</term><description><paramref name="x"/> is greater than <paramref name="y"/>.</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <seealso cref="ConflictResolutionRule"/>
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

            string scopeX = x.ApplicabilityScope;
            string scopeY = y.ApplicabilityScope;

            if (null == scopeX)
            {
                throw new ArgumentNullException("x.ApplicabilityScope");
            }

            if (null == scopeY)
            {
                throw new ArgumentNullException("y.ApplicabilityScope");
            }


            if (!BasicPathScopeInterpreter.IsScopeStringWellFormed(scopeX))
            {
                throw new ArgumentException(string.Format(
                    "{0} is not a well-formed basic scope string.", "x.ApplicableScope"));
            }

            if (!BasicPathScopeInterpreter.IsScopeStringWellFormed(scopeY))
            {
                throw new ArgumentException(string.Format(
                    "{0} is not a well-formed basic scope string.", "y.ApplicableScope"));
            }

            scopeX = scopeX.Trim();
            scopeY = scopeY.Trim();

            if (scopeX == string.Empty)
            {
                if (scopeY == string.Empty)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            string[] scopeXPath = scopeX.Split(BasicPathScopeInterpreter.PathSeparator.ToCharArray());
            string[] scopeYPath = scopeY.Split(BasicPathScopeInterpreter.PathSeparator.ToCharArray());

            if (scopeXPath.Length < scopeYPath.Length)
            {
                return 1;
            }

            if (scopeXPath.Length > scopeYPath.Length)
            {
                return -1;
            }

            for (int i = 0; i < scopeXPath.Length; ++i)
            {
                int rslt = string.Compare(scopeXPath[i], scopeYPath[i], true);

                if (rslt != 0)
                {
                    return rslt;
                }
            }

            return 0;
        }

        #endregion
    }
}
