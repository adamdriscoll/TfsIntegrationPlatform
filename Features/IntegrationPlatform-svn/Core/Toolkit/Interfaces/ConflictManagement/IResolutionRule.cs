// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Defines a generalized resolution rule factory that a class implements when defining conflict
    /// resolution rules.
    /// </summary>
    /// <remarks>This interface is implemented by types that are used to define conflict resolution
    /// rules.</remarks>
    /// <seealso cref="ResolutionAction"/>
    public interface IResolutionRuleFactory
    {
        /// <summary>
        /// Creates a new <see cref="ConflictResolutionRule"/>.
        /// </summary>
        /// <param name="applicableScope">The scope as it applies to the <see cref="ConflictResolutionRule"/> being
        /// created.</param>
        /// <param name="description">The description of the <see cref="ConflictResolutionRule"/>.</param>
        /// <param name="actionData">The action data to be associated with the <see cref="ConflictResolutionRule"/> 
        /// defined as a collection of key/value pairs.</param>
        /// <returns>A new <see cref="ConflictResolutionRule"/>.</returns>
        /// <remarks>The NewRule method is implemented by types that are used to define conflict resolution rules.
        /// <para>This method is only a definition and must be implemented by a specific class to have effect.  The meaning
        /// of <paramref name="applicableScope"/>, <paramref name="description"/>, and <paramref name="actionData"/> 
        /// depend on the particular implementation.</para></remarks>
        ConflictResolutionRule NewRule(string applicableScope, string description, Dictionary<string, string> actionData);
    }
}
