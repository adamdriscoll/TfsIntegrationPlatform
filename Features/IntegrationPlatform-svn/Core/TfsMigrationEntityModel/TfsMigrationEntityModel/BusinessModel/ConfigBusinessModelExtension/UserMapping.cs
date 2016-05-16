// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    public partial class UserMapping
    {
        public override string ToString()
        {
            return string.Format("/{0}/{1}/{2}/{3}",
                LeftUser.Alias ?? string.Empty, LeftUser.Domain ?? string.Empty,
                RightUser.Alias ?? string.Empty, RightUser.Domain ?? string.Empty);
        }
    }
}
