// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class UserIdLookupConfiguration
    {
        const UserIdPropertyNameEnum DefaultUserIdPropertyName = UserIdPropertyNameEnum.DisplayName;
        readonly bool m_lookupIsEnabled;
        Dictionary<Guid, Guid[]> m_perMigrationSourceSortedLookupProviderRefName = new Dictionary<Guid,Guid[]>();
        Dictionary<Guid, UserIdPropertyNameEnum> m_perMigratinSourceDefaultUserIdPropertyName = new Dictionary<Guid, UserIdPropertyNameEnum>();

        public UserIdLookupConfiguration(
            bool userIdLookupEnabled,
            MigrationSourcesElement migrationSourcesConfig)
        {
            m_lookupIsEnabled = userIdLookupEnabled;

            // configuration needed for static user id mapping
            foreach (MigrationSource migrSrcConfig in migrationSourcesConfig.MigrationSource)
            {
                Guid migrationSourceId = new Guid(migrSrcConfig.InternalUniqueId);

                UserIdPropertyNameEnum userIdPropName = migrSrcConfig.Settings.DefaultUserIdProperty.UserIdPropertyName;
                m_perMigratinSourceDefaultUserIdPropertyName[migrationSourceId] = userIdPropName;
            }

            if (m_lookupIsEnabled)
            {
                // configuration needed for runtime lookup

                // Dictionary<MigrationSourceId, Dictionary<AddinRefName, List<Addins>>>
                Dictionary<Guid, Dictionary<Guid, LookupAddin>> configuredAddins = new Dictionary<Guid,Dictionary<Guid, LookupAddin>>();

                foreach (MigrationSource migrSrcConfig in migrationSourcesConfig.MigrationSource)
                {
                    Guid migrationSourceId = new Guid(migrSrcConfig.InternalUniqueId);

                    if (!configuredAddins.ContainsKey(migrationSourceId))
                    {
                        configuredAddins.Add(migrationSourceId, new Dictionary<Guid, LookupAddin>());
                    }
                    
                    foreach (LookupAddin addin in migrSrcConfig.Settings.UserIdentityLookup.LookupAddin)
                    {
                        if (configuredAddins[migrationSourceId].ContainsKey(addin.ReferenceNameGuid))
                        {
                            configuredAddins[migrationSourceId].Add(addin.ReferenceNameGuid, addin);
                        }
                    }

                    UserIdPropertyNameEnum userIdPropName = migrSrcConfig.Settings.DefaultUserIdProperty.UserIdPropertyName;
                    m_perMigratinSourceDefaultUserIdPropertyName[migrationSourceId] = userIdPropName;
                }

                foreach (var addins in configuredAddins)
                {
                    LookupAddin[] perMigrSrcAddins = addins.Value.Values.ToArray();
                    Array.Sort(perMigrSrcAddins, new LookupAddinPrecedenceComparer());

                    Guid[] providerRefNames = new Guid[perMigrSrcAddins.Length];
                    for (int i = 0; i < perMigrSrcAddins.Length; ++i)
                    {
                        providerRefNames[i] = perMigrSrcAddins[i].ReferenceNameGuid;
                    }

                    m_perMigrationSourceSortedLookupProviderRefName.Add(addins.Key, providerRefNames);
                }
            }
        }

        public bool LookupIsEnabled
        {
            get { return m_lookupIsEnabled; }
        } 

        /// <summary>
        /// Gets all configured applicable User Id lookup providers' reference names
        /// </summary>
        /// <param name="migrationSourceId"></param>
        /// <param name="configuredProviderRefNames">Array of reference names; empty array if no configuration information is available.</param>
        /// <returns></returns>
        public bool TryGetConfiguredProviders(Guid migrationSourceId, out Guid[] configuredProviderRefNames)
        {
            if (!LookupIsEnabled)
            {
                configuredProviderRefNames = null;
                return false;
            }

            if (m_perMigrationSourceSortedLookupProviderRefName.ContainsKey(migrationSourceId))
            {
                configuredProviderRefNames = m_perMigrationSourceSortedLookupProviderRefName[migrationSourceId];
            }
            else
            {
                // no configuration info is available for the subject migration source
                configuredProviderRefNames = new Guid[0];
            }

            return true;
        }

        /// <summary>
        /// Gets the default user id property name
        /// </summary>
        /// <param name="migrationSourceId"></param>
        /// <param name="defaultUserIdPropertyName"></param>
        /// <returns></returns>
        public bool TryGetDefaultUserIdPropertyName(Guid migrationSourceId, out UserIdPropertyNameEnum defaultUserIdPropertyName)
        {
            if (m_perMigratinSourceDefaultUserIdPropertyName.ContainsKey(migrationSourceId))
            {
                return m_perMigratinSourceDefaultUserIdPropertyName.TryGetValue(migrationSourceId, out defaultUserIdPropertyName);
            }
            else
            {
                defaultUserIdPropertyName = DefaultUserIdPropertyName;
                return true;
            }
        }

        class LookupAddinPrecedenceComparer : IComparer<LookupAddin>
        {

            #region IComparer<LookupAddin> Members

            public int Compare(LookupAddin x, LookupAddin y)
            {
                return x.Precedence.CompareTo(y.Precedence);
            }

            #endregion
        }
    }
}
