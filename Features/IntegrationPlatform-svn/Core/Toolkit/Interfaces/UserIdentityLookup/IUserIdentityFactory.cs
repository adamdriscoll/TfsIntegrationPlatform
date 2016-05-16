// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Provides a factory to create RichIdentity instances.
    /// </summary>
    internal interface IUserIdentityFactory
    {
        /// <summary>
        /// Creates a RichdIdentity instance.
        /// </summary>
        /// <returns></returns>
        RichIdentity CreateUserIdentity();
    }
}
