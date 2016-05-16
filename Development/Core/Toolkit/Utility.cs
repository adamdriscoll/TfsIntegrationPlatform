// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class Utility
    {
        private static ICollection<ProviderHandler> s_providers;

        public static ICollection<ProviderHandler> LoadProvider(params DirectoryInfo[] probingDirectories)
        {
            if (s_providers == null || s_providers.Count == 0)
            {
                // Initialize a list that will contain all plugin types discovered
                s_providers = new List<ProviderHandler>();

                if (probingDirectories != null)
                {
                    // Iterate over the probing directories and look for plugins
                    foreach (DirectoryInfo directory in probingDirectories)
                    {
                        Debug.Assert(directory.Exists, string.Format("Plugins directory does not exist: {0}", directory.FullName));
                        if (directory.Exists)
                        {
                            // Try to load plugins from each dll
                            foreach (FileInfo file in directory.GetFiles("*.dll"))
                            {
                                try
                                {
                                    // Load the dll into an assembly
                                    Assembly assembly = Assembly.LoadFrom(file.FullName);

                                    // Iterate over all types contained in the assembly
                                    foreach (Type type in assembly.GetTypes())
                                    {
                                        // Only consider public, concrete types that implement IProvider
                                        if (type.IsPublic && !type.IsAbstract && (type.GetInterface(typeof(IProvider).Name) != null))
                                        {
                                            ProviderHandler handler = ProviderHandler.FromType(type);
                                            if (null == handler
                                                || handler.ProviderId == null
                                                || handler.ProviderId == Guid.Empty)
                                            {
                                                continue;
                                            }

                                            s_providers.Add(handler);
                                            TraceManager.TraceInformation("Provider {0} {1} is available", handler.ProviderName, handler.ProviderId.ToString());
                                        }
                                    }
                                }
                                catch (ReflectionTypeLoadException)
                                {
                                    TraceManager.TraceInformation("Provider {0} is unavailable.  One or more of the requested types cannot be loaded.", file.FullName);
                                }
                                catch (Exception)
                                {
                                    // We try to load all .dll files and some of them just are not providers.
                                    // Just log a warning and move along
                                    TraceManager.TraceWarning("Failed to load the possible provider file: {0}", file.FullName);
                                }
                            }
                        }
                    }
                }
            }
            return s_providers;
        }

        public static ConflictResolutionResult ResolveConflictByUpdateConfig(ConflictResolutionRule rule)
        {
            ConflictResolutionResult rslt = new ConflictResolutionResult(true, ConflictResolutionType.UpdatedConfiguration);
            string newConfigurationId;
            if (rule.DataFieldDictionary.TryGetValue(Constants.DATAKEY_UPDATED_CONFIGURATION_ID, out newConfigurationId))
            {
                return rslt;
            }
            else
            {
                Debug.Assert(false, "DATAKEY_UPDATED_CONFIGURATION_ID is not included in the resolution rule");
                return new ConflictResolutionResult(false, ConflictResolutionType.Other);
            }
        }
    }
}
