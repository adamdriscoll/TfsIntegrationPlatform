// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SysConfig = System.Configuration;

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{
    /// <summary>
    /// This class provides easy access to common settings in the app.config
    /// </summary>
    internal static class AppConfig
    {
        static readonly string MaxLogFileSizeInByteSettingName = "MaxLogFileSizeInByte";
        const long DefaultMaxLogFileSizeInByte = 100 * 1000 * 1000; // default to approx. 100 MB

        static SysConfig.Configuration s_appConfig;
        static long? s_maxLogFileSizeInByte = null;

        private static SysConfig.Configuration Configuration
        {
            get
            {
                if (s_appConfig == null)
                {
                    s_appConfig = SysConfig.ConfigurationManager.OpenExeConfiguration(SysConfig.ConfigurationUserLevel.None);
                }
                return s_appConfig;
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
                            throw new InvalidOperationException(ex.ToString());
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
