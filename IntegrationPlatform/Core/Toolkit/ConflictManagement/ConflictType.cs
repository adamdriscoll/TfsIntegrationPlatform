// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Conflict Type class.
    /// </summary>
    public abstract class ConflictType
    {
        protected IConflictHandler m_conflictHandler;
        protected Dictionary<Guid, ResolutionAction> m_supportedResolutionActions;
        protected List<string> m_conflictDetailsPropertyKeys = new List<string>();

        /// <summary>
        /// Constructor. Constructed conflict type will use the default path (unix path syntax)
        /// as the scope to be used in rules and conflict's scope hint
        /// </summary>
        /// <param name="conflictHandler"></param>
        public ConflictType(IConflictHandler conflictHandler)
        {
            Initializer(conflictHandler);
            conflictHandler.ConflictTypeHandled = this;
            RegisterDefaultSupportedResolutionActions();
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public abstract Guid ReferenceName
        {
            get;
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public virtual string FriendlyName
        {
            get
            {
                return this.GetType().ToString();
            }
        }

        public virtual bool SupportsMultipleRetry
        {
            get
            {
                // NOTE: MultipleRetry resolution action is special such that each individual conflict can be re-evaluated
                // for configurable multiple times.
                // Normally, conflicts are either in Status 0 (unresolved) or Status 1 (resolved). Conflicts that
                // "SupportMultipleRetry" can also be in Status 2 (scheduled for retry). The number of retries are recorded
                // in the conflict count column. 
                // At the beginning of each session, the platform "reactivates" the conflicted
                // actions and mark the corresponding conflict as resolved. If the conflict is raised again, the count is
                // incremented for that conflict. 
                // The conflict changes to Status 0 when the max retry attempts are exhausted.
                return m_supportedResolutionActions.ContainsKey(new MultipleRetryResolutionAction().ReferenceName);
            }
        }

        public virtual string HelpKeyword
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets all the supported resolution action of this conflict type.
        /// </summary>
        public Dictionary<Guid, ResolutionAction> SupportedResolutionActions
        {
            get
            {
                return m_supportedResolutionActions;
            }
        }

        public ReadOnlyCollection<string> ConflictDetailsPropertyKeys
        {
            get
            {
                return m_conflictDetailsPropertyKeys.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets a flag to indicate whether the conflicts of this type is countable
        /// </summary>
        /// <remarks>
        /// The framework only saves one active conflict in the storage with a counter for the coutable conflicts.
        /// </remarks>
        public virtual bool IsCountable
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the conflict handler of this conflict type.
        /// </summary>
        public IConflictHandler Handler
        {
            get
            {
                return m_conflictHandler;
            }
        }

        /// <summary>
        /// Gets the scope string interpreter of this conflict type.
        /// </summary>
        public virtual IApplicabilityScopeInterpreter ScopeInterpreter
        {
            get
            {
                return new BasicPathScopeInterpreter();
            }
        }

        /// <summary>
        /// Gets the syntax hint that's specific to a conflict type.
        /// </summary>
        public virtual string ScopeSyntaxHint
        {
            get
            {
                return ScopeInterpreter.ScopeSyntaxHint;
            }
        }

        /// <summary>
        /// Gets a named resolution action that is registed to resolve this type of conflict.
        /// </summary>
        /// <param name="actionReferenceName"></param>
        /// <returns></returns>
        public ResolutionAction this[Guid actionReferenceName]
        {
            get
            {
                if (this.m_supportedResolutionActions.ContainsKey(actionReferenceName))
                {
                    return m_supportedResolutionActions[actionReferenceName];
                }

                return null;
            }
        }

        /// <summary>
        /// Creates an active conflict of this type
        /// </summary>
        /// <param name="conflictDetails"></param>
        /// <returns></returns>
        public virtual MigrationConflict CreateConflict(
            string conflictDetails, 
            string scopeHint, 
            IMigrationAction conflictedAction)
        {
            return MigrationConflictInitializer(conflictDetails, MigrationConflict.Status.Unresolved, scopeHint, conflictedAction);
        }
        
        /// <summary>
        /// Creates a conflict of this type in the provided status
        /// </summary>
        /// <param name="conflictDetails"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public virtual MigrationConflict CreateConflict(
            string conflictDetails, 
            MigrationConflict.Status status, 
            string scopeHint, 
            IMigrationAction conflictedAction)
        {
            return MigrationConflictInitializer(conflictDetails, status, scopeHint, conflictedAction);
        }

        /// <summary>
        /// Adds a new supported resolution action.
        /// </summary>
        /// <param name="action"></param>
        public void AddSupportedResolutionAction(ResolutionAction action)
        {
            if (this.m_supportedResolutionActions.ContainsKey(action.ReferenceName))
            {
                return;
            }

            this.m_supportedResolutionActions.Add(action.ReferenceName, action);
        }

        /// <summary>
        /// Translates the raw string conflict details to more readable string reprentation
        /// </summary>
        /// <param name="dtls"></param>
        /// <returns></returns>
        public virtual string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            return dtls;
        }

        /// <summary>
        /// Subclasses should implement this method and register all the supported resolution actions
        /// </summary>
        protected abstract void RegisterDefaultSupportedResolutionActions();

        /// <summary>
        /// Subclasses should override this method and register all the keys used in the conflict details property bag
        /// </summary>
        /// <remarks>Note that empty string cannot be used as the property key. 
        /// For each key to be registered, call RegisterConflictDetailsPropertyKey declared in the base class.</remarks>
        protected virtual void RegisterConflictDetailsPropertyKeys()
        {
            // do nothing by default
        }

        /// <summary>
        /// Registers a key to be used in the conflict details property bag
        /// </summary>
        /// <param name="key">The key to be registered</param>
        /// <exception cref="System.ArgumentNullException">This exception is thrown if the key to be registered is empty or null</exception>
        protected void RegisterConflictDetailsPropertyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            if (!m_conflictDetailsPropertyKeys.Contains(key))
            {
                m_conflictDetailsPropertyKeys.Add(key);
            }
        }

        /// <summary>
        /// Try translating the raw string conflict details to the Conflict Details Property Bag. This may
        /// fail if the details are old free-style conflict details.
        /// </summary>
        /// <param name="dtls">The conflict details to be translated.</param>
        /// <param name="conflictDetailsProperty">The output conflict details</param>
        /// <returns>TRUE if the details are translated to the conflict details property bag; otherwise, FALSE.</returns>
        public bool TryTranslateConflictDetailsToNamedProperties(string dtls, out ConflictDetailsProperties conflictDetailsProperty)
        {
            conflictDetailsProperty = null;
            bool retVal = false;
            try
            {
                conflictDetailsProperty = ConflictDetailsProperties.Deserialize(dtls);
                retVal = true;
            }
            catch (Exception e)
            {
                // trace the exception, eat it, and we will return null
                TraceManager.TraceException(e);
            }

            return retVal;
        }

        private void Initializer(IConflictHandler conflictHandler)
        {
            if (null == conflictHandler)
            {
                throw new ArgumentNullException("conflictHandler");
            }

            m_conflictHandler = conflictHandler;

            m_supportedResolutionActions = new Dictionary<Guid, ResolutionAction>();
        }

        private MigrationConflict MigrationConflictInitializer(
            string conflictDetails,
            MigrationConflict.Status status,
            string scopeHint,
            IMigrationAction conflictedAction)
        {
            // using default details to readable description translator
            MigrationConflict conflict = new MigrationConflict(this, status, conflictDetails, scopeHint);
            conflict.ConflictedChangeAction = conflictedAction;
            return conflict;
        }
    }
}
