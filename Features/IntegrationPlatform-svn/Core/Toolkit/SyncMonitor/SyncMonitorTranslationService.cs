// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.SyncMonitor
{
    // The SyncMonitor only needs to use one method of ITranslationService, namely IsSyncGeneratedItemVersion which is
    // implemented in TranslationServiceBase so this just extends TranslationServiceBase to pick up that implementation
    // but all other methods are not implemented as they should never be called.
    internal class SyncMonitorTranslationService : TranslationServiceBase
    {
        public SyncMonitorTranslationService(Session session) : base(session, null)
        {
        }

        #region ITranslationService Members

        public override void Translate(IMigrationAction action, Guid migrationSourceIdOfChangeGroup)
        {
            throw new NotImplementedException();
        }      

        public override bool IsSyncGeneratedAction(IMigrationAction action, Guid migrationSourceIdOfChangeGroup)
        {
            throw new NotImplementedException();
        }

        public override string TryGetTargetItemId(string sourceWorkItemId, Guid sourceId)
        {
            throw new NotImplementedException();
        }

        public override void CacheItemVersion(string sourceItemId, string sourceVersionId, Guid sourceId)
        {
            throw new NotImplementedException();
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
        
    }
}
