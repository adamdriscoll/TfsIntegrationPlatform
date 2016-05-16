// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;

//using Microsoft.TeamFoundation.Migration.Toolkit;

//namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
//{
//    /// <summary>
//    /// Argument for the completed work item analysis event.
//    /// </summary>
//    public class AnalysisCompleteEventArgs: WorkItemMigrationEventArgs
//    {
//        private FullId m_targetId;                          // Id of the target work item, null if the item is to be created
//        private int m_sourceRevisions;                      // Number of revisions on the source side
//        private int m_targetRevisions;                      // Number of revisions to be created on the target side

//        /// <summary>
//        /// Constructor.
//        /// </summary>
//        /// <param name="primarySystem">Primary system</param>
//        /// <param name="sourceId">Id of the source work item</param>
//        /// <param name="targetId">Id of the work item on the other side, null if the item is to be created</param>
//        /// <param name="sourceRevisions">Number of revisions to be migrated from the source side</param>
//        /// <param name="targetRevisions">Number of revisions to be created on the target side</param>
//        public AnalysisCompleteEventArgs(
//            string sourceMigrationSourceReferenceName,
//            FullId sourceId,
//            FullId targetId,
//            int sourceRevisions,
//            int targetRevisions)
//            : base(sourceMigrationSourceReferenceName, sourceId)
//        {
//            m_targetId = targetId;
//            m_sourceRevisions = sourceRevisions;
//            m_targetRevisions = targetRevisions;

//            Description = string.Format(
//                TfsWITAdapterResources.Culture,
//                TfsWITAdapterResources.MsgItemAnalysisComplete, 
//                sourceId, sourceRevisions);
//        }

//        /// <summary>
//        /// Gets the work item on the other side. Null if a new work item is to be created
//        /// </summary>
//        public FullId TargetId { get { return m_targetId; } }

//        /// <summary>
//        /// Returns number of revisions to be migrated from the source side.
//        /// </summary>
//        public int SourceRevisions { get { return m_sourceRevisions; } }

//        /// <summary>
//        /// Returns number of revisions to be created on the target side.
//        /// </summary>
//        public int TargetRevisions { get { return m_targetRevisions; } }
//    }
//}
