// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// This class implements an in-memory cache of the migrated Migration Items
    /// </summary>
    public class MigrationItemCache
    {
        // item id pairs: per SourceId (Guid), Pair of Items
        Dictionary<Guid, Dictionary<string, string>> m_itemPairs = new Dictionary<Guid, Dictionary<string, string>>();
        object m_itemPairsLock = new object();
        const int ItemPairMaxSize = 10000;

        // item version list: per SourceId (Guid), per ItemId (string), list of Versions (List<string>) migrated by us
        Dictionary<Guid, Dictionary<string, List<string>>> m_migratedByUsItemVersions = new Dictionary<Guid, Dictionary<string, List<string>>>();
        object m_migratedByUsItemVersionsLock = new object();
        const int ItemsInVersionCacheMaxSize = 10000;
        const int PerItemVersionCacheMaxSize = 100;

        /// <summary>
        /// Determines whether an item version is migrated by the platform.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="itemId"></param>
        /// <param name="itemVersion"></param>
        /// <returns></returns>
        public bool IsItemVersionMigratedByUs(
            Guid sourceId,
            string itemId,
            string itemVersion)
        {
            lock (m_migratedByUsItemVersionsLock)
            {
                if (m_migratedByUsItemVersions.ContainsKey(sourceId)
                    && m_migratedByUsItemVersions[sourceId].ContainsKey(itemId))
                {
                    Debug.Assert(m_migratedByUsItemVersions[sourceId][itemId] != null,
                        "m_migratedByUsItemVersions[sourceId][itemId] == null");

                    return m_migratedByUsItemVersions[sourceId][itemId].Contains(itemVersion);
                }

                return false;
            }
        }

        /// <summary>
        /// Add new versions to an existing item version list
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="itemId"></param>
        /// <param name="itemVersions"></param>
        internal void AddNewItemVersions(
            Guid sourceId,
            string itemId,
            string[] itemVersions)
        {
            lock (m_migratedByUsItemVersionsLock)
            {
                TryCreateDataEntry(sourceId, itemId);
                List<string> cachedRevisions = m_migratedByUsItemVersions[sourceId][itemId];

                if (cachedRevisions.Count >= PerItemVersionCacheMaxSize)
                {
                    // fuzzy logic to limit the number of revisions we cache
                    if (itemVersions.Length >= PerItemVersionCacheMaxSize)
                    {
                        cachedRevisions.Clear();
                    }
                    else
                    {
                        cachedRevisions.RemoveRange(0, cachedRevisions.Count / 2);
                    }
                }
                cachedRevisions.AddRange(itemVersions.AsEnumerable());
            }
        }

        /// <summary>
        /// Replace an existing item version list with the new version list
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="itemId"></param>
        /// <param name="itemVersions"></param>
        internal void RefreshItemVersions(
            Guid sourceId,
            string itemId,
            string[] itemVersions)
        {
            lock (m_migratedByUsItemVersionsLock)
            {
                TryCreateDataEntry(sourceId, itemId);
                m_migratedByUsItemVersions[sourceId][itemId] = new List<string>(itemVersions.AsEnumerable());
            }
        }

        /// <summary>
        /// Add a pair of migrated items to the cache
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="sourceItemId"></param>
        /// <param name="targetItemId"></param>
        internal void AddItemPair(
            Guid sourceId,
            string sourceItemId,
            string targetItemId)
        {
            lock (m_itemPairsLock)
            {
                if (!m_itemPairs.ContainsKey(sourceId))
                {
                    m_itemPairs.Add(sourceId, new Dictionary<string, string>());
                }

                if (m_itemPairs[sourceId].Count >= ItemPairMaxSize)
                {
                    // cache size limit is reached, we make space by removing the first half of the cache
                    var firstHalf = m_itemPairs[sourceId].Take(ItemPairMaxSize / 2).ToArray();
                    foreach (var pair in firstHalf)
                    {
                        m_itemPairs[sourceId].Remove(pair.Key);
                    }
                }

                if (!m_itemPairs[sourceId].ContainsKey(sourceItemId))
                {
                    m_itemPairs[sourceId].Add(sourceItemId, targetItemId);
                }
            }
        }

        /// <summary>
        /// Finds the Id of a migrated item given the source migration item Id.
        /// </summary>
        /// <param name="sourceItemId"></param>
        /// <param name="sourceId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool TryFindMirroredItemId(
            string sourceItemId,
            Guid sourceId,
            out string itemId)
        {
            lock (m_itemPairsLock)
            {
                if (m_itemPairs.ContainsKey(sourceId) && m_itemPairs[sourceId].ContainsKey(sourceItemId))
                {
                    itemId = m_itemPairs[sourceId][sourceItemId];
                    return true;
                }
                else
                {
                    itemId = string.Empty;
                    return false;
                }
            }
        }

        private void TryCreateDataEntry(
            Guid sourceId,
            string itemId)
        {
            if (!m_migratedByUsItemVersions.ContainsKey(sourceId))
            {
                m_migratedByUsItemVersions.Add(sourceId, new Dictionary<string, List<string>>());
            }

            if (!m_migratedByUsItemVersions[sourceId].ContainsKey(itemId))
            {
                Dictionary<string, List<string>> perSourceItemVersionCache = m_migratedByUsItemVersions[sourceId];
                if (perSourceItemVersionCache.Count >= ItemsInVersionCacheMaxSize)
                {
                    // cache size limit is reached, we make space by removing the first half of the cache
                    var firstHalf = perSourceItemVersionCache.Take(ItemsInVersionCacheMaxSize / 2).ToArray();
                    foreach (var itemVersionList in firstHalf)
                    {
                        perSourceItemVersionCache.Remove(itemVersionList.Key);
                    }
                }

                perSourceItemVersionCache.Add(itemId, new List<string>());
            }
        }
    }
}
