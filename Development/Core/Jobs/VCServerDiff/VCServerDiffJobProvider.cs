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
    [ProviderDescription("60087491-FF28-4544-88DB-BE25F22B8FE8", "VC Server Diff Job", "1.0")]
    public class VCServerDiffJobProvider : IProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ITfsIntegrationJob))
            {
                return new VCServerDiffJob();
            }
            else
            {
                return null;
            }
        }
    }
}
