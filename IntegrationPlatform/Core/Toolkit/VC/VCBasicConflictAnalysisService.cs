// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class VCBasicConflictAnalysisService : IConflictAnalysisService
    {

        #region IConflictAnalysisService Members

        public ChangeGroupService TargetChangeGroupService
        {
            get;
            set;
        }

        public ConflictManager ConflictManager
        {
            get;
            set;
        }

        public ITranslationService TranslationService
        {
            get;
            set;
        }

        public Guid TargetSystemId
        {
            get;
            set;
        }

        public Guid SourceSystemId
        {
            get;
            set;
        }

        public Session Configuration
        {
            get;
            set;
        }

        public void Analyze()
        {
            if (null == Configuration || null == TargetChangeGroupService || Guid.Empty == TargetSystemId)
            {
                return;
            }
            detectVCContentConflict();
        }

        #endregion

        private void detectVCContentConflict()
        {
            foreach (var conflictedActionPairs in TargetChangeGroupService.DetectContentConflict())
            {
                List<MigrationAction> resultMigrationActions;

                MigrationConflict contentConflict;

                if (conflictedActionPairs.Value.Action == WellKnownChangeActionId.Edit && conflictedActionPairs.Key.Action == WellKnownChangeActionId.Edit)
                {
                    contentConflict = VCContentConflictType.CreateConflict(conflictedActionPairs.Key, conflictedActionPairs.Value);
                }
                else
                {
                    contentConflict = VCNameSpaceContentConflictType.CreateConflict(conflictedActionPairs.Key);
                }

                var resolutionRslt = ConflictManager.TryResolveNewConflict(TargetSystemId, contentConflict, out resultMigrationActions);

                if (!resolutionRslt.Resolved)
                {
                    int contentConflictId = contentConflict.InternalId;
                    MigrationConflict chainOnConflictConflict = new MigrationConflict(
                        new ChainOnConflictConflictType(), 
                        MigrationConflict.Status.Unresolved,
                        ChainOnConflictConflictType.CreateConflictDetails(contentConflictId),
                        ChainOnConflictConflictType.CreateScopeHint(contentConflictId));

                    chainOnConflictConflict.ConflictedChangeAction = conflictedActionPairs.Value;

                    ConflictManager.BacklogUnresolvedConflict(TargetSystemId, chainOnConflictConflict, false);
                }
            }
        }
    }
}
