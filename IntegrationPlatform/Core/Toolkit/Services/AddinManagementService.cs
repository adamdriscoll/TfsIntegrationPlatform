// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// Add-In manager.
    /// </summary>
    internal class AddinManagementService : IAddinManagementService
    {
        Dictionary<Guid, IAddin> m_addIns = new Dictionary<Guid, IAddin>();

        Dictionary<Guid, List<AnalysisAddin>> m_analysisAddinsByMigrationSource = new Dictionary<Guid, List<AnalysisAddin>>();

        Dictionary<Guid, List<MigrationAddin>> m_migrationAddinsByMigrationSource = new Dictionary<Guid, List<MigrationAddin>>();

        /// <summary>
        /// Registers an Add-In
        /// </summary>
        /// <param name="addIn"></param>
        public void RegisterAddin(IAddin addIn)
        {
            if (null == addIn)
            {
                throw new ArgumentNullException("addIn");
            }

            if (!m_addIns.ContainsKey(addIn.ReferenceName))
            {
                m_addIns.Add(addIn.ReferenceName, addIn);
            }
        }

        /// <summary>
        /// Gets the count of the registered Add-Ins
        /// </summary>
        public int Count
        {
            get
            {
                return m_addIns.Count;
            }
        }
        
        public void ValidateRegisteredAddins(NotifyingCollection<AddinElement> addinElemCollection)
        {
            List<Guid> configuredAndLoadedAddins = new List<Guid>(addinElemCollection.Count);
            Dictionary<Guid, AddinElement> configuredButNotLoadedAddins = new Dictionary<Guid,AddinElement>();
            foreach (var addinElem in addinElemCollection)
            {
                Guid addinRefName = new Guid(addinElem.ReferenceName);

                if (!m_addIns.ContainsKey(addinRefName))
                {
                    // this configured add-in is not loaded
                    if (!configuredButNotLoadedAddins.ContainsKey(addinRefName))
                    {
                        configuredButNotLoadedAddins.Add(addinRefName, addinElem);
                    }
                }
                else if (!configuredAndLoadedAddins.Contains(addinRefName))
                {
                    // this add-in is configured and loaded 
                    configuredAndLoadedAddins.Add(addinRefName);
                }
            }

            List<Guid> loadedButNotConfiguredAddins = new List<Guid>(m_addIns.Count);
            foreach (Guid loadedAddin in m_addIns.Keys)
            {
                if (!configuredAndLoadedAddins.Contains(loadedAddin))
                {
                    // this loaded add-in is not configured for use
                    if (!loadedButNotConfiguredAddins.Contains(loadedAddin))
                    {
                        loadedButNotConfiguredAddins.Add(loadedAddin);
                    }
                }
            }

            if (configuredButNotLoadedAddins.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Some Add-ins are configured for use but not loaded.");
                foreach (var configButNotLoadedAddin in configuredButNotLoadedAddins)
                {
                    sb.AppendFormat("{0} ({1})\n",
                        configButNotLoadedAddin.Value.FriendlyName,
                        configButNotLoadedAddin.Key.ToString());
                }

                throw new MigrationException(sb.ToString());
            }
        }

        public IAddin[] RegisteredAddins
        {
            get
            {
                return m_addIns.Values.ToArray();
            }
        }

        public void RegisterMigrationSourceAddins(Dictionary<Guid, MigrationSource> migrationSources)
        {
            foreach (KeyValuePair<Guid, MigrationSource> keyValuePair in migrationSources)
            {
                if (keyValuePair.Value.Settings != null && keyValuePair.Value.Settings.Addins != null)
                {
                    foreach (AddinElement addinElement in keyValuePair.Value.Settings.Addins.Addin)
                    {
                        IAddin addin = GetAddin(new Guid(addinElement.ReferenceName));

                        AnalysisAddin analysisAddin = addin as AnalysisAddin;
                        if (analysisAddin != null)
                        {
                            RegisterMigrationSourceAnalysisAddin(keyValuePair.Key, keyValuePair.Value, analysisAddin);
                        }

                        MigrationAddin migrationAddin = addin as MigrationAddin;
                        if (migrationAddin != null)
                        {
                            RegisterMigrationSourceMigrationAddin(keyValuePair.Key, keyValuePair.Value, migrationAddin);
                        }
                    }
                }
               
                // BEGIN SUPPORT FOR COMPATABILITY WITH OLD CONFIG FILES
                try
                {
                    List<string> analysisAddinNames = new List<string>();

                    foreach (CustomSetting setting in keyValuePair.Value.CustomSettings.CustomSetting)
                    {
                        if (setting.SettingKey.StartsWith(MigrationToolkitResources.AnalysisAddinKeyName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            analysisAddinNames.Add(setting.SettingValue);
                        }
                    }

                    foreach (string analysisAddinName in analysisAddinNames)
                    {
                        AnalysisAddin analysisAddin = GetAddin(new Guid(analysisAddinName)) as AnalysisAddin;
                        if (analysisAddin != null)
                        {
                            RegisterMigrationSourceAnalysisAddin(keyValuePair.Key, keyValuePair.Value, analysisAddin);
                        }
                        else
                        {
                            // TODO: Raise error
                            TraceManager.TraceWarning(String.Format(MigrationToolkitResources.AnalysisAddinNotFound, analysisAddinName));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Raise error
                    TraceManager.TraceError(String.Format(MigrationToolkitResources.ExceptionLoadingAnalysisAddin, ex.ToString()));
                }

                try
                {
                    List<string> migrationAddinNames = new List<string>();

                    foreach (CustomSetting setting in keyValuePair.Value.CustomSettings.CustomSetting)
                    {
                        if (setting.SettingKey.StartsWith(MigrationToolkitResources.MigrationAddinKeyName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            migrationAddinNames.Add(setting.SettingValue);
                        }
                    }

                    foreach (string migrationAddinName in migrationAddinNames)
                    {
                        MigrationAddin migrationAddin = GetAddin(new Guid(migrationAddinName)) as MigrationAddin;
                        if (migrationAddin != null)
                        {
                            RegisterMigrationSourceMigrationAddin(keyValuePair.Key, keyValuePair.Value, migrationAddin);
                        }
                        else
                        {
                            // TODO: Raise error
                            TraceManager.TraceWarning(String.Format(MigrationToolkitResources.MigrationAddinNotFound, migrationAddinName));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Raise error
                    TraceManager.TraceError(String.Format(MigrationToolkitResources.ExceptionLoadingMigrationAddin, ex.ToString()));
                }
                // END SUPPORT FOR COMPATABILITY WITH OLD CONFIG FILES
            }

        }

        #region IAddinManagementService Members

        /// <summary>
        /// Gets the loaded Add-In by its reference name.
        /// </summary>
        /// <param name="referenceName">The reference name of the Add-In to get</param>
        /// <returns>An Add-in object that has the requested reference name; NULL if the Add-In is unknown.</returns>
        public IAddin GetAddin(Guid referenceName)
        {
            IAddin retVal = null;

            if (m_addIns.ContainsKey(referenceName))
            {
                retVal = m_addIns[referenceName];
            }

            return retVal;
        }

        /// <summary>
        /// Enumerate all of the AnalysisAddins configured for a given migration source
        /// </summary>
        public IEnumerable<AnalysisAddin> GetMigrationSourceAnalysisAddins(Guid migrationSourceId)
        {
            List<AnalysisAddin> analysisAddins;
            if (!m_analysisAddinsByMigrationSource.TryGetValue(migrationSourceId, out analysisAddins))
            {
                analysisAddins = new List<AnalysisAddin>();
            }
            return analysisAddins;
        }

        /// <summary>
        /// Enumerate all of the MigrationAddins configured for a given migration source
        /// </summary>
        public IEnumerable<MigrationAddin> GetMigrationSourceMigrationAddins(Guid migrationSourceId)
        {
            List<MigrationAddin> migrationAddins;
            if (!m_migrationAddinsByMigrationSource.TryGetValue(migrationSourceId, out migrationAddins))
            {
                migrationAddins = new List<MigrationAddin>();
            }
            return migrationAddins;
        }
        #endregion

        #region private methods
        private void RegisterMigrationSourceAnalysisAddin(Guid migrationSourceId, MigrationSource migrationSource, AnalysisAddin analysisAddin)
        {
            if (!m_analysisAddinsByMigrationSource.ContainsKey(migrationSourceId))
            {
                m_analysisAddinsByMigrationSource.Add(migrationSourceId, new List<AnalysisAddin>());
            }
            m_analysisAddinsByMigrationSource[migrationSourceId].Add(analysisAddin);
            TraceManager.TraceInformation(String.Format(MigrationToolkitResources.LoadedAnalysisAddin,
                analysisAddin.GetType().ToString(), migrationSource.FriendlyName));
        }

        private void RegisterMigrationSourceMigrationAddin(Guid migrationSourceId, MigrationSource migrationSource, MigrationAddin migrationAddin)
        {
            if (!m_migrationAddinsByMigrationSource.ContainsKey(migrationSourceId))
            {
                m_migrationAddinsByMigrationSource.Add(migrationSourceId, new List<MigrationAddin>());
            }
            m_migrationAddinsByMigrationSource[migrationSourceId].Add(migrationAddin);
            TraceManager.TraceInformation(String.Format(MigrationToolkitResources.LoadedMigrationAddin,
                migrationAddin.GetType().ToString(), migrationSource.FriendlyName));
        }
        #endregion
    }
}
