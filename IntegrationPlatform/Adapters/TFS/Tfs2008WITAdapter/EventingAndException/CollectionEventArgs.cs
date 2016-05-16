// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Microsoft.TeamFoundation.Migration.TfsAdapter
//{
//    /// <summary>
//    /// Arguments describing conflict between two collections (attachments or links)
//    /// </summary>
//    public class CollectionEventArgs : WorkItemMigrationEventArgs
//    {
//        private FullId m_targetId;                          // Id of the target work item

//        /// <summary>
//        /// Constructor.
//        /// </summary>
//        /// <param name="primarySystem">Primary system</param>
//        /// <param name="ids">Ids of conflicting work items</param>
//        /// <param name="reaction">Reaction to the conflict</param>
//        /// <param name="description">Event's description</param>
//        public CollectionEventArgs(
//            string sourceMigrationSourceReferenceName,
//            FullId sourceId,
//            FullId targetId,
//            string description)
//            : base(sourceMigrationSourceReferenceName, sourceId, description)
//        {
//            m_targetId = targetId;
//        }

//        /// <summary>
//        /// Returns id of a target work item.
//        /// </summary>
//        public FullId TargetId { get { return m_targetId; } }
//    }
//}
