// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    public class ShellUtil
    {
        public static Guid NormalizeUniqueId(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                throw new ArgumentNullException("uniqueId");
            }
            return new Guid(uniqueId);
        }
    }
}
