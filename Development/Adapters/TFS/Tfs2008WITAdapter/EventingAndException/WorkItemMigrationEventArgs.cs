// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;

//using Microsoft.TeamFoundation.Migration.Toolkit;

//namespace Microsoft.TeamFoundation.Migration.TfsAdapter
//{
//    /// <summary>
//    /// Arguments for work item migration events.
//    /// </summary>
//    public class WorkItemMigrationEventArgs: MigrationEventArgs
//    {
//        private string m_sourceMigrationSourceReferenceName;
//        private FullId m_sourceId;                          // Id of work item being synchronized

//        /// <summary>
//        /// Constructor.
//        /// </summary>
//        /// <param name="primarySystem">Primary system</param>
//        /// <param name="sourceId">Id of the work item being migrated</param>
//        public WorkItemMigrationEventArgs(
//            string sourceMigrationSourceReferenceName,
//            FullId sourceId)
//            : this(sourceMigrationSourceReferenceName, sourceId, null)
//        {
//        }

//        /// <summary>
//        /// Constructor.
//        /// </summary>
//        /// <param name="primarySystem">Primary system</param>
//        /// <param name="sourceId">Id of the work item being migrated</param>
//        /// <param name="description">Description of the event</param>
//        public WorkItemMigrationEventArgs(
//            string sourceMigrationSourceReferenceName,
//            FullId sourceId,
//            string description)
//            : this(sourceMigrationSourceReferenceName, sourceId, description, null)
//        {
//        }

//        /// <summary>
//        /// Constructor.
//        /// </summary>
//        /// <param name="primarySystem">Primary system</param>
//        /// <param name="sourceId">Id of the source work item</param>
//        /// <param name="description">Description of the event</param>
//        /// <param name="exception">Exception associated with the event</param>
//        public WorkItemMigrationEventArgs(
//            string sourceMigrationSourceReferenceName,
//            FullId sourceId,
//            string description,
//            Exception exception)
//            : base(description, exception)
//        {
//            m_sourceMigrationSourceReferenceName = sourceMigrationSourceReferenceName;
//            m_sourceId = sourceId;
//        }

//        /// <summary>
//        /// Gets id of the source work item.
//        /// </summary>
//        public FullId SourceId { get { return m_sourceId; } set { m_sourceId = value; } }

//        /// <summary>
//        /// Gets primary system used in the migration.
//        /// </summary>
//        public string SourceMigrationSourceReferenceName 
//        { 
//            get 
//            {
//                return m_sourceMigrationSourceReferenceName;
//            }
//            set 
//            { 
//                m_sourceMigrationSourceReferenceName = value; 
//            }
//        }
//    }
//}
