// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    [ProviderDescription("F2D74BB3-9B1E-45BB-9F16-E665C8BC1AC4", "WIT Server Diff Job", "1.0")]
    public class WITServerDiffJobProvider : IProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ITfsIntegrationJob))
            {
                return new WITServerDiffJob();
            }
            else
            {
                return null;
            }
        }
    }
}
