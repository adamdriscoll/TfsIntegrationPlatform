// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Threading;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class ConflictUnresolvedEventArgs
    {
        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unresolvedConflict"></param>
        /// <param name="message"></param>
        /// <param name="sourceId">migration source id or framework source id</param>
        /// <param name="scopeId">session group unique id or session unique id</param>
        public ConflictUnresolvedEventArgs(
            MigrationConflict unresolvedConflict,
            string message,
            Guid sourceId,
            Guid scopeId,
            Thread conflictedThread)
        {
            Initializer(unresolvedConflict, message, sourceId, scopeId, conflictedThread);
        }

        /// <summary>
        /// Constructor. This constructor creates an arg for a global (not session specific) conflict.
        /// </summary>
        /// <param name="unresolvedConflict"></param>
        /// <param name="message"></param>
        public ConflictUnresolvedEventArgs(
            MigrationConflict unresolvedConflict,
            string message,
            Guid scopeId,
            Thread conflictedThread)
        {
            Initializer(unresolvedConflict, message, Constants.FrameworkSourceId, scopeId, conflictedThread);
        }
        #endregion

        private void Initializer(
            MigrationConflict unresolvedConflict,
            string message,
            Guid sourceId,
            Guid scopeId,
            Thread conflictedThread)
        {
            m_conflict = unresolvedConflict;
            m_message = message;
            m_sourceId = sourceId;
            m_scopeId = scopeId;
            m_conflictedThread = conflictedThread;
            m_conflictSyncOrchOption = null;
        }

        /// <summary>
        /// Gets the unresolved conflict
        /// </summary>
        public MigrationConflict UnresolvedConflict
        {
            get
            {
                return m_conflict;
            }
        }

        /// <summary>
        /// Gets the message
        /// </summary>
        public string Message
        {
            get
            {
                return m_message;
            }
        }

        /// <summary>
        /// Gets the session scope id - unique identifier of a session group or session
        /// </summary>
        public Guid ScopeId
        {
            get
            {
                return m_scopeId;
            }
        }

        /// <summary>
        /// Gets the source id - unique identifier of a migration source or the framework itself
        /// </summary>
        public Guid SourceId
        {
            get
            {
                return m_sourceId;
            }
        }

        /// <summary>
        /// Tells if the conflict is in a global scope (vs session scope)
        /// </summary>
        public bool IsGlobalConflict
        {
            get
            {
                return SourceId.Equals(Constants.FrameworkSourceId);
            }
        }

        public Thread ConflictedThread
        {
            get
            {
                return m_conflictedThread;
            }
        }

        public SyncOrchestrator.ConflictsSyncOrchOptions? SyncOrchestrationOption
        {
            get
            {
                return m_conflictSyncOrchOption;
            }
            set
            {
                m_conflictSyncOrchOption = value;
            }
        }

        private MigrationConflict m_conflict;
        private string m_message;
        private Guid m_scopeId;
        private Guid m_sourceId;
        private Thread m_conflictedThread;
        private SyncOrchestrator.ConflictsSyncOrchOptions? m_conflictSyncOrchOption;
    }
}
