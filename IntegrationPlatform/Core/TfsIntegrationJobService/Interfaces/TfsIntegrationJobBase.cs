// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    /// <summary>
    /// This class provides basic implementation of ITfsIntegrationJob
    /// </summary>
    public abstract class TfsIntegrationJobBase : ITfsIntegrationJob
    {
        /// <summary>
        /// Gets the reference name of the job
        /// </summary>
        public abstract Guid ReferenceName { get; }

        /// <summary>
        /// Gets the friendly name of the job
        /// </summary>
        public abstract string FriendlyName { get; }

        /// <summary>
        /// Initialize the job with the settings in TfsIntegrationJobService.config
        /// </summary>
        /// <param name="jobConfiguration"></param>
        public abstract void Initialize(Job jobConfiguration);

        /// <summary>
        /// Implement the job logics in this method
        /// </summary>
        protected abstract void DoJob();

        /// <summary>
        /// Starts the job
        /// </summary>
        public virtual void Run()
        {
            try
            {
                DoJob();
            }
            catch (Exception e)
            {
                TraceManager.TraceError(e.ToString());
            }
        }

        /// <summary>
        /// Stops the job
        /// </summary>
        public virtual void Stop()
        {
            return;
        }
    }
}
