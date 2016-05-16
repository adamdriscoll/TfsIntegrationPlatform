// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.Migration.Toolkit.VC;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    // Hack: This should be in Dogfood VC adapter only, if we want to bypass interface.
    public class VCTranslationService : TranslationServiceBase, IVCServerPathTranslationService
    {
        IVCServerPathTranslationService m_serverPathTranslationService;
        KeyValuePair<long, string> m_cachedUserIdLookupResult = new KeyValuePair<long, string>(0, string.Empty);
        Dictionary<Guid, Guid> m_migratinSourcePair = new Dictionary<Guid, Guid>();

        internal VCTranslationService(
            Session session,
            Guid leftMigrationSourceId,
            Guid rightMigrationSourceId,
            IProvider leftProvider,
            IProvider rightProvider,
            UserIdentityLookupService userIdLookupService)
            : base(session, userIdLookupService)
        {
            IServerPathTranslationService leftPathTranslationService = 
                leftProvider.GetService(typeof(IServerPathTranslationService)) as IServerPathTranslationService;
            if (null == leftPathTranslationService)
            {
                TraceManager.TraceWarning(MigrationToolkitResources.ErrorMissingVCPathTranslationService, 
                                          leftMigrationSourceId.ToString());

                // default to UnixStyle Server Path translation service
                leftPathTranslationService = new UnixStyleServerPathTranslationService();
            }
            leftPathTranslationService.Initialize(session.Filters[leftMigrationSourceId]);

            IServerPathTranslationService rightPathTranslationService = 
                rightProvider.GetService(typeof(IServerPathTranslationService)) as IServerPathTranslationService;
            if (null == rightPathTranslationService)
            {
                TraceManager.TraceWarning(MigrationToolkitResources.ErrorMissingVCPathTranslationService, 
                                          rightMigrationSourceId.ToString());

                // default to UnixStyle Server Path translation service
                rightPathTranslationService = new UnixStyleServerPathTranslationService();
            }
            rightPathTranslationService.Initialize(session.Filters[rightMigrationSourceId]);

            m_serverPathTranslationService = new VCServerPathTranslationService(leftMigrationSourceId,
                                                                                leftPathTranslationService,
                                                                                rightMigrationSourceId,
                                                                                rightPathTranslationService,
                                                                                m_session.Filters.FilterPair);

            m_migratinSourcePair.Add(leftMigrationSourceId, rightMigrationSourceId);
            m_migratinSourcePair.Add(rightMigrationSourceId, leftMigrationSourceId);
        }

        #region ITranslationService Members

        public override void Translate(IMigrationAction action, Guid migrationSourceIdOfChangeGroup)
        {
            MigrationAction migrAction = action as MigrationAction;
            Debug.Assert(migrAction != null, "action is not a MigrationActin");

            migrAction.Path = GetMappedPath(action.Path, migrationSourceIdOfChangeGroup);
            migrAction.FromPath = GetMappedPath(action.FromPath, migrationSourceIdOfChangeGroup);

            if (UserIdLookupService.IsConfigured)
            {
                if (action.ChangeGroup.ChangeGroupId != m_cachedUserIdLookupResult.Key)
                {
                    IdentityLookupContext context = new IdentityLookupContext(
                            migrationSourceIdOfChangeGroup, m_migratinSourcePair[migrationSourceIdOfChangeGroup]);

                    UserIdPropertyNameEnum srcDefaultUserIdProperty;
                    UserIdPropertyNameEnum targetDefaultUserIdProperty;
                    if (UserIdLookupService.TryGetDefaultUserIdProperty(context.SourceMigrationSourceId, out srcDefaultUserIdProperty)
                        && UserIdLookupService.TryGetDefaultUserIdProperty(context.TargetMigrationSourceId, out targetDefaultUserIdProperty))
                    {
                        RichIdentity srcUserId = new RichIdentity();
                        srcUserId[srcDefaultUserIdProperty.ToString()] = action.ChangeGroup.Owner; // todo: parse qualified name?

                        RichIdentity mappedUserId;
                        if (UserIdLookupService.TryLookup(srcUserId, context, out mappedUserId))
                        {
                            action.ChangeGroup.Owner = mappedUserId[targetDefaultUserIdProperty.ToString()];
                        }
                    }

                    m_cachedUserIdLookupResult = new KeyValuePair<long, string>(action.ChangeGroup.ChangeGroupId, action.ChangeGroup.Owner);
                }
                else
                {
                    action.ChangeGroup.Owner = m_cachedUserIdLookupResult.Value;
                }
            }
        }

        public override bool IsSyncGeneratedAction(IMigrationAction action, Guid migrationSourceIdOfChangeGroup)
        {
            if (action.ChangeGroup != null
                && !string.IsNullOrEmpty(action.ChangeGroup.Name))
            {
                return IsSyncGeneratedItemVersion(action.ChangeGroup.Name, Constants.ChangeGroupGenericVersionNumber, migrationSourceIdOfChangeGroup);
            }
            else
            {
                throw new ArgumentException("action.ChangeGroup is unknown", "action");
            }
        }

        public override string TryGetTargetItemId(string sourceWorkItemId, Guid sourceId)
        {
            // todo: cache results

            var targetItemId = string.Empty;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationItemResult =
                    from mi in context.RTMigrationItemSet
                    where mi.ItemId.Equals(sourceWorkItemId)
                        && mi.ItemVersion.Equals(Constants.ChangeGroupGenericVersionNumber) // include only non-versioned migration items
                    select mi;
                if (migrationItemResult.Count() == 0)
                {
                    return targetItemId;
                }

                RTMigrationItem sourceItem = null;
                foreach (RTMigrationItem rtMigrationItem in migrationItemResult)
                {
                    rtMigrationItem.MigrationSourceReference.Load();
                    if (rtMigrationItem.MigrationSource.UniqueId.Equals(sourceId))
                    {
                        sourceItem = rtMigrationItem;
                    }
                }
                if (null == sourceItem)
                {
                    return targetItemId;
                }

                var sessionUniqueId = new Guid(m_session.SessionUniqueId);
                var itemConvPairResult =
                    from p in context.RTItemRevisionPairSet
                    where (p.LeftMigrationItem.Id == sourceItem.Id || p.RightMigrationItem.Id == sourceItem.Id)
                        && (p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionUniqueId))
                    select p;

                if (itemConvPairResult.Count() == 0)
                {
                    return targetItemId;
                }

                RTItemRevisionPair itemRevisionPair = itemConvPairResult.First();
                if (itemRevisionPair.LeftMigrationItem == sourceItem)
                {
                    itemRevisionPair.RightMigrationItemReference.Load();
                    targetItemId = itemRevisionPair.RightMigrationItem.ItemId;
                }
                else
                {
                    itemRevisionPair.LeftMigrationItemReference.Load();
                    targetItemId = itemRevisionPair.LeftMigrationItem.ItemId;
                }
            }

            return targetItemId;
        }

        public override string GetLastProcessedItemVersion(string sourceItemId, Guid sourceId)
        {
            throw new NotImplementedException();
        }

        public override void UpdateLastProcessedItemVersion(Dictionary<string, string> itemVersionPair, long lastChangeGroupId, Guid sourceId)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// This method takes the other side's serverPath, returns the mapped path of this side. 
        /// It returns null if the path is not mapped.
        /// </summary>
        /// <param name="peerServerPath">Server path of peer side.</param>
        public string GetMappedPath(string peerServerPath, Guid migrationSourceIdOfChangeGroup)
        {
            if (peerServerPath == null)
            {
                return null;
            }

            try
            {
                return m_serverPathTranslationService.Translate(migrationSourceIdOfChangeGroup, peerServerPath);
            }
            catch (ServerPathTranslationException ex)
            {
                // The path is not mapped, return null.
                TraceManager.TraceVerbose(ex.Message);
                return null;                
            }
        }

        #region IVCServerPathTranslationService Members

        public string Translate(Guid srcMigrationSourceId, string srcServerPath)
        {
            return m_serverPathTranslationService.Translate(srcMigrationSourceId, srcServerPath);
        }

        #endregion

        // remove a last trailing path separator
        internal static string TrimTrailingPathSeparator(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimEnd('/', '\\');
            }

            return path;
        }
    }
}
