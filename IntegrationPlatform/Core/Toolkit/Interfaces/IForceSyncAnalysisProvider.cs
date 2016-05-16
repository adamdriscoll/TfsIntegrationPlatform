// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The common analysis interface that all provider needs to implement.
    /// </summary>
    public interface IForceSyncAnalysisProvider
    {
        /// <summary>
        /// Generate the delta table.
        /// </summary>
        void GenerateDeltaForForceSync(IEnumerable<string> itemIdsToForceSync);
    }
}
