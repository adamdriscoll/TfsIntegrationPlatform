// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// A base interface is intended to be extended by interfaces specific to a diff operation of a particular type
    /// such as the IWITDiffProvider for performing a diff operation on work items
    /// </summary>
    public interface IDiffProvider : IServiceProvider, IDisposable
    {
        /// <summary>
        /// Initialize method of the diff provider - acquire references to the services provided by the platform.
        /// </summary>
        /// <param name="diffServiceContainer">A service container that the implemenation can use to obtain toolkit services</param>
        void InitializeServices(IServiceContainer diffServiceContainer);

        /// <summary>
        /// Initialize method of the diff provider.
        /// Please implement all the heavy-weight initialization logic here, e.g. server connection.
        /// </summary>
        /// <param name="migrationSource">The MigrationSource associated with this adapter instance</param>
        void InitializeClient(MigrationSource migrationSource);
    }
}
