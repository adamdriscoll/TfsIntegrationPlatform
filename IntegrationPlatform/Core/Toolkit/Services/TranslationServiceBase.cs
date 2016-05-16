// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// This abstract class provides the basic common implementation for some of the ITranslationService methods.
    /// </summary>
    public abstract class TranslationServiceBase : ITranslationService
    {
        protected readonly Session m_session;
        protected MigrationItemCache m_migrationItemCache = new MigrationItemCache();

        // The Guid key is a MigrationSource Guid
        protected Dictionary<Guid, MigrationItemId> m_lastMigratedItemsForMigrationSource = new Dictionary<Guid, MigrationItemId>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="userIdLookupService"></param>
        internal TranslationServiceBase(Session session, UserIdentityLookupService userIdLookupService)
        {
            m_session = session;
            UserIdLookupService = userIdLookupService;
        }

        internal UserIdentityLookupService UserIdLookupService 
        { 
            get; 
            set; 
        }

        #region ITranslationService Members

        public abstract void Translate(IMigrationAction action, Guid migrationSourceIdOfChangeGroup);

        public abstract bool IsSyncGeneratedAction(IMigrationAction action, Guid migrationSourceIdOfChangeGroup);

        public virtual bool IsSyncGeneratedItemVersion(string itemId, string itemVersion, Guid migrationSourceIdOfChangeGroup)
        {
            return IsSyncGeneratedItemVersion(itemId, itemVersion, migrationSourceIdOfChangeGroup, false);
        }

        public virtual bool IsSyncGeneratedItemVersion(string itemId, string itemVersion, Guid migrationSourceIdOfChangeGroup, bool onlyAsTarget)
        {
            if (m_migrationItemCache.IsItemVersionMigratedByUs(migrationSourceIdOfChangeGroup, itemId, itemVersion))
            {
                return true;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationItemResult =
                    from mi in context.RTMigrationItemSet
                    where mi.ItemId.Equals(itemId)
                    select mi;

                if (migrationItemResult.Count() > 0)
                {
                    var migrationItemVersionQuery =
                        from rev in migrationItemResult
                        where rev.ItemVersion.Equals(itemVersion)
                           && rev.MigrationSource.UniqueId.Equals(migrationSourceIdOfChangeGroup)
                        select rev.Id;

                    if (migrationItemVersionQuery.Count() > 0)
                    {
                        long sourceItemId = migrationItemVersionQuery.First();
                        Guid sessionId = new Guid(m_session.SessionUniqueId);

                        if (onlyAsTarget)
                        {
                            var itemConvPairResult =
                                from p in context.RTItemRevisionPairSet
                                where (p.RightMigrationItem.Id == sourceItemId)
                                   && p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionId)
                                select p;
                            return itemConvPairResult.Count() > 0;
                        }
                        else
                        {
                            var itemConvPairResult =
                                from p in context.RTItemRevisionPairSet
                                where (p.LeftMigrationItem.Id == sourceItemId || p.RightMigrationItem.Id == sourceItemId)
                                   && p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionId)
                                select p;
                            return itemConvPairResult.Count() > 0;
                        }
                    }
                }

                return false;
            }
        }

        public abstract string TryGetTargetItemId(string sourceItemId, Guid sourceId);

        public virtual void CacheItemVersion(string sourceItemId, string sourceVersionId, Guid sourceId)
        {
            MigrationItemId migrationItemId = new MigrationItemId();
            migrationItemId.ItemId = sourceItemId;
            migrationItemId.ItemVersion = sourceVersionId;
            if (m_lastMigratedItemsForMigrationSource.ContainsKey(sourceId))
            {
                m_lastMigratedItemsForMigrationSource[sourceId] = migrationItemId;
            }
            else
            {
                m_lastMigratedItemsForMigrationSource.Add(sourceId, migrationItemId);
            }
        }

        public virtual MigrationItemId GetLastMigratedItemId(Guid sourceId)
        {
            MigrationItemId migrationItemId;
            if (!m_lastMigratedItemsForMigrationSource.TryGetValue(sourceId, out migrationItemId))
            {
                migrationItemId.ItemId = null;
            }
            return migrationItemId;
        }

        public abstract string GetLastProcessedItemVersion(string sourceItemId, Guid sourceId);

        public abstract void UpdateLastProcessedItemVersion(Dictionary<string, string> itemVersionPair, long lastChangeGroupId, Guid sourceId);

        #endregion
    }
}
