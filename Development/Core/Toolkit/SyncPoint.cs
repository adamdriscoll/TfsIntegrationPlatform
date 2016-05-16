// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The SyncPoint class contains the data describing a "sync point" which is the completion of the processing through the pipeline
    /// in a single direction.  The data describes the "point in time" (or equivalent data) on each of the two servers that should be
    /// in sync after processing the pipeline.  This is not simply the time at which the processing of the pipeline completed, because
    /// items may have been modified on the source side of the sync after the analysis phase was complete (so are not included in the sync)
    /// but before the pipeline procesing was completed. There may be many sync points in a continuous automatic session.
    /// </summary>
    internal class SyncPoint 
    {
        /// <summary>
        /// SourceHighWaterMarkName is the name of the HighWaterMark from the source side which identifies the type of data in the SourceHighWaterMarkValue
        /// </summary>
        public string SourceHighWaterMarkName { get; set; }
        
        /// <summary>
        /// SourceHighWaterMarkValue is the value of the HighWaterMark from the source side which identifies the more recently changed source item
        /// included in a syncronization pass
        /// </summary>
        public string SourceHighWaterMarkValue { get; set; }
        
        /// <summary>
        /// LastMigratedTargetItemId identifies the last item to be migrated to the target side in a syncronization pass
        /// </summary>
        public string LastMigratedTargetItemId { get; set; }

        /// <summary>
        /// LastMigratedTargetItemVersion is the most recent version of the item identified by LastMigratedTargetItemId that was migrated
        /// in the syncronization pass
        /// </summary>
        public string LastMigratedTargetItemVersion { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SyncPoint()
        {
        }

        /// <summary>
        /// Returns a SyncPoint object containg data about the last migrated items for the
        /// last time one direction of a sync or migration operation completed
        /// </summary>
        /// <param name="session">A Session object</param>
        /// <returns>A SyncPoint object or null if there have been no sync points completed for the specified session</returns>
        public static SyncPoint GetLatestSyncPointForSession(Session session)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                Guid sessionGuid = new Guid(session.SessionUniqueId);
                var syncPointQuery =
                    (from sp in context.RTSyncPointSet
                     where (sp.SessionUniqueId == sessionGuid)
                     orderby sp.Id descending
                     select sp).Take(1);

                if (syncPointQuery.Count() == 0)
                {
                    return null;
                }

                RTSyncPoint rtSyncPoint = syncPointQuery.First();
                SyncPoint syncPoint = new SyncPoint();
                syncPoint.SourceHighWaterMarkName = rtSyncPoint.SourceHighWaterMarkName;
                syncPoint.SourceHighWaterMarkValue = rtSyncPoint.SourceHighWaterMarkValue;
                syncPoint.LastMigratedTargetItemId = rtSyncPoint.LastMigratedTargetItemId;
                syncPoint.LastMigratedTargetItemVersion = rtSyncPoint.LastMigratedTargetItemVersion;
                return syncPoint;
            }
        }
    }


}
