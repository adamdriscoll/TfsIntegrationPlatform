// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Contains properties and methods for defining conflict resolution actions. 
    /// </summary>
    /// <remarks>This abstract class provides default implementations for the methods and properties of 
    /// the <see cref="IResolutionRuleFactory"/> interface.
    /// <para/>This class can only be instantiated in a derived form.</remarks>
    /// <seealso cref="IResolutionRuleFactory"/>
    /// <example>The following example shows how to create a specific implementation of <see cref="ResolutionAction"/> that
    /// might be used to determine whether or not a work item attachment conflict can be resolved by dropping an
    /// attachment.
    /// <code>
    /// public class FileAttachmentGeneralConflictDropAttachmentAction : ResolutionAction
    /// {
    ///    public static readonly string DATAKEY_MIN_FILE_SIZE_TO_DROP = "MinFileSizeToDrop";
    /// 
    ///    private static readonly Guid s_actionReferenceName;
    ///    private static readonly string s_actionFriendlyName;
    ///    private static readonly List&lt;string&gt; s_supportedActionDataKeys;
    /// 
    ///    /// <summary>
    ///    /// Initializes the FileAttachmentGeneralConflictDropAttachmentAction class.
    ///    /// </summary>
    ///    static FileAttachmentGeneralConflictDropAttachmentAction()
    ///    {
    ///        // Assign a unique ID to this conflict resolution action
    ///        s_actionReferenceName = new Guid("E3EE75A3-8BDC-40a5-903E-52D7EFA0DDDD");
    /// 
    ///        // Provide a description for this conflict resolution action
    ///        s_actionFriendlyName = "Resolve file attachment oversized conflict by dropping it.";
    /// 
    ///        // Setup the list of supported action keys
    ///        s_supportedActionDataKeys = new List&lt;string&gt;();
    ///        s_supportedActionDataKeys.Add(DATAKEY_MIN_FILE_SIZE_TO_DROP);
    ///    }
    /// 
    ///    /// <summary>
    ///    /// Gets the reference name of this action.
    ///    /// </summary>
    ///    public override Guid ReferenceName
    ///    {
    ///        get
    ///        {
    ///            return s_actionReferenceName;
    ///        }
    ///    }
    /// 
    ///    /// <summary>
    ///    /// Gets the friendly name of the action.
    ///    /// </summary>
    ///    public override string FriendlyName
    ///    {
    ///        get
    ///        {
    ///            return s_actionFriendlyName;
    ///        }
    ///    }
    /// 
    ///    /// <summary>
    ///    /// Gets the collection of action data keys associated with this action.
    ///    /// </summary>
    ///    public override System.Collections.ObjectModel.ReadOnlyCollection&lt;string&gt; ActionDataKeys
    ///    {
    ///        get
    ///        {
    ///            return s_supportedActionDataKeys.AsReadOnly();
    ///        }
    ///    }
    /// }
    /// </code>
    /// </example>
    public abstract class ResolutionAction : IResolutionRuleFactory
    {
        /// <summary>
        /// Gets the reference name of this action.
        /// </summary>
        public abstract Guid ReferenceName
        {
            get;
        }

        /// <summary>
        /// Gets the friendly name of the action.
        /// </summary>
        public abstract string FriendlyName
        {
            get;
        }

        /// <summary>
        /// Gets the collection of action data keys associated with this action.
        /// </summary>
        public abstract ReadOnlyCollection<string> ActionDataKeys
        {
            get;
        }

        #region IResolutionRuleFactory Members

        /// <summary>
        /// Creates a default <see cref="ConflictResolutionRule"/>.
        /// </summary>
        /// <param name="applicableScope">The scope as it applies to the <see cref="ConflictResolutionRule"/> being
        /// created.</param>
        /// <param name="description">The description of the <see cref="ConflictResolutionRule"/>.</param>
        /// <param name="actionData">The action data to be associated with the <see cref="ConflictResolutionRule"/>
        /// defined as a collection of key/value pairs.</param>
        /// <returns>
        /// A new, default <see cref="ConflictResolutionRule"/>.
        /// </returns>
        /// <remarks>Override this method in a derived class to provide a more specific conflict resolution rule 
        /// implementation.</remarks>
        public virtual ConflictResolutionRule NewRule(string applicableScope, string description, Dictionary<string, string> actionData)
        {
            ConflictResolutionRule newRule = new ConflictResolutionRule();
            newRule.ActionReferenceName = ReferenceName.ToString();
            newRule.ApplicabilityScope = applicableScope;
            newRule.RuleDescription = description;
            newRule.RuleReferenceName = Guid.NewGuid().ToString();
            newRule.DataField = new DataField[actionData.Count];
            for (int i = 0; i < actionData.Count; ++i)
            {
                newRule.DataField[i] = new DataField();
                newRule.DataField[i].FieldName = actionData.ElementAt(i).Key;
                newRule.DataField[i].FieldValue = actionData.ElementAt(i).Value;
            }

            return newRule;
        }

        #endregion
    }
}
