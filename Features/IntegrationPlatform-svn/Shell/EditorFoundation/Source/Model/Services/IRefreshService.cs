// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public delegate void RefreshCallback();

    interface IRefreshService
    {
        /// <summary>
        /// Delay between calls to RefreshCallback delegates in milliseconds.
        /// </summary>
        int RefreshIntervalMilliseconds { get; set; }

        void Pause();
        void Resume();

        /// <summary>
        /// This event is used to signal to consumers of service that it is time
        /// to refresh data.
        /// </summary>
        event EventHandler AutoRefresh;

        /// <summary>
        /// Event fired when refresh service goes into paused state.
        /// </summary>
        event EventHandler RefreshPause;

        /// <summary>
        /// Event fired when refresh service goes into resumed state.
        /// </summary>
        event EventHandler RefreshResume;
    }
}
