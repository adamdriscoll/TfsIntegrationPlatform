// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public enum ForceSyncItemStatus
    {
        Pending = 0,
        Complete = 1
    }

    /// <summary>
    /// A Service for retrieving and updating the status of force sync items in the database
    /// </summary>
    internal class ForceSyncItemService : IForceSyncItemService
    {
        private HashSet<string> m_currentForceSyncItemIds;

        public Guid SessionId { get; set; }

        public Guid MigrationSourceid { get; set; }

        /// <summary>
        /// Return the set of items to force sync
        /// </summary>
        public IEnumerable<string> GetItemsForForceSync()
        {
            if (m_currentForceSyncItemIds == null)
            {
                m_currentForceSyncItemIds = new HashSet<string>();

                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    IQueryable<RTForceSyncItem> forceSyncItemQuery = GetRTForceSyncItems(context);
                    if (forceSyncItemQuery != null)
                    {
                        foreach (RTForceSyncItem RTForceSyncItem in forceSyncItemQuery)
                        {
                            m_currentForceSyncItemIds.Add(RTForceSyncItem.ItemId);
                        }
                    }
                }
            }

            return m_currentForceSyncItemIds;
        }

        /// <summary>
        /// Update the status of the current set of force sync items for the context
        /// </summary>
        public void MarkCurrentItemsProcessed()
        {
            if (m_currentForceSyncItemIds != null && m_currentForceSyncItemIds.Count > 0)
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    IQueryable<RTForceSyncItem> forceSyncItemQuery = GetRTForceSyncItems(context);
                    if (forceSyncItemQuery != null)
                    {
                        foreach (RTForceSyncItem rTForceSyncItem in forceSyncItemQuery)
                        {
                            if (m_currentForceSyncItemIds.Contains(rTForceSyncItem.ItemId))
                            {
                                rTForceSyncItem.Status = (int)ForceSyncItemStatus.Complete;
                            }
                        }
                        context.TrySaveChanges();
                    }
                }
            }

            // Set the current cached Ids to null so that the next call to GetItemsForForceSync will go to the database
            m_currentForceSyncItemIds = null;
        }

        private IQueryable<RTForceSyncItem> GetRTForceSyncItems(RuntimeEntityModel context)
        {
            var sessionQuery =
                (from session in context.RTSessionSet
                    where session.SessionUniqueId == SessionId
                    select session);
            if (sessionQuery.Count() > 0)
            {
                RTSession rtSession = sessionQuery.First();

                var forceSyncItemQuery =
                    (from forceSyncItem in context.RTForceSyncItemSet
                     where forceSyncItem.SessionId == rtSession.Id &&
                         forceSyncItem.MigrationSource.UniqueId == MigrationSourceid &&
                         ((!(forceSyncItem.Status.HasValue)) || forceSyncItem.Status != (int)ForceSyncItemStatus.Complete)
                     select forceSyncItem);
                return forceSyncItemQuery;
            }
            else
            {
                return null;
            }
        }
    }
}
