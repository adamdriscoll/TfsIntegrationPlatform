// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The interface that top-level migration providers will implement.
    /// </summary>
    public interface IMigrationProvider : ISettingsSection, IDisposable
    {
        /// <summary>
        /// Begins the migration process implemented by this provider.
        /// </summary>
        void Start();

        /// <summary>
        /// Aborts the running migration process implemented by this provider
        /// </summary>
        void Abort();

        /// <summary>
        /// Stops the running migration process implemented by this provider
        /// </summary>
        void Stop();

        /// <summary>
        /// Does a single synchronization pass.
        /// </summary>
        /// <param name="primarySystem">System which will be queried for updates</param>
        void Synchronize(SystemType primarySystem);

        /// <summary>
        /// Does a two-way synchronizaion pass.
        /// </summary>
        void SynchronizeFull();
    }
}
