// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    [Serializable]
    public class WorkItemArtifactType : ArtifactType
    {
        private const string REFERENCE_NAME = "Microsoft.TeamFoundation.Migration.TFS.ArtifactType.WorkItem";
        private const string FRIENDLY_NAME = "Team Foundation Server Work Item artifact type";

        public WorkItemArtifactType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, WellKnownContentType.WorkItem.ReferenceName)
        {}
    }
}