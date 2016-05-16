// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    interface IUserIdMappingRuleEvaluator
    {
        /// <summary>
        /// Try map the user identity based on the mappings rules in the configuration
        /// </summary>
        /// <param name="sourceUserIdentity"></param>
        /// <param name="context"></param>
        /// <param name="mappedUserIdentity"></param>
        /// <returns>True if a mapping rule is applied to the sourceUserIdentity; FALSE otherwise</returns>
        bool TryMapUserIdentity(RichIdentity sourceUserIdentity, IdentityLookupContext context, RichIdentity mappedUserIdentity);
    }
}
