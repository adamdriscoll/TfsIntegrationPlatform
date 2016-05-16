// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Defines a set of properties describing a migration conflict.
    /// </summary>
    public class MigrationConflict
    {
        /// <summary>
        /// The migration conflict status.
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// The migration conflict is unresolved.
            /// </summary>
            Unresolved = 0,
            /// <summary>
            /// The migration conflict is resolved.
            /// </summary>
            Resolved = 1,
            /// <summary>
            /// The migration conflict is scheduled for retry
            /// </summary>
            ScheduledForRetry = 2,
        }

        /// <summary>
        /// Provides a delegate that can translate the details of a <see cref="MigrationConflict"/>
        /// into a human-readable format.
        /// </summary>
        public delegate string TranslateConflictDetailsToReadableDescription(string dtls);

        private ConflictType m_conflictType;
        private Status m_conflictStatus;
        private string m_conflictDetails;
        private string m_scopeHint;
        private TranslateConflictDetailsToReadableDescription m_translator;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationConflict"/> class.
        /// </summary>
        /// <param name="conflictType">Specifies the type of conflict being created.</param>
        /// <param name="conflictStatus">Specifies the conflict status.</param>
        /// <param name="conflictDetails">Specifies the conflict details.</param>
        /// <param name="scopeHint">Specifies the scope hint used during conflict resolution.</param>
        public MigrationConflict(
            ConflictType conflictType,
            Status conflictStatus,
            string conflictDetails,
            string scopeHint)
        {
            Initializer(conflictType, conflictStatus, conflictDetails, scopeHint, conflictType.TranslateConflictDetailsToReadableDescription);
        }

        #endregion

        private void Initializer(
            ConflictType conflictType,
            Status conflictStatus,
            string conflictDetails,
            string scopeHint,
            TranslateConflictDetailsToReadableDescription translator)
        {
            if (null == conflictType)
            {
                throw new ArgumentNullException("conflictType");
            }
            if (null == translator)
            {
                throw new ArgumentNullException("translator");
            }

            m_conflictType = conflictType;
            m_conflictStatus = conflictStatus;
            m_conflictDetails = conflictDetails;
            m_scopeHint = scopeHint;
            m_translator = translator;
        }

        /// <summary>
        /// Reload conflict status from DB. 
        /// Since only status will be changed after a conflict is stored in DB, no other properties are reloaded. 
        /// </summary>
        /// <returns>True if the conflict is successfully reloaded. </returns>
        public bool Reload()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var conflictQuery =
                    from c in context.RTConflictSet
                    where c.Id == InternalId
                    select c;

                if (conflictQuery.Count() > 0)
                {
                    m_conflictStatus = (Status) conflictQuery.First().Status;
                    return true;
                }
                return false;
            }
        }

        #region Properties

        /// <summary>
        /// If a conflict is resolved by multiple retries, gets the number of retry attempts that have been made
        /// </summary>
        public int CurrentNumberOfRetry
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the conflict type.
        /// </summary>
        public ConflictType ConflictType
        {
            get
            {
                return m_conflictType;
            }
        }

        /// <summary>
        /// Gets the conflict status.
        /// </summary>
        public Status ConflictStatus
        {
            get
            {
                return m_conflictStatus;
            }
        }

        /// <summary>
        /// Gets the scope hint used in conflict resolution.
        /// </summary>
        public string ScopeHint
        {
            get
            {
                return m_scopeHint;
            }
        }

        /// <summary>
        /// Gets or sets the change action that caused the conflict (optional).
        /// </summary>
        public IMigrationAction ConflictedChangeAction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the link change action that caused the conflict (optional).
        /// </summary>
        public LinkChangeAction ConflictedLinkChangeAction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the description associated with this conflict.
        /// </summary>
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(ConflictDetails))
                {
                    return string.Empty;
                }

                Debug.Assert(null != m_translator, "Translator delegate has not been assigned");
                return m_translator(ConflictDetails);
            }
        }

        /// <summary>
        /// Gets the property bag of the conflict details
        /// </summary>
        public ConflictDetailsProperties ConflictDetailsProperties
        {
            get
            {
                Debug.Assert(null != m_conflictType, "m_conflictType is NULL");
                ConflictDetailsProperties retVal;
                if (!m_conflictType.TryTranslateConflictDetailsToNamedProperties(ConflictDetails, out retVal))
                {
                    retVal = new ConflictDetailsProperties();
                    retVal.Properties.Add(ConflictDetailsProperties.DefaultConflictDetailsKey, ConflictDetails);
                }

                return retVal;
            }
        }

        /// <summary>
        /// Gets the details associated with this conflict.
        /// </summary>
        public string ConflictDetails
        {
            get
            {
                return m_conflictDetails;
            }
        }

        /// <summary>
        /// Id in the data storage, for internal use only
        /// </summary>
        internal int InternalId
        {
            get;
            set;
        }

        #endregion
    }
}
