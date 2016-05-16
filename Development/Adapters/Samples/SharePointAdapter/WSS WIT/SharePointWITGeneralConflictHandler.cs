//------------------------------------------------------------------------------
// <copyright file="SharePointWITGeneralConflictHandler.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------
namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System.Collections.Generic;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// 
    /// </summary>
    public class SharePointWITGeneralConflictHandler : IConflictHandler
    {
        #region IConflictHandler Members

        /// <summary>
        /// Determines whether this conflict handler can resolve the current conflict being handled.
        /// </summary>
        /// <param name="conflict">The <see cref="MigrationConflict"/> being handled.</param>
        /// <param name="rule">The <see cref="ConflictResolutionRule"/> that is being tested against
        /// <paramref name="conflict"/>.</param>
        /// <returns>
        /// 	<c>true</c> if this conflict handler can resolve the current conflict being handled;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>The CanResolve method is implemented by types that are used to handle conflicts.
        /// <para/>This method is only a definition and must be implemented by a specific class to have effect.  The meaning
        /// of <paramref name="conflict"/> and <paramref name="rule"/> depend on the particular implementation.
        /// <para/>In general, implementations of this method will determine whether the conflict's scope falls within the
        /// resolution rule's scope and whether or not there are any conflict resolution methods that can be successfully
        /// invoked.</remarks>
        /// <example>This example shows how a conflict handler for file attachments might be implemented.
        /// <code>
        /// public class FileAttachmentGeneralConflictHandler : IConflictHandler
        /// {
        /// #region IConflictHandler Members
        /// public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        /// {
        /// // If the conflict is not within the scope of the conflict resolution rule, then we can't resolve this conflict
        /// if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
        /// {
        /// return false;
        /// }
        /// // Can we resolve this conflict by dropping the attachment?
        /// if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        /// {
        /// return CanResolveByDroppingAttachment(conflict, rule);
        /// }
        /// return true;
        /// }
        /// public ConflictResolutionResult Resolve(MigrationConflict conflict, ConflictResolutionRule rule, out List&lt;MigrationAction&gt; actions)
        /// {
        /// actions = null;
        /// // Check to see if the conflict can be manually resolved
        /// if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
        /// {
        /// return ManualResolve(conflict, rule, out actions);
        /// }
        /// // Check to see if the conflict can be resolved by dropping an attachment
        /// else if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        /// {
        /// return ResolveByDroppingAttachment(conflict, rule, out actions);
        /// }
        /// return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        /// }
        /// public ConflictType ConflictTypeHandled
        /// {
        /// get;
        /// set;
        /// }
        /// #endregion
        /// // Private members implemented below...
        /// </code>
        /// </example>
        public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        {
            return ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope);
        }

        /// <summary>
        /// Attempts to resolve the migration conflict based on the provided <see cref="ConflictResolutionRule"/>.
        /// </summary>
        /// <param name="conflict">The <see cref="Microsoft.TeamFoundation.Migration"/> being resolved.</param>
        /// <param name="rule">The <see cref="ConflictResolutionRule"/> that is to be applied to <paramref name="conflict"/>.</param>
        /// <param name="actions">A list of <see cref="MigrationAction"/> objects specific to the type of conflict
        /// manager in use.</param>
        /// <returns>
        /// An instance of <see cref="ConflictResolutionResult"/> providing the result of the method call.
        /// </returns>
        /// <remarks>The Resolve method is implemented by types that are used to handle conflicts.
        /// <para/>This method is only a definition and must be implemented by a specific class to have effect.  The meaning
        /// of <paramref name="conflict"/>, <paramref name="rule"/>, and <paramref name="actions"/> depend on the particular
        /// implementation.
        /// <para/>In general, implementations of this method will determine whether the conflict's scope falls within the
        /// resolution rule's scope and whether or not there are any conflict resolution methods that can be successfully
        /// invoked.</remarks>
        /// <seealso cref="ConflictResolutionResult"/>
        /// <example>This example shows how a conflict handler for file attachments might be implemented.
        /// <code>
        /// public class FileAttachmentGeneralConflictHandler : IConflictHandler
        /// {
        /// #region IConflictHandler Members
        /// public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        /// {
        /// // If the conflict is not within the scope of the conflict resolution rule, then we can't resolve this conflict
        /// if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
        /// {
        /// return false;
        /// }
        /// // Can we resolve this conflict by dropping the attachment?
        /// if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        /// {
        /// return CanResolveByDroppingAttachment(conflict, rule);
        /// }
        /// return true;
        /// }
        /// public ConflictResolutionResult Resolve(MigrationConflict conflict, ConflictResolutionRule rule, out List&lt;MigrationAction&gt; actions)
        /// {
        /// actions = null;
        /// // Check to see if the conflict can be manually resolved
        /// if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
        /// {
        /// return ManualResolve(conflict, rule, out actions);
        /// }
        /// // Check to see if the conflict can be resolved by dropping an attachment
        /// else if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        /// {
        /// return ResolveByDroppingAttachment(conflict, rule, out actions);
        /// }
        /// return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        /// }
        /// public ConflictType ConflictTypeHandled
        /// {
        /// get;
        /// set;
        /// }
        /// #endregion
        /// // Private members implemented below...
        /// </code>
        /// </example>
        public ConflictResolutionResult Resolve(MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;

            if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
            {
                return ManualResolve(out actions);
            }

            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }

        /// <summary>
        /// Gets or sets the <see cref="ConflictType"/> being handled.
        /// </summary>
        /// <value></value>
        public ConflictType ConflictTypeHandled
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Resolves the conflict manually.
        /// </summary>
        /// <param name="actions">The actions.</param>
        /// <returns></returns>
        private static ConflictResolutionResult ManualResolve(out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(true, ConflictResolutionType.Other);
        }


        public ConflictResolutionResult Resolve(System.ComponentModel.Design.IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions)
        {
            actions = null;
            return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        }
    }
}
