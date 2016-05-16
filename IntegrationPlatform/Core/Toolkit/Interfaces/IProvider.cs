// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// IProvider must be implemented by all adapters
    /// for the framework to recognize it as a legal provider
    /// </summary>
    public interface IProvider : IServiceProvider
    {
    }
}
