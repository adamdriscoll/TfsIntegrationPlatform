// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// Provides data for the PluginLoaded event.
    /// </summary>
    public class PluginLoadedEventArgs : EventArgs
    {
        #region Fields
        private PluginDescriptor plugin;
        #endregion

        #region Constructors
        internal PluginLoadedEventArgs (PluginDescriptor plugin)
        {
            this.plugin = plugin;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the PluginDescriptor for the Plugin that was loaded.
        /// </summary>
        public PluginDescriptor Plugin
        {
            get
            {
                return this.plugin;
            }
        }
        #endregion
    }
}
