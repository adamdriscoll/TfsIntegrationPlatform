// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BM = Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// This helper class understands the policy settings in the error management configuration
    /// section, and helps create the corresponding ErrorRoutingPolicy instances.
    /// </summary>
    static class ErrorRoutingPolicyFactory
    {
        public static ErrorRoutingPolicy CreateRoutingPolicy(BM.Policy policyConfig)
        {
            // policyConfig can be NULL, in which case create a default policy
            if (null == policyConfig)
            {
                return new MaxOccurrenceErrorRoutingPolicy(MaxOccurrenceErrorRoutingPolicy.DefaultMaxOccurrence);
            }

            return new MaxOccurrenceErrorRoutingPolicy(policyConfig.OccurrenceCount);
        }
    }
}
