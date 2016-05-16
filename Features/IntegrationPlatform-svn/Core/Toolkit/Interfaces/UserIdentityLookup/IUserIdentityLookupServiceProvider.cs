// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public interface IUserIdentityLookupServiceProvider
    {
        /// <summary>
        /// Look up the user identity based on the properties of the richIdentity. If
        /// the user identity is found, update richIdentity's properties
        /// </summary>
        /// <param name="richIdentity"></param>
        /// <returns></returns>
        bool TryLookup(RichIdentity richIdentity, IdentityLookupContext context);
    }
}
