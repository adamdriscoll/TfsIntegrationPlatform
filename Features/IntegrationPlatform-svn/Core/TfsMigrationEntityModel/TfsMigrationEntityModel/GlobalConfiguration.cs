// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// GlobalConfiguration class
    /// </summary>
    public static class GlobalConfiguration
    {
        static readonly int DEFAULT_COMMAND_TIMEOUT_IN_SECONDS = 900;
        static readonly string GlobalConfigFileName = "MigrationToolServers.config";
        static readonly string DefaultConnectionStringSettingName = "TfsMigrationDBConnection";
        static readonly string WorkSpaceRootSettingName = "WorkSpaceRoot";
        static readonly string DefaultWorkSpaceRootFolder = "TfsIPData";
        static readonly string TimeoutValueSettingName = "CommandTimeoutValue";
        static readonly string UseWindowsServiceSettingName = "UseWindowsService";
        static readonly string MaxLogFileSizeInByteSettingName = "MaxLogFileSizeInByte";

        static string s_workSpaceRoot = string.Empty;
        static string s_tfsMigrationDbConnectionString = string.Empty;
        static int? s_cmdTimeoutInSeconds = null;
        static Configuration s_configCache = null;
        static bool? s_useWindowsService = null;
        static long? s_maxLogFileSizeInByte = null;
        const long DefaultMaxLogFileSizeInByte = 100 * 1000 * 1000; // default to approx. 100 MB

        #region TraceManager
        public const string TfsIntegrationPlatformTraceSwitchName = "TfsIntegrationPlatformTraceSwitch";
        public const string TfsIntegrationPlatformTraceSwitchDescription = "Migration toolkit verbosity switch";
        public const string TfsIntegrationPlatformTraceSwitchDefault = "2";
        private static TraceSwitch s_TfsIntegrationPlatformTraceSwitch;

        public const string VCMigrationEngineSwitchName = "VCMigrationEngineTraceSwitch";
        public const string VCMigrationEngineSwitchDescription = "Migration engine verbosity switch";
        public const string VCMigrationEngineSwitchDefault = "2";
        private static TraceSwitch s_VCMigrationEngineSwitchName;
        #endregion

        /// <summary>
        /// Gets the general trace switch used by the platform
        /// </summary>
        public static TraceSwitch TfsIntegrationPlatformTraceSwitch
        {
            get
            {
                if (s_TfsIntegrationPlatformTraceSwitch == null)
                {
                    if (null != Configuration.AppSettings && null != Configuration.AppSettings.Settings[TfsIntegrationPlatformTraceSwitchName])
                    {
                        string traceLevelStr = Configuration.AppSettings.Settings[TfsIntegrationPlatformTraceSwitchName].Value;
                        int traceLevel;
                        if (int.TryParse(traceLevelStr, out traceLevel))
                        {
                            s_TfsIntegrationPlatformTraceSwitch = new TraceSwitch(
                                GlobalConfiguration.TfsIntegrationPlatformTraceSwitchName,
                                GlobalConfiguration.TfsIntegrationPlatformTraceSwitchDescription,
                                traceLevelStr);
                        }
                    }

                    if (s_TfsIntegrationPlatformTraceSwitch == null)
                    {
                        s_TfsIntegrationPlatformTraceSwitch = new TraceSwitch(
                            GlobalConfiguration.TfsIntegrationPlatformTraceSwitchName,
                            GlobalConfiguration.TfsIntegrationPlatformTraceSwitchDescription,
                            GlobalConfiguration.TfsIntegrationPlatformTraceSwitchDefault);
                    }
                }

                return s_TfsIntegrationPlatformTraceSwitch;
            }
        }

        /// <summary>
        /// Gets the trace switch used by the VC migration engine
        /// </summary>
        public static TraceSwitch VCMigrationEngineTraceSwitch
        {
            get
            {
                if (s_VCMigrationEngineSwitchName == null)
                {
                    if (null != Configuration.AppSettings && null != Configuration.AppSettings.Settings[VCMigrationEngineSwitchName])
                    {
                        string traceLevelStr = Configuration.AppSettings.Settings[VCMigrationEngineSwitchName].Value;
                        int traceLevel;
                        if (int.TryParse(traceLevelStr, out traceLevel))
                        {
                            s_VCMigrationEngineSwitchName = new TraceSwitch(
                                GlobalConfiguration.VCMigrationEngineSwitchName,
                                GlobalConfiguration.VCMigrationEngineSwitchDescription,
                                traceLevelStr);
                        }
                    }

                    if (s_VCMigrationEngineSwitchName == null)
                    {
                        s_VCMigrationEngineSwitchName = new TraceSwitch(
                            GlobalConfiguration.VCMigrationEngineSwitchName,
                            GlobalConfiguration.VCMigrationEngineSwitchDescription,
                            GlobalConfiguration.VCMigrationEngineSwitchDefault);
                    }
                }

                return s_VCMigrationEngineSwitchName;
            }
        }

        /// <summary>
        /// Gets the DB connection string from the configuration file.
        /// </summary>
        public static string TfsMigrationDbConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(s_tfsMigrationDbConnectionString))
                {
                    if (null != Configuration.ConnectionStrings.ConnectionStrings[DefaultConnectionStringSettingName])
                    {
                        s_tfsMigrationDbConnectionString = Configuration.ConnectionStrings.ConnectionStrings[DefaultConnectionStringSettingName].ConnectionString;
                    }
                }
                return s_tfsMigrationDbConnectionString;
            }
        }

        /// <summary>
        /// Gets the Work Space root folder path.
        /// </summary>
        public static string WorkSpaceRoot
        {
            get
            {
                if (string.IsNullOrEmpty(s_workSpaceRoot))
                {
                    if (null != Configuration.AppSettings && null != Configuration.AppSettings.Settings[WorkSpaceRootSettingName])
                    {
                        s_workSpaceRoot = Path.GetFullPath(Configuration.AppSettings.Settings[WorkSpaceRootSettingName].Value);
                    }
                    else
                    {
                        string systemDrive = Environment.GetEnvironmentVariable("SYSTEMDRIVE") + Path.DirectorySeparatorChar;
                        s_workSpaceRoot = Path.GetFullPath(Path.Combine(systemDrive, DefaultWorkSpaceRootFolder));
                    }
                }

                if (!Directory.Exists(s_workSpaceRoot))
                {
                    Directory.CreateDirectory(s_workSpaceRoot);
                }

                return s_workSpaceRoot;
            }
        }

        /// <summary>
        /// Gets the fully qualified path to the global configuration file.
        /// </summary>
        public static string GlobalConfigPath
        {
            get
            {
                return Path.Combine(Environment.CurrentDirectory, GlobalConfigFileName);
            }
        }

        /// <summary>
        /// default command time out value in seconds
        /// </summary>
        internal static int CommandTimeOutValue
        {
            get
            {
                if (!s_cmdTimeoutInSeconds.HasValue)
                {
                    if (null != Configuration.AppSettings && null != Configuration.AppSettings.Settings[TimeoutValueSettingName])
                    {
                        try
                        {
                            s_cmdTimeoutInSeconds = int.Parse(Configuration.AppSettings.Settings[TimeoutValueSettingName].Value);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConfigurationException(Resource.ErrorDefaultTimeoutSettingInvalid, ex);
                        }
                    }
                    else
                    {
                        s_cmdTimeoutInSeconds = DEFAULT_COMMAND_TIMEOUT_IN_SECONDS;
                    }
                }

                return s_cmdTimeoutInSeconds.Value;
            }
        }

        public static Configuration Configuration
        {
            get
            {
                if (s_configCache != null)
                {
                    return s_configCache;
                }

                ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();

                string assemblyParentFolder = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                configFile.ExeConfigFilename = Path.Combine(assemblyParentFolder, GlobalConfigPath);

                s_configCache = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);

                if (!s_configCache.HasFile)
                {
                    string errMsg = string.Format(Resource.ErrorMissingGlobalConfigFile, GlobalConfigFileName, Environment.CurrentDirectory);
                    throw new InvalidConfigurationException(errMsg);
                }

                return s_configCache;
            }
        }

        /// <summary>
        /// Gets a setting whether to host the WCF services in Windows Service process or not.
        /// </summary>
        public static bool UseWindowsService
        {
            get
            {
                if (!s_useWindowsService.HasValue)
                {
                    if (null != Configuration.AppSettings && null != Configuration.AppSettings.Settings[UseWindowsServiceSettingName])
                    {
                        try
                        {
                            s_useWindowsService = bool.Parse(Configuration.AppSettings.Settings[UseWindowsServiceSettingName].Value);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConfigurationException(Resource.ErrorUseWindowsServiceSettingInvalid, ex);
                        }
                    }
                    else
                    {
                        s_useWindowsService = false;
                    }
                }

                return s_useWindowsService.Value;
            }
        }

        /// <summary>
        /// Gets a setting of the size limit of the log files (in byte).
        /// </summary>
        public static long MaxLogFileSizeInByte
        {
            get
            {
                if (!s_maxLogFileSizeInByte.HasValue)
                {
                    if (null != Configuration.AppSettings && null != Configuration.AppSettings.Settings[MaxLogFileSizeInByteSettingName])
                    {
                        try
                        {
                            s_maxLogFileSizeInByte = long.Parse(Configuration.AppSettings.Settings[MaxLogFileSizeInByteSettingName].Value);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConfigurationException(Resource.ErrorUseWindowsServiceSettingInvalid, ex);
                        }
                    }
                    else
                    {
                        s_maxLogFileSizeInByte = DefaultMaxLogFileSizeInByte;
                    }
                }

                return s_maxLogFileSizeInByte.Value;
            }
        }
    }
}
