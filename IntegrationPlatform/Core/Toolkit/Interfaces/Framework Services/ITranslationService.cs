// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Public interface to create custom translaters that can help in translating adapter specific informatio to
    /// toolkit specific information during migration.
    /// </summary>
    public interface ITranslationService
    {
        void Translate(IMigrationAction action, Guid migrationSourceIdOfChangeGroup);
        bool IsSyncGeneratedAction(IMigrationAction action, Guid migrationSourceIdOfChangeGroup);
        bool IsSyncGeneratedItemVersion(string itemId, string itemVersion, Guid migrationSourceIdOfChangeGroup);
        bool IsSyncGeneratedItemVersion(string itemId, string itemVersion, Guid migrationSourceIdOfChangeGroup, bool onlyAsTarget);
        string TryGetTargetItemId(string sourceItemId, Guid sourceId);
        void CacheItemVersion(string sourceItemId, string sourceVersionId, Guid sourceId);
        MigrationItemId GetLastMigratedItemId(Guid sourceId);
        string GetLastProcessedItemVersion(string sourceItemId, Guid sourceId);
        void UpdateLastProcessedItemVersion(Dictionary<string, string> itemVersionPair, long lastChangeGroupId, Guid sourceId);
    }
}
