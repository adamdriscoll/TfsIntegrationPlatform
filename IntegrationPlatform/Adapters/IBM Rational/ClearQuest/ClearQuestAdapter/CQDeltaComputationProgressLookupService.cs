// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQDeltaComputationProgressLookupService
    {
        private ITranslationService m_translationService;
        private Guid m_sourceId;
        private Dictionary<string, int> m_lastMigrationItemCache = new Dictionary<string, int>();

        public static string CreateHistoryItemId(
            string recordEntityDefName,
            string recordDispName, 
            string historyFieldName)
        {
            // EntityDef::EntityDispName::HistoryFieldName
            return recordEntityDefName + CQHistoryMigrationItem.Delimiter + 
                   recordDispName;
        }

        public CQDeltaComputationProgressLookupService(
            ITranslationService translationService,
            Guid sourceId)
        {
            m_translationService = translationService;
            m_sourceId = sourceId;
        }

        public bool IsMigrationItemProcessed(string itemId, int itemVersion)
        {
            if (!IsMigrationItemInCache(itemId))
            {
                LoadMigrationItemInStorage(itemId);
            }

            return IsMigratoinItemProcessed(itemId, itemVersion);
        }
                
        public int GetLastProcessedItemVersion(string itemId)
        {
            if (!IsMigrationItemInCache(itemId))
            {
                LoadMigrationItemInStorage(itemId);
            }

            return m_lastMigrationItemCache[itemId];
        }

        public void UpdateCache(string itemId, int itemVersion)
        {
            const int CacheSize = 100000;
            if (m_lastMigrationItemCache.Count() > CacheSize)
            {
                TraceManager.TraceInformation("CQ last delta revision cache max size is reached - start cleaning up job.");
                var keys = m_lastMigrationItemCache.Keys;
                for (int i = 0; i < keys.Count() && i <= CacheSize / 2; ++i)
                {
                    m_lastMigrationItemCache.Remove(keys.ElementAt(i));
                }
                TraceManager.TraceInformation("CQ last delta revision cache cleaning up job finishes.");
            }

            if (!m_lastMigrationItemCache.ContainsKey(itemId))
            {
                m_lastMigrationItemCache.Add(itemId, itemVersion);
            }
            else if (itemVersion > m_lastMigrationItemCache[itemId])
            {
                m_lastMigrationItemCache[itemId] = itemVersion;
            }
        }

        private void LoadMigrationItemInStorage(string itemId)
        {
            string lastMigrationItemInStorage = m_translationService.GetLastProcessedItemVersion(itemId, m_sourceId);
            if (string.IsNullOrEmpty(lastMigrationItemInStorage))
            {
                lastMigrationItemInStorage = "-1";
            }

            UpdateCache(itemId, int.Parse(lastMigrationItemInStorage));
        }

        private bool IsMigrationItemInCache(string itemId)
        {
            return m_lastMigrationItemCache.ContainsKey(itemId);
        }

        private bool IsMigratoinItemProcessed(string itemId, int itemVersion)
        {
            return itemVersion <= m_lastMigrationItemCache[itemId];
        }
    }
}
