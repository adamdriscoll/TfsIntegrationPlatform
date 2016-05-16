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
    [ProviderDescription("1DF002B3-0669-4811-8734-25D90010EA3C", "Sync Monitor Job", "1.0")]
    public class SyncMonitorJobProvider : IProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ITfsIntegrationJob))
            {
                return new SyncMonitorJob();
            }
            else
            {
                return null;
            }
        }
    }
}
