// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class ProviderManager
    {
        private AddinManagementService m_AddinManagementService = new AddinManagementService();

        public Configuration Configuration { get; set; }

        public ProviderManager(Configuration config)
        {
            Configuration = config;
        }

        internal AddinManagementService AddinManagementService
        {
            get
            {
                return m_AddinManagementService;
            }
        }

        /// <summary>
        /// Discovers providers under the specified directories and loads them if used in MigrationSources 
        /// defined in the configuration. It instantiates a unique provider instance for each MigrationSource.
        /// </summary>
        public Dictionary<Guid, ProviderHandler> LoadProvider(params DirectoryInfo[] probingDirectories)
        {
            IEnumerable<ProviderHandler> providers = Utility.LoadProvider(probingDirectories);

            // Initialize a list that will contain all plugin types discovered
            Dictionary<Guid, ProviderHandler> providerHandlers = new Dictionary<Guid, ProviderHandler>();

            foreach (ProviderHandler handler in providers)
            {
                try
                {
                    #region Register Add-Ins
                    IProvider provider = handler.Provider;
                    IAddin addIn = provider.GetService(typeof(IAddin)) as IAddin;
                    if (null != addIn)
                    {
                        m_AddinManagementService.RegisterAddin(addIn);
                    }
                    #endregion

                    Guid[] sourceIds = GetMigrationSourceId(handler.ProviderId);
                    if (sourceIds == null || sourceIds.Length == 0)
                    {
                        continue;
                    }

                    // try persist provider information to db
                    handler.FindSaveProvider();

                    // create a unique provider instance for each migration source
                    foreach (Guid migrationSource in sourceIds)
                    {
                        ProviderHandler providerHandler = new ProviderHandler(handler);

                        if (!providerHandlers.ContainsKey(migrationSource))
                        {
                            providerHandlers.Add(migrationSource, providerHandler);
                            TraceManager.TraceInformation("Provider {0} {1} is loaded", providerHandler.ProviderName, handler.ProviderId.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceManager.TraceError("A failure occurred while trying to load the {0} Provider: {1}{2}",
                                handler.ProviderName, Environment.NewLine, ex.ToString());
                }
            }

            return providerHandlers;
        }

        // returns an array of migration source id associated with the given provider id
        private Guid[] GetMigrationSourceId(Guid providerId)
        {
            List<Guid> ids = new List<Guid>();

            foreach (MigrationSource source in Configuration.SessionGroup.MigrationSources.MigrationSource)
            {
                if (providerId.Equals(new Guid(source.ProviderReferenceName)))
                {
                    ids.Add(new Guid(source.InternalUniqueId));    
                }
            }

            return ids.ToArray();
        }
    }
}
