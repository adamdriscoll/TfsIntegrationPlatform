// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
public class MigrationContext 
{
    internal MigrationContext(IServiceContainer tookitServiceContainer, IMigrationProvider migrationProvider)
    {
        TookitServiceContainer = tookitServiceContainer;
    }

    /// <summary>
    /// 
    /// </summary>
    public IServiceContainer TookitServiceContainer { get; internal set; }

    // What else?
}
}
