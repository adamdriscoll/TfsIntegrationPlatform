// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    /// <summary>
    /// This is the interface that every TFS Integration job must implement
    /// </summary>
    /// <remarks>
    /// Default implementation is provided in TfsIntegrationJobBase.cs.
    /// </remarks>
    public interface ITfsIntegrationJob
    {
        /// <summary>
        /// Gets the reference name of the job
        /// </summary>
        Guid ReferenceName { get; }

        /// <summary>
        /// Gets the friendly name of the job
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Initialize the job with the settings in TfsIntegrationJobService.config
        /// </summary>
        /// <param name="jobConfiguration"></param>
        void Initialize(Job jobConfiguration);

        /// <summary>
        /// Starts the job
        /// </summary>
        void Run();

        /// <summary>
        /// Stops the job
        /// </summary>
        void Stop();
    }
}
