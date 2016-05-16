// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Arguments for session-specific events.
    /// </summary>
    public class MigrationSessionEventArgs: MigrationEventArgs
    {
        private long m_migratedItemCount;                   // Number of items that were successfully migrated
        private long m_failedItemCount;                     // Number of items that were not migrated

        public long MigratedItemCount { get { return m_migratedItemCount; } set { m_migratedItemCount = value; } }
        public long FailedItemCount { get { return m_failedItemCount; } set { m_failedItemCount = value; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="description">Description of the event</param>
        public MigrationSessionEventArgs(
            string description)
            : this(description, 0, 0)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="description">Description of the event</param>
        /// <param name="migratedCount">Number of items successfully migrated</param>
        /// <param name="failedCount">Number of items failed to migrate</param>
        public MigrationSessionEventArgs(
            string description,
            long migratedCount,
            long failedCount)
            : this(description, migratedCount, failedCount, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="description">Description of the event</param>
        /// <param name="migratedCount">Number of items successfully migrated</param>
        /// <param name="failedCount">Number of items failed to migrate</param>
        /// <param name="exception">Exception associated with the event</param>
        public MigrationSessionEventArgs(
            string description,
            long migratedCount,
            long failedCount,
            Exception exception)
            : base(description, exception)
        {
            m_migratedItemCount = migratedCount;
            m_failedItemCount = failedCount;
        }
    }
}
