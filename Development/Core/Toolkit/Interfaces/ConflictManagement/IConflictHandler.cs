// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Specifies the conflict resolution type.
    /// </summary>
    public enum ConflictResolutionType
    {
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        UpdatedConflictedChangeAction,
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        CreatedNewChangeActions,
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        SuppressedConflictedChangeAction,
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        ChangeMappingInConfiguration,
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        SuppressedConflictedChangeGroup,
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        SkipConflictedChangeAction,
        /// <summary>
        /// The resolution action is unknown.
        /// </summary>
        UnknownResolutionAction,
        /// <summary>
        /// TODO: Provide specific definition.
        /// </summary>
        UpdatedConflictedLinkChangeAction,
        /// <summary>
        /// The resolution action should be retried.
        /// </summary>
        Retry,
        /// <summary>
        /// A resolution action not specific to <see cref="ConflictResolutionType"/> was used.
        /// </summary>
        Other,
        /// <summary>
        /// A resolution action that updates the session group configuration.
        /// </summary>
        UpdatedConfiguration,
        /// <summary>
        /// A resolution action that addes new link change actions.
        /// </summary>
        CreatedUnlockLinkChangeActions,
        /// <summary>
        /// A resolution action that schedules the conflicted actions to be retried in the following session trip.
        /// </summary>
        ScheduledForRetry,
        /// <summary>
        /// A resolution action that resolve the conflicted actions automatically.
        /// </summary>
        AutoResolve,
        /// <summary>
        /// A resolution action that supress the other side's change action in content conflict
        /// </summary>
        SuppressOtherSideChangeAction
    }

    /// <summary>
    /// Defines a set of properties describing the result of a conflict resolution.
    /// </summary>
    public class ConflictResolutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResolutionResult"/> class.
        /// </summary>
        /// <param name="resolved">Specifies whether or not the conflict was resolved.</param>
        /// <param name="type">The conflict resolution type.</param>
        public ConflictResolutionResult(bool resolved, ConflictResolutionType type)
        {
            Resolved = resolved;
            ResolutionType = type;
        }

        /// <summary>
        /// Gets or sets the conflict resolution type.
        /// </summary>
        public ConflictResolutionType ResolutionType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the conflict has been resolved.
        /// </summary>
        public bool Resolved
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a comments to be associated with the conflict resolution.
        /// </summary>
        public string Comment
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ID of the conflict.
        /// </summary>
        public int ConflictInternalId
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Defines a generalized conflict handler that a class implements for handling conflicts.
    /// </summary>
    /// <remarks>This interface is implemented by types that are used to handle conflicts.</remarks>
    public interface IConflictHandler
    {
        /// <summary>
        /// Determines whether this conflict handler can resolve the current conflict being handled.
        /// </summary>
        /// <param name="conflict">The <see cref="MigrationConflict"/> being handled.</param>
        /// <param name="rule">The <see cref="ConflictResolutionRule"/> that is being tested against
        /// <paramref name="conflict"/>.</param>
        /// <returns><c>true</c> if this conflict handler can resolve the current conflict being handled; 
        /// otherwise, <c>false</c>.</returns>
        /// <remarks>The CanResolve method is implemented by types that are used to handle conflicts.
        /// <para/>This method is only a definition and must be implemented by a specific class to have effect.  The meaning
        /// of <paramref name="conflict"/> and <paramref name="rule"/> depend on the particular implementation.
        /// <para/>In general, implementations of this method will determine whether the conflict's scope falls within the
        /// resolution rule's scope and whether or not there are any conflict resolution methods that can be successfully 
        /// invoked.</remarks>
        /// <example>This example shows how a conflict handler for file attachments might be implemented.
        /// <code>
        ///  public class FileAttachmentGeneralConflictHandler : IConflictHandler
        ///  {
        ///    #region IConflictHandler Members
        /// 
        ///    public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        ///    {
        ///      // If the conflict is not within the scope of the conflict resolution rule, then we can't resolve this conflict
        ///      if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
        ///      {
        ///        return false;
        ///      }
        ///
        ///      // Can we resolve this conflict by dropping the attachment?
        ///      if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        ///      {
        ///        return CanResolveByDroppingAttachment(conflict, rule);
        ///      }
        ///
        ///      return true;
        ///    }
        ///
        ///    public ConflictResolutionResult Resolve(MigrationConflict conflict, ConflictResolutionRule rule, out List&lt;MigrationAction&gt; actions)
        ///    {
        ///      actions = null;
        ///
        ///      // Check to see if the conflict can be manually resolved
        ///      if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
        ///      {
        ///        return ManualResolve(conflict, rule, out actions);
        ///      }
        ///      // Check to see if the conflict can be resolved by dropping an attachment
        ///      else if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        ///      {
        ///        return ResolveByDroppingAttachment(conflict, rule, out actions);
        ///      }
        ///
        ///      return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        ///    }
        ///
        ///    public ConflictType ConflictTypeHandled
        ///    {
        ///      get;
        ///      set;
        ///    }
        /// 
        ///    #endregion
        /// 
        ///    // Private members implemented below...
        /// </code>
        /// </example>
        bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule);

        /// <summary>
        /// Attempts to resolve the migration conflict based on the provided <see cref="ConflictResolutionRule"/>.
        /// </summary>
        /// <param name="serviceContainer">The <see cref="System.ComponentModel.Design.ServiceContainer" /> that contains services provided by the platform.</param>
        /// <param name="conflict">The <see cref="Microsoft.TeamFoundation.Migration"/> being resolved.</param>
        /// <param name="rule">The <see cref="ConflictResolutionRule"/> that is to be applied to <paramref name="conflict"/>.</param>
        /// <param name="actions">A list of <see cref="MigrationAction"/> objects specific to the type of conflict
        /// manager in use.</param>
        /// <returns>An instance of <see cref="ConflictResolutionResult"/> providing the result of the method call.</returns>
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
        ///  public class FileAttachmentGeneralConflictHandler : IConflictHandler
        ///  {
        ///    #region IConflictHandler Members
        /// 
        ///    public bool CanResolve(MigrationConflict conflict, ConflictResolutionRule rule)
        ///    {
        ///      // If the conflict is not within the scope of the conflict resolution rule, then we can't resolve this conflict
        ///      if (!ConflictTypeHandled.ScopeInterpreter.IsInScope(conflict.ScopeHint, rule.ApplicabilityScope))
        ///      {
        ///        return false;
        ///      }
        ///
        ///      // Can we resolve this conflict by dropping the attachment?
        ///      if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        ///      {
        ///        return CanResolveByDroppingAttachment(conflict, rule);
        ///      }
        ///
        ///      return true;
        ///    }
        ///
        ///    public ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List&lt;MigrationAction&gt; actions)
        ///    {
        ///      actions = null;
        ///
        ///      // Check to see if the conflict can be manually resolved
        ///      if (rule.ActionRefNameGuid.Equals(new ManualConflictResolutionAction().ReferenceName))
        ///      {
        ///        return ManualResolve(conflict, rule, out actions);
        ///      }
        ///      // Check to see if the conflict can be resolved by dropping an attachment
        ///      else if (rule.ActionRefNameGuid.Equals(new FileAttachmentGeneralConflictDropAttachmentAction().ReferenceName))
        ///      {
        ///        return ResolveByDroppingAttachment(conflict, rule, out actions);
        ///      }
        ///
        ///      return new ConflictResolutionResult(false, ConflictResolutionType.Other);
        ///    }
        ///
        ///    public ConflictType ConflictTypeHandled
        ///    {
        ///      get;
        ///      set;
        ///    }
        /// 
        ///    #endregion
        /// 
        ///    // Private members implemented below...
        /// </code>
        /// </example>
        ConflictResolutionResult Resolve(IServiceContainer serviceContainer, MigrationConflict conflict, ConflictResolutionRule rule, out List<MigrationAction> actions);

        /// <summary>
        /// Gets or sets the <see cref="ConflictType"/> being handled.
        /// </summary>
        ConflictType ConflictTypeHandled
        {
            get;
            set;
        }
    }
}
