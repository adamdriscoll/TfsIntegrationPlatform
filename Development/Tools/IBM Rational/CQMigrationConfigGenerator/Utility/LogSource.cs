// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Enum representing logging sources.

namespace Microsoft.TeamFoundation.Converters.Utility
{
    /// <summary>
    /// Enum representing sources used for logging
    /// </summary>
    internal enum LogSource
    {
        // BUGBUG:: If the list grows too big, consider adding #define.
        Common,     // Common pieces like CommandLine, Reporting
        CQ,         // ClearQuest
        WorkItemTracking,
        VersionControl,
        PS,         // Product Studio
        SD,         // Source Depot
        VSS,        // Visual Source Safe
		TXT,        // Txt (CSV & XML)

        // add your source above in sorted order
    }
}
