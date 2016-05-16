// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Diagnostics;

namespace MigrationTestLibrary
{
    public class AdapterManager
    {
        private Dictionary<Guid, AdapterHandler> m_adapterHandlers = new Dictionary<Guid, AdapterHandler>();

        public AdapterManager(params DirectoryInfo[] probingDirectories)
        {
            Initialize(probingDirectories);
        }

        /// <summary>
        /// Discovers test case adapters under the specified directories and loads them 
        /// </summary>
        public void Initialize(params DirectoryInfo[] probingDirectories)
        {
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
                                Assembly assembly = Assembly.LoadFrom(file.FullName);

                                // Iterate over all types contained in the assembly
                                foreach (Type type in assembly.GetTypes())
                                {
                                    // Only consider public, concrete types that implement IProvider
                                    if (type.IsPublic && !type.IsAbstract && (type.GetInterface(typeof(ITestCaseAdapter).Name) != null))
                                    {
                                        AdapterHandler handler = AdapterHandler.FromType(type);
                                        if (null == handler || handler.AdapterId == null || handler.AdapterId == Guid.Empty)
                                        {
                                            continue;
                                        }

                                        m_adapterHandlers[handler.AdapterId] = handler;
                                        Trace.TraceInformation("Found TC Adapter {0} {1}", assembly.FullName, handler.AdapterId);
                                    }
                                }
                            }
                            catch (ReflectionTypeLoadException e)
                            {
                                TraceManager.TraceError("A failure occurred while trying to load the {0} Adapter: {1}{2}",
                                    file.FullName, Environment.NewLine, e.ToString());

                                foreach (Exception exception in e.LoaderExceptions)
                                {
                                    Console.WriteLine(exception.Message);
                                }
                            }
                            catch (Exception ex)
                            {
                                TraceManager.TraceError("A failure occurred while trying to load the {0} Adapter: {1}{2}",
                                    file.FullName, Environment.NewLine, ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        public ITestCaseAdapter LoadAdapter(Guid adapterGuid)
        {
            if (m_adapterHandlers.ContainsKey(adapterGuid))
            {
                return m_adapterHandlers[adapterGuid].CreateAdapter();
            }
            else
            {
                throw new FileNotFoundException(String.Format("Failed to find TC Adapter for Guid {0}", adapterGuid));
            }
        }
    }
}
