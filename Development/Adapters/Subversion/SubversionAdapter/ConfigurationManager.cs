// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    internal class ConfigurationManager
    {
        #region Private Members

        private ConfigurationService m_configurationService;

        private Uri m_serverUri;
        private List<Uri> m_cloakedServerPaths;
        private List<Uri> m_mappedServerPaths;

        private string m_userName;
        private string m_passowrd;
        private int m_cacheSize;

        #endregion

        #region Constructor

        internal ConfigurationManager(ConfigurationService configurationService)
        {
            if (null == configurationService)
            {
                throw new ArgumentNullException("configurationService");
            }

            m_configurationService = configurationService;
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the user name that will be used to connect to the SVN repository
        /// </summary>
        internal string Username
        {
            get
            {
                if(null == m_userName)
                {
                    InitializeCustomSettings();
                }

                return m_userName;
            }
        }

        /// <summary>
        /// Gets the password that will be used to connect to the SVN repository
        /// </summary>
        internal string Password
        {
            get
            {
                if (null == m_passowrd)
                {
                    InitializeCustomSettings();
                }

                return m_passowrd;
            }
        }

        /// <summary>
        /// Gets the amount of records that should be prefeteched
        /// </summary>
        internal int ChangesetCacheSize
        {
            get 
            {
                if (m_cacheSize <= 0)
                {
                    InitializeCustomSettings();
                }

                return m_cacheSize;
            }
        }

        /// <summary>
        /// Returns the normalized server uri that will be used to connect to the svn repository
        /// </summary>
        internal Uri RepositoryUri
        {
            get
            {
                if (null == m_serverUri)
                {
                    m_serverUri = PathUtils.GetNormalizedPath(m_configurationService.ServerUrl);
                }

                return m_serverUri;
            }
        }

        /// <summary>
        /// Returns all fully qualified server pathes that are mapped
        /// </summary>
        internal IEnumerable<Uri> MappedServerPaths
        {
            get
            {
                if (null == m_mappedServerPaths)
                {
                    m_mappedServerPaths = ResolveFullQualifiedPaths(false);
                }

                return m_mappedServerPaths;
            }
        }

        /// <summary>
        /// Retrieves all fully qualified server pathes that are cloaked
        /// </summary>
        internal IEnumerable<Uri> CloakedServerPaths
        {
            get
            {
                if (null == m_cloakedServerPaths)
                {
                    m_cloakedServerPaths = ResolveFullQualifiedPaths(true);
                }

                return m_cloakedServerPaths;
            }
        }

        #endregion

        #region Private Helpers

        private void InitializeCustomSettings()
        {
            m_userName = string.Empty;
            m_passowrd = string.Empty;

            foreach (var setting in m_configurationService.MigrationSource.CustomSettings.CustomSetting)
            {
                if (setting.SettingKey.Equals("username", StringComparison.InvariantCultureIgnoreCase))
                {
                    m_userName = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals("password", StringComparison.InvariantCultureIgnoreCase))
                {
                    m_passowrd = setting.SettingValue;
                }
                else if (setting.SettingKey.Equals("LogRecordPrefetchSize", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!Int32.TryParse(setting.SettingValue, out m_cacheSize))
                    {
                        TraceManager.TraceWarning("Unable to parse the input string for the history prefetch size. Defaulting to 50");
                        m_cacheSize = 50;
                    }

                    if (m_cacheSize <= 0)
                    {
                        TraceManager.TraceWarning("Any prefetch size below 0 is not valid. Defaulting to 50");
                        m_cacheSize = 50;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all cloaked or non cloaked server paths and converts them to fully qualified server paths
        /// </summary>
        /// <param name="cloaked">Determines whether we want to retrieve the cloaked or the mapped paths</param>
        /// <returns>Returns a list with all fully qualified server paths</returns>
        private List<Uri> ResolveFullQualifiedPaths(bool cloaked)
        {
            var paths = m_configurationService.Filters.Where(x => x.Cloak == cloaked).Select(x => x.Path);
            var mappedPaths = new List<Uri>(paths.Count());

            foreach (var path in paths)
            {
                mappedPaths.Add(PathUtils.Combine(RepositoryUri, path));
            }

            return mappedPaths;
        }

        #endregion
    }
}
