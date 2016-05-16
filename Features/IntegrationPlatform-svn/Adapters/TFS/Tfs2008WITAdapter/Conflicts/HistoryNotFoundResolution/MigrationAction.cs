// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts.HistoryNotFoundResolution
{
    internal class MigrationAction
    {
        public string SourceWorkItemId { get; set; }
        public TfsWITRecordDetails RecordDetails { get; set; }
        public TfsConstants.ChangeActionId ChangeActionId { get; set; }

        public MigrationAction(
            string sourceWorkItemId,
            TfsWITRecordDetails recordDetails,
            TfsConstants.ChangeActionId changeActionId)
        {
            SourceWorkItemId = sourceWorkItemId;
            ChangeActionId = changeActionId;
            RecordDetails = recordDetails;
        }
    }
}
