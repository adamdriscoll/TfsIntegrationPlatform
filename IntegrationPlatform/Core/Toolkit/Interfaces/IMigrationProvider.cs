// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The interface that top-level migration providers will implement.
    /// </summary>
    public interface IMigrationProvider : IServiceProvider, IDisposable
    {
        /// <summary>
        /// Process a changegroup.
        /// </summary>
        /// <param name="changeGroup"></param>
        ConversionResult ProcessChangeGroup(ChangeGroup changeGroup);

        /// <summary>
        /// Initialize method of the migration provider - acquire references to the services provided by the platform.
        /// </summary>
        void InitializeServices(IServiceContainer analysisServiceContainer);

        /// <summary>
        /// Initialize method of the migration provider.
        /// Please implement all the heavey-weight initialization logic here, e.g. server connection.
        /// </summary>
        void InitializeClient();

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        void RegisterConflictTypes(ConflictManager conflictManager);

        /// <summary>
        /// Establish the context based on the context info from the side of the pipeline
        /// </summary>
        void EstablishContext(ChangeGroupService sourceSystemChangeGroupService);
    }
}
