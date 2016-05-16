// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
public class AnalysisContext 
{
    internal AnalysisContext(
        IServiceContainer tookitServiceContainer)
    {
        TookitServiceContainer = tookitServiceContainer;
    }

    /// <summary>
    /// A ServiceContainer that contains services provided by the Toolkit that are available to AnalysisAddins
    /// </summary>
    public IServiceContainer TookitServiceContainer { get; internal set; }

}
}
