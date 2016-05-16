// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts.HistoryNotFoundResolution
{
    internal class ItemConversionHistory
    {
        public ItemConversionHistory(
            string sourceItemId,
            string sourceItemVersion,
            string targetItemId,
            string targetItemVersion)
        {
            SourceItemId = sourceItemId;
            SourceItemVersion = sourceItemVersion;
            TargetItemId = targetItemId;
            TargetItemVersion = targetItemVersion;
        }

        public string SourceItemId { get; set; }
        public string SourceItemVersion { get; set; }
        public string TargetItemId { get; set; }
        public string TargetItemVersion { get; set; }

    }
}
