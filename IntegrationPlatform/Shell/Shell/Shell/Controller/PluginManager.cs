// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorFoundation.Extensibility;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;


namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// This is a variation of EditorFoundation.Extensibility.PluginManager that
    /// tries to load plugins from the AppBase/PrivateBins path of the AppDomain
    /// before looking to configured probing paths.  AppBase/PrivateBins is given
    /// precedence because it results in a Load context for the assembly rather
    /// than a LoadFrom context.
    /// </summary>

    public class LoadContextPluginManager : EditorFoundation.Extensibility.PluginManager
    {
        private static PluginHandler[] DiscoverPlugins(DirectoryInfo[] probingDirectories)
        {
            // Initialize a list that will contain all plugin types discovered
            List<PluginHandler> pluginHandlers = new List<PluginHandler>();

            if (probingDirectories != null)
            {
                // Iterate over the probing directories and look for plugins
                foreach (DirectoryInfo directory in probingDirectories)
                {
                    if (directory.Exists)
                    {
                        // Try to load plugins from each dll
                        foreach (FileInfo file in directory.GetFiles("*.dll"))
                        {
                            try
                            {
                                // Load the dll into an assembly
                                Assembly assembly = Assembly.Load(file.FullName);

                                // Iterate over all types contained in the assembly
                                foreach (Type type in assembly.GetTypes())
                                {
                                    // Only consider public, concrete types that implement IPlugin
                                    if (type.IsPublic && !type.IsAbstract && typeof(IPlugin).IsAssignableFrom(type))
                                    {
                                        PluginHandler pluginHandler = PluginHandler.FromType(type);
                                        if (pluginHandler != null)
                                        {
                                            pluginHandlers.Add(pluginHandler);
                                        }
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                // TODO: Properly report error
                                //Utilities.DefaultTraceSource.TraceEvent(TraceEventType.Error, 0, "A failure occurred while trying to load the {0} Plugin: {1}{2}", file.FullName, Environment.NewLine, exception.ToString());
                            }
                        }
                    }
                }
            }

            // Return the list of plugin types discovered
            return pluginHandlers.ToArray();
        }
    }
}
