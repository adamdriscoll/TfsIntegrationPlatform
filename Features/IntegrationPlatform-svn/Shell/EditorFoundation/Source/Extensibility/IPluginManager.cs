// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// Defines the public interface of a Plugin Manager.
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// Gets the Context Provider collection. Context Providers can freely be added and removed at runtime.
        /// </summary>
        ICollection<object> PluginContexts { get; }

        /// <summary>
        /// Gets information about all Plugins that have been loaded by the Plugin Manager.
        /// </summary>
        IEnumerable<PluginDescriptor> LoadedPlugins { get; }

        /// <summary>
        /// Occurs when a Plugin is loaded.
        /// </summary>
        event EventHandler<PluginLoadedEventArgs> PluginLoaded;

        /// <summary>
        /// Occurs when a Plugin is unloaded.
        /// </summary>
        event EventHandler<PluginLoadedEventArgs> PluginUnloaded;
    }
}
