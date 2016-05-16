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
    /// An interface that adapters may optionally implement to support monitoring of the backlog of items that
    /// have been changed in the server that the adapter is supporting but have not yet been changed in the
    /// peer server.
    /// </summary>
    public interface ISyncMonitorProvider : IServiceProvider, IDisposable
    {
        /// <summary>
        /// Initialize method of the diff provider - acquire references to the services provided by the platform.
        /// </summary>
        void InitializeServices(IServiceContainer syncMonitorServiceContainer);

        /// <summary>
        /// Initialize method of the monitor provider.
        /// Please implement any heavy-weight initialization logic here, e.g. server connection.
        /// </summary>
        /// <param name="migrationSource">The MigrationSource associated with this adapter instance</param>
        void InitializeClient(MigrationSource migrationSource);

        /// <summary>
        /// Given a MigrationItemId structure in the format recognizable to the specific adapter,
        /// this method returns a struct with summary data about the changes that have occurred
        /// since (but NOT including) the change that created the version of the item specified by changedItemId.  
        /// The scope of the changes returned is limited to the tree or sub-tree identified by the 
        /// session configuration which the adapter can obtain from the service container passed
        /// to InitializeServices.
        /// </summary>
        /// <param name="changedItemId">A string that identifies a specific change in the migration source; the format of the string is adapter specific
        /// and matches the format of the strings that the adapter uses for the Name property of the ChangeGroups that it creates</param>
        /// <param name="filterStrings">The list of filter strings that were configured for this migration source when the migration session was run</param>
        /// <returns></returns>
        ChangeSummary GetSummaryOfChangesSince(string changedItemId, List<string> filterStrings);
    }
}
