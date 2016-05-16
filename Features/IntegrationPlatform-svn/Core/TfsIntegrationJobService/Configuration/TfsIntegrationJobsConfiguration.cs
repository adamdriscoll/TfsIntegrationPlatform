// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    /// <summary>
    /// This class provides easy access to the settings in TfsIntegrationJobService.config
    /// </summary>
    public static class TfsIntegrationJobsConfiguration
    {
        private static Configuration s_configCache = null;
        private static Dictionary<Guid, Job> s_configuredJobs = null;

        public const string StartJobSettingName = "StartJob";
        public const string ConfigFileName = "TfsIntegrationJobService.config";

        /// <summary>
        /// Gets a Dictionary of paired Reference Name and Configuration of each configured job
        /// </summary>
        public static Dictionary<Guid, Job> ConfiguredJobs
        {
            get
            {
                if (null == s_configuredJobs)
                {
                    s_configuredJobs = new Dictionary<Guid,Job>();

                    if (Configuration != null && Configuration.Jobs != null && Configuration.Jobs.Job != null)
                    {
                        foreach (Job job in Configuration.Jobs.Job)
                        {
                            try
                            {
                                Guid jobRefName = new Guid(job.ReferenceName);
                                s_configuredJobs[jobRefName] = job;
                            }
                            catch (Exception e)
                            {
                                TraceManager.TraceError(e.ToString());
                            }
                        }
                    }
                }
                return s_configuredJobs;
            }
        }

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        private static string ConfigFilePath
        {
            get
            {
                return Path.Combine(Environment.CurrentDirectory, ConfigFileName);
            }
        }

        /// <summary>
        /// Gets the deserialized object model of TfsIntegrationJobService.config
        /// </summary>
        private static Configuration Configuration
        {
            get
            {
                if (s_configCache == null)
                {
                    if (!File.Exists(ConfigFilePath))
                    {
                        throw new InvalidOperationException(
                            string.Format(Resource.ErrorMissingConfigFile, ConfigFilePath));
                    }

                    string configContent = File.ReadAllText(ConfigFilePath);
                    s_configCache = Deserialize(configContent, typeof(Configuration)) as Configuration;
                    
                    if (s_configCache == null)
                    {
                        throw new InvalidOperationException(
                            string.Format(Resource.ErrorInvalidConfigFile, ConfigFilePath));
                    }
                }

                return s_configCache;
            }
        }

        private static object Deserialize(string blob, Type objectType)
        {
            object retval;

            if (string.IsNullOrEmpty(blob))
            {
                retval = null;
            }
            else
            {
                var serializer = new XmlSerializer(objectType);
                using (var strReader = new StringReader(blob))
                using (var xmlReader = XmlReader.Create(strReader))
                {
                    retval = serializer.Deserialize(xmlReader);
                }
            }
            return retval;
        }
    }
}
