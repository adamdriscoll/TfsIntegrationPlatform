// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class ArtifactComparer : IComparer<IArtifact>
    {
        public int Compare(IArtifact x, IArtifact y)
        {
            int pos = x.ArtifactType.ReferenceName.CompareTo(y.ArtifactType.ReferenceName);
            if (pos != 0) return pos;

            return string.Compare(
                x.Uri,
                y.Uri,
                StringComparison.InvariantCultureIgnoreCase);
        }
    }
}