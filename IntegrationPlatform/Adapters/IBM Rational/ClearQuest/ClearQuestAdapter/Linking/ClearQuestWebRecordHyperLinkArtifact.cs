// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Linking
{
    /// <summary>
    /// Artifact Type for the ClearQuest Web Record Urls
    /// </summary>
    [Serializable]
    public class ClearQuestWebRecordHyperLinkArtifactType : ArtifactType
    {
        private const string REFERENCE_NAME = "ClearQuestAdapter.ArtifactType.Web.RecordHyperLink";
        private const string FRIENDLY_NAME = "ClearQuest Web Record Hyper Link Artifact";

        public ClearQuestWebRecordHyperLinkArtifactType()
            : base(REFERENCE_NAME, FRIENDLY_NAME, WellKnownContentType.GenericContent.ReferenceName)
        { }
    }
}
