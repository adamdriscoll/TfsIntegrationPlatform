// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Defines a generalized scope interpreter that a class implements to determine whether
    /// a given value falls within a specific scope.
    /// </summary>
    /// <remarks>This interface is implemented by types that are used to determine scope membership.</remarks>
    public interface IApplicabilityScopeInterpreter
    {
        /// <summary>
        /// Determines whether a given value falls within the specified scope.
        /// </summary>
        /// <param name="scopeToCheck">The scope value to check.</param>
        /// <param name="scope">The scope.</param>
        /// <returns><c>true</c> if the <paramref name="scopeToCheck"/> is determined to fall within <paramref name="scope"/>; 
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>The IsInScope method is implemented by types that are used to determine scope membership.
        /// <para>This method is only a definition and must be implemented by a specific class to have effect.  The meaning
        /// of <paramref name="scopeToCheck"/> and <paramref name="scope"/> depend on the particular implementation.</para></remarks>
        /// <example>
        /// <code>
        /// // Determine if a given integer value is within scope
        /// IntegerRangeScopeInterpreter target = new IntegerRangeScopeInterpreter(); 
        ///
        /// string scopeToCheck = "18"; 
        /// string scope = "1-20"; 
        ///
        /// if (target.IsInScope(scopeToCheck, scope))
        /// {
        ///    // Do something...
        /// }
        /// </code>
        /// </example>
        bool IsInScope(string scopeToCheck, string scope);

        /// <summary>
        /// Gets the rule scope comparer for this instance.
        /// </summary>
        /// <value>The rule scope comparer specific to this implementation.</value>
        /// <remarks>This interface is used in conjunction with sorting methods for arrays and collections.   It provides a way 
        /// to customize the sort order of a collection.</remarks>
        /// <example>
        /// <code>
        /// // Sort a set of conflict resolution rules based on the returned RuleScopeComparer
        /// resultRules.Sort(conflictType.ScopeInterpreter.RuleScopeComparer);
        /// </code>
        /// </example>
        /// <seealso cref="IComparable{T}"/>
        IComparer<ConflictResolutionRule> RuleScopeComparer
        {
            get;
        }

        /// <summary>
        /// Gets the syntax hint that is expected by this Scope Interpreter
        /// </summary>
        string ScopeSyntaxHint
        {
            get;
        }

        /// <summary>
        /// Validates the syntax of a scope
        /// </summary>
        /// <param name="ruleScopeToValidate"></param>
        /// <param name="hint">Suggestion or hint on why a rule does not pass the validation</param>
        /// <returns>True if validation passes; false otherwise.</returns>
        bool IsResolutionRuleScopeValid(string ruleScopeToValidate, out string hint);
    }
}
