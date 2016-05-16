// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// This class discovers and loads Plugins. It also exposes information about
    /// the loaded Plugins, and exposes an interface for invoking Plugin commands.
    /// </summary>
    public class PluginManager : IPluginManager
    {
        #region Fields
        private readonly PluginHandler[] pluginHandlers;
        private readonly PluginContextCollection pluginContexts;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager"/> class.
        /// </summary>
        /// <remarks>
        /// Plugin probing directories are read from the application configuration file.
        /// </remarks>
        public PluginManager () : this (PluginManager.GetPluginDirectoriesFromConfig ())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager"/> class.
        /// </summary>
        /// <param name="probingDirectories">
        /// Specifies directories to probe for Plugins.
        /// </param>
        public PluginManager (params DirectoryInfo[] probingDirectories)
        {
            this.pluginHandlers = PluginManager.DiscoverPlugins (probingDirectories);
            this.pluginContexts = new PluginContextCollection (this);
            
            foreach (PluginHandler pluginHandler in this.pluginHandlers)
            {
                pluginHandler.Loaded += this.OnPluginLoaded;
                pluginHandler.Unloaded += this.OnPluginUnloaded;
            }
        }

        public IEnumerable<IMigrationSourceView> GetMigrationSourceViews()
        {
            List<IMigrationSourceView> migrationSourceViews = new List<IMigrationSourceView>();

            foreach (PluginHandler pluginHandler in pluginHandlers)
            {
                migrationSourceViews.Add(pluginHandler.GetMigrationSourceView());
            }

            return migrationSourceViews;
        }

        public IEnumerable<IConflictTypeView> GetConflictTypes(Guid providerId)
        {
            try
            {
                foreach (PluginHandler pluginHandler in pluginHandlers)
                {
                    if (pluginHandler.Descriptor.Id.Equals(providerId))
                    {
                        return pluginHandler.GetConflictTypeViews();
                    }
                }
            }
            catch (NotImplementedException)
            { }

            return null;
        }

        public ExecuteFilterStringExtension GetFilterStringExtension(Guid providerId)
        {
            try
            {
                foreach (PluginHandler pluginhandler in pluginHandlers)
                {
                    if (pluginhandler.Descriptor.Id.Equals(providerId))
                    {
                        return pluginhandler.FilterStringExtension;
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets the Context Provider collection. Context Providers can freely be added and removed at runtime.
        /// </summary>
        public ICollection<object> PluginContexts
        {
            get
            {
                return this.pluginContexts;
            }
        }

        /// <summary>
        /// Gets information about all Plugins that have been loaded by the Plugin Manager.
        /// </summary>
        public IEnumerable<PluginDescriptor> LoadedPlugins
        {
            get
            {
                foreach (PluginHandler pluginHandler in this.LoadedPluginHandlers)
                {
                    yield return pluginHandler.Descriptor;
                }
            }
        }

        private IEnumerable<PluginHandler> LoadedPluginHandlers
        {
            get
            {
                foreach (PluginHandler pluginHandler in this.pluginHandlers)
                {
                    if (pluginHandler.IsLoaded)
                    {
                        yield return pluginHandler;
                    }
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a Plugin is loaded.
        /// </summary>
        public event EventHandler<PluginLoadedEventArgs> PluginLoaded;

        /// <summary>
        /// Occurs when a Plugin is unloaded.
        /// </summary>
        public event EventHandler<PluginLoadedEventArgs> PluginUnloaded;
        #endregion

        #region Private Methods
        private void OnPluginContextAdded (object context)
        {
            foreach (PluginHandler pluginHandler in this.pluginHandlers)
            {
                if (pluginHandler.SupportsContext (context.GetType ()))
                {
                    pluginHandler.OnContextEnter (context);
                }
            }
        }

        private void OnPluginContextRemoved (object context)
        {
            foreach (PluginHandler pluginHandler in this.LoadedPluginHandlers)
            {
                if (pluginHandler.SupportsContext (context.GetType ()))
                {
                    pluginHandler.OnContextLeave (context);
                }
            }
        }

        private static DirectoryInfo[] GetPluginDirectoriesFromConfig ()
        {
            List<DirectoryInfo> pluginDirectories = new List<DirectoryInfo> (Properties.Settings.Default.PluginDirectories.Count);
            foreach (string pluginDirectory in Properties.Settings.Default.PluginDirectories)
            {
                // Expand environment variables
                string resolvedPluginDirectory = Environment.ExpandEnvironmentVariables (pluginDirectory);

                // If the directory is not rooted, make it relative to the application path
                if (!Path.IsPathRooted (resolvedPluginDirectory))
                {
                    resolvedPluginDirectory = Path.Combine (System.Windows.Forms.Application.StartupPath, resolvedPluginDirectory);
                }

                // Add the directory to the running list
                pluginDirectories.Add (new DirectoryInfo (resolvedPluginDirectory));
            }

            return pluginDirectories.ToArray ();
        }

        private static PluginHandler[] DiscoverPlugins (DirectoryInfo[] probingDirectories)
        {
            // Initialize a list that will contain all plugin types discovered
            List<PluginHandler> pluginHandlers = new List<PluginHandler> ();

            if (probingDirectories != null)
            {
                // Iterate over the probing directories and look for plugins
                foreach (DirectoryInfo directory in probingDirectories)
                {
                    if (directory.Exists)
                    {
                        // Try to load plugins from each dll
                        foreach (FileInfo file in directory.GetFiles ("*.dll"))
                        {
                            try
                            {
                                // Load the dll into an assembly
                                Assembly assembly = Assembly.LoadFrom (file.FullName);

                                // Iterate over all types contained in the assembly
                                foreach (Type type in assembly.GetTypes ())
                                {
                                    // Only consider public, concrete types that implement IPlugin
                                    if (type.IsPublic && !type.IsAbstract && typeof (IPlugin).IsAssignableFrom (type))
                                    {
                                        PluginHandler pluginHandler = PluginHandler.FromType (type);
                                        if (pluginHandler != null)
                                        {
                                            pluginHandlers.Add (pluginHandler);
                                        }
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Utilities.DefaultTraceSource.TraceEvent (TraceEventType.Error, 0, "A failure occurred while trying to load the {0} Plugin: {1}{2}", file.FullName, Environment.NewLine, exception.ToString ());
                            }
                        }
                    }
                }
            }

            // Return the list of plugin types discovered
            return pluginHandlers.ToArray ();
        }

        private void OnPluginLoaded (object sender, EventArgs e)
        {
            PluginHandler pluginHandler = (PluginHandler)sender;
            if (this.PluginLoaded != null)
            {
                this.PluginLoaded (this, new PluginLoadedEventArgs (pluginHandler.Descriptor));
            }
        }

        private void OnPluginUnloaded (object sender, EventArgs e)
        {
            PluginHandler pluginHandler = (PluginHandler)sender;
            if (this.PluginUnloaded != null)
            {
                this.PluginUnloaded (this, new PluginLoadedEventArgs (pluginHandler.Descriptor));
            }
        }
        #endregion

        #region Classes
        private class PluginContextCollection : Collection<object>
        {
            #region Fields
            private PluginManager pluginManager;
            #endregion

            #region Constructors
            public PluginContextCollection (PluginManager pluginManager)
            {
                this.pluginManager = pluginManager;
            }
            #endregion

            #region Protected Methods
            protected override void InsertItem (int index, object context)
            {
                base.InsertItem (index, context);
                this.pluginManager.OnPluginContextAdded (context);
            }

            protected override void RemoveItem (int index)
            {
                object context = this[index];
                base.RemoveItem (index);
                this.pluginManager.OnPluginContextRemoved (context);
            }

            protected override void SetItem (int index, object newContext)
            {
                object oldContext = this[index];
                base.SetItem (index, newContext);
                this.pluginManager.OnPluginContextRemoved (oldContext);
                this.pluginManager.OnPluginContextAdded (newContext);
            }

            protected override void ClearItems ()
            {
                foreach (object context in this)
                {
                    this.pluginManager.OnPluginContextRemoved (context);
                }

                base.ClearItems ();
            }
            #endregion
        }
        #endregion
    }
}
