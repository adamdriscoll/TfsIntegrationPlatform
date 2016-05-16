// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Identifies system used in synchronization.
    /// </summary>
    public enum SystemType
    {
        Tfs,                            // TFS system
        Other,                          // Non-TFS (ACME) system
    }
}
