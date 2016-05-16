// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking
{
    [Serializable]
    public class ChangeListArtifactType : ArtifactType
    {
        private const string REFERENCE_NAME = "Microsoft.TeamFoundation.Migration.TFS.ArtifactType.Changeset";
        private const string FRIENDLY_NAME = "Team Foundation Server Changeset artifact type";

        public ChangeListArtifactType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, WellKnownContentType.VersionControlChangeGroup.ReferenceName)
        {}        
    }
}