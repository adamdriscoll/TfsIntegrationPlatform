// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class LinkComparer : IComparer<ILink>
    {
        public int Compare(ILink x, ILink y)
        {
            if (null == x)
            {
                throw new ArgumentNullException("x");
            }

            if (null == y)
            {
                throw new ArgumentNullException("y");
            }

            int result = string.Compare(x.LinkType.ReferenceName, y.LinkType.ReferenceName);
            if (result != 0) return result;

            result = string.Compare(x.SourceArtifact.Uri, y.SourceArtifact.Uri);
            if (result != 0) return result;

            result = string.Compare(x.TargetArtifact.Uri, y.TargetArtifact.Uri);
            return result;
        }
    }
}