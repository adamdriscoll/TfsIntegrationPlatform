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
    /// Public interface to get an Add-In object.
    /// </summary>
    public interface IAddinManagementService
    {
        /// <summary>
        /// Returns the list of registered Addins successfully loaded from the Plugins directory
        /// </summary>
        IAddin[] RegisteredAddins { get; }

        /// <summary>
        /// Gets the loaded Add-In by its reference name.
        /// </summary>
        /// <param name="referenceName">The reference name of the Add-In to get</param>
        /// <returns>An Add-in object that has the requested reference name; NULL if the Add-In is unknown.</returns>
        IAddin GetAddin(Guid referenceName);

        /// <summary>
        /// Enumerate all of the AnalysisAddins configured for a given migration source
        /// </summary>
        IEnumerable<AnalysisAddin> GetMigrationSourceAnalysisAddins(Guid migrationSourceId);

        /// <summary>
        /// Enumerate all of the MigrationAddins configured for a given migration source
        /// </summary>
        IEnumerable<MigrationAddin> GetMigrationSourceMigrationAddins(Guid migrationSourceId);
    }
}
