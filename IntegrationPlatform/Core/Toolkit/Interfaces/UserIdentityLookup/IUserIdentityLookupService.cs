// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Provides a mechanism to lookup user identities.
    /// </summary>
    internal interface IUserIdentityLookupService : IUserIdentityFactory
    {
        /// <summary>
        /// Based on the context and the user Id mapping configuration, translate a source (original)
        /// user identity to a target (translated) user identity.
        /// </summary>
        /// <param name="originalUserIdentity"></param>
        /// <param name="context"></param>
        /// <param name="translatedUserIdentity"></param>
        /// <returns>TRUE if lookup succeeded; FALSE otherwise.</returns>
        bool TryLookup(RichIdentity originalUserIdentity, IdentityLookupContext context, out RichIdentity translatedUserIdentity);

        /// <summary>
        /// Registers a lookup service provider.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="provider"></param>
        void RegisterLookupServiceProvider(Guid referenceName, IUserIdentityLookupServiceProvider provider);
    }
}
