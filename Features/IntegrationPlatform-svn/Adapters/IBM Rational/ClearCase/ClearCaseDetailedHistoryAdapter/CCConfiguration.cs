// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// This class contains custom settings defined for ClearCase.
    /// The ClearCase adapter can be configured to use either a snapshot view (which is created
    /// by the adapter) or a dynamic view which must already exist and be accessible via an MVFS
    /// file system on the machine where the migration session is running.
    /// 
    /// To use the snapshot view mode, include CustomSettings such as these for the CC MigrationSource:
    ///   <CustomSetting SettingKey="StorageLocation" SettingValue="\\machine1\ccstg_c_1" />
    ///   <CustomSetting SettingKey="VobName" SettingValue="\my_elements_vob" />
    ///   <!-- The CCBranchName setting is optional; if not specified the main branch is used -->
    ///   <CustomSetting SettingKey="CCBranchName" SettingValue="Branch1" />
    ///   
    /// To use a dynamic view, include CustomSettings such as these for the CC MigrationSource:
    ///   <CustomSetting SettingKey="StorageLocation" SettingValue="\\machine1\ccstg_c_1" />
    ///   <CustomSetting SettingKey="DynamicViewName" SettingValue="integration_view" />
    ///   <!-- The DynamicViewRoot setting is optional, but only if the view is mapped to the default m drive -->
    ///   <CustomSetting SettingKey="DynamicViewRoot" SettingValue="w:\" />
    /// 
    /// </summary>
    public class CCConfiguration
    {
        private const string c_defaultDynamicViewRoot = @"m:\";

        private static Dictionary<string, CCConfiguration> instanceTable;

        public string BranchName { get; private set; }
        public string StorageLocation { get; private set; }
        public string StorageLocationLocalPath { get; private set; }
        public List<string> VobList { get; private set; }
        public bool UsePrecreatedView { get; private set; }
        public string PrecreatedViewName { get; private set; }
        public string DynamicViewRoot { get; private set; }
        public string DownloadFolder { get; private set; }
        public bool DetectChangesInCC { get; private set; }
        public bool LabelAllVersions { get;private set;}
        public bool UseDynamicView { get; private set; }
        public bool BatchChangesInGroup { get; private set; }
        public bool QueryRenameHistory { get; private set; }
        public ClearfsimportConfiguration ClearfsimportConfiguration { get; private set; }


        private CCConfiguration(MigrationSource migrationSource)
        {
            VobList = new List<string>();
            UsePrecreatedView = false;
            DetectChangesInCC = true;
            LabelAllVersions = false;
            QueryRenameHistory = true;
            ClearfsimportConfiguration = new ClearfsimportConfiguration();
            foreach (CustomSetting setting in migrationSource.CustomSettings.CustomSetting)
            {
                if (string.Equals(setting.SettingKey, CCResources.BranchSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    BranchName = setting.SettingValue;
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.StorageLocationSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    StorageLocation = setting.SettingValue;
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.StorageLocationLocalPathSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    StorageLocationLocalPath = setting.SettingValue;
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.VobSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    VobList.Add(setting.SettingValue);
                    continue;
                } 
                if (string.Equals(setting.SettingKey, CCResources.PrecreatedViewSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    PrecreatedViewName = setting.SettingValue;
                    UsePrecreatedView = true;
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.DetectChangesInCC, StringComparison.OrdinalIgnoreCase))
                {
                    DetectChangesInCC = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                } 
                if (string.Equals(setting.SettingKey, CCResources.DynamicViewRootSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    DynamicViewRoot = ValidateAndNormalizeDynamicViewRoot(setting);
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.BatchChangesInGroup, StringComparison.OrdinalIgnoreCase))
                {
                    BatchChangesInGroup = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.DownloadFolderSettingName, StringComparison.OrdinalIgnoreCase))
                {
                    DownloadFolder = setting.SettingValue;
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.ClearfsimportConfigurationUnco, StringComparison.OrdinalIgnoreCase))
                {
                    ClearfsimportConfiguration.Unco = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.ClearfsimportConfigurationMaster, StringComparison.OrdinalIgnoreCase))
                {
                    ClearfsimportConfiguration.Master = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.ClearfsimportConfigurationParseOutput, StringComparison.OrdinalIgnoreCase))
                {
                    ClearfsimportConfiguration.ParseOutput = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.QueryRenameHistory, StringComparison.OrdinalIgnoreCase))
                {
                    QueryRenameHistory = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.ClearfsimportConfigurationBatchSize, StringComparison.OrdinalIgnoreCase))
                {
                    int batchSize;
                    if (int.TryParse(setting.SettingValue, out batchSize))
                    {
                        ClearfsimportConfiguration.BatchSize = batchSize;
                    }
                    else
                    {
                        TraceManager.TraceWarning("The specified {0} of {1} is not a valid batch size. Use the default batch size of 1000", 
                            CCResources.ClearfsimportConfigurationBatchSize,
                            setting.SettingValue);
                    }
                    continue;
                }
                if (string.Equals(setting.SettingKey, CCResources.LabelAllVersions, StringComparison.OrdinalIgnoreCase))
                {
                    LabelAllVersions = string.Equals(setting.SettingValue, "True", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
            }

            verifyCCCustomSettings();
        }

        // Todo raise a conflict instead throw exception
        private void verifyCCCustomSettings()
        {
            if (string.IsNullOrEmpty(DownloadFolder))
            {
                throw new MigrationException(string.Format(CCResources.Culture, CCResources.DownloadLocationNotSpecified));
            }

            if (UsePrecreatedView)
            {
                if (string.IsNullOrEmpty(DynamicViewRoot) && (string.IsNullOrEmpty(StorageLocation)))
                {
                    throw new MigrationException(string.Format(CCResources.Culture, CCResources.ViewRootNeeded));
                }
                else if (!string.IsNullOrEmpty(DynamicViewRoot))
                {
                    if (!string.IsNullOrEmpty(StorageLocation))
                    {
                        throw new MigrationException(string.Format(CCResources.Culture, CCResources.RedundentViewRoot));
                    }
                    else
                    {
                        UseDynamicView = true;
                    }
                }
                else
                {
                    // StorageLocation is non-null and DynamicViewRoot is null
                    if (string.IsNullOrEmpty(StorageLocationLocalPath))
                    {
                        throw new MigrationException(
                            string.Format(CCResources.Culture, CCResources.CustomSettingNotSpecified, "StorageLocationLocalPath", "SnapshotView"));
                    }
                    Utils.VerifyStorageLocationSetting(StorageLocation);
                    UseDynamicView = false;
                }
            }
            else
            {
                // Runtime created snapshot view.
                if (string.IsNullOrEmpty(StorageLocation))
                {
                    throw new MigrationException(
                        string.Format(CCResources.Culture, CCResources.CustomSettingNotSpecified, "StorageLocation", "SnapshotView"));
                }
                if (string.IsNullOrEmpty(StorageLocationLocalPath))
                {
                    throw new MigrationException(
                        string.Format(CCResources.Culture, CCResources.CustomSettingNotSpecified, "StorageLocationLocalPath", "SnapshotView"));
                }
                if (!string.IsNullOrEmpty(DynamicViewRoot))
                {
                    throw new MigrationException(
                        string.Format(CCResources.Culture, CCResources.RedundentViewRoot));
                }
            }
 
            // Log a warning if the branch name is not specified when a snapshot view is used,
            // but not for dynamic views as the view defines the branch (or brances) that make up the view.
            if (!UsePrecreatedView)
            {
                if (string.IsNullOrEmpty(BranchName))
                {
                    TraceManager.TraceWarning(
                        "Branch name is not specified in the configuration file. Using the default branch name {0}",
                        CCResources.DefaultBranchName);
                    BranchName = CCResources.DefaultBranchName;
                }
            }
        }

        /// <summary>
        /// Get an instance of the CCConfiguration object. CCConfiguration object is keyed on migration source id.
        /// </summary>
        /// <param name="configurationService"></param>
        /// <returns></returns>
        public static CCConfiguration GetInstance(MigrationSource migrationSource)   
        {
            if (instanceTable == null)
            {
                instanceTable = new Dictionary<string, CCConfiguration>();
            }
            if (instanceTable.ContainsKey(migrationSource.InternalUniqueId))
            {
                return instanceTable[migrationSource.InternalUniqueId];
            }
            else
            {
                CCConfiguration ccConfiguration = new CCConfiguration(migrationSource);
                instanceTable.Add(migrationSource.InternalUniqueId, ccConfiguration);
                return ccConfiguration;
            }
        }

        public string GetViewName(string snapshotViewSuffix)
        {
            string viewName;
            if (UsePrecreatedView)
            {
                viewName = PrecreatedViewName;
            }
            else
            {
                viewName = string.Format(CultureInfo.InvariantCulture, CCResources.ViewName, Environment.MachineName, snapshotViewSuffix);
            }
            return viewName;
        }

        private static string ValidateAndNormalizeDynamicViewRoot(CustomSetting setting)
        {
            string dynamicViewRoot = setting.SettingValue;
            // dynamicViewRoot should be stored in the form "<driveletter>:\", but we will accept value such as "m" or "n:"
            bool invalidDynamicViewRoot = false;
            if (dynamicViewRoot.Length < 1 || dynamicViewRoot.Length > 3)
            {
                invalidDynamicViewRoot = true;
            }
            else
            {
                char[] chars = dynamicViewRoot.ToCharArray();
                if (!Char.IsLetter(chars[0]))
                {
                    invalidDynamicViewRoot = true;
                }
                else
                {
                    if (chars.Length == 1)
                    {
                        dynamicViewRoot += ":\\";
                    }
                    else if (chars.Length == 2)
                    {
                        if (chars[1] != ':')
                        {
                            invalidDynamicViewRoot = true;
                        }
                        else
                        {
                            dynamicViewRoot += "\\";
                        }
                    }
                    else if (chars[1] != ':' || chars[2] != '\\')
                    {
                        invalidDynamicViewRoot = true;
                    }
                }
            }
            if (invalidDynamicViewRoot)
            {
                throw new MigrationException(String.Format(CultureInfo.InvariantCulture,
                    CCResources.InvalidDynamicViewRoot, setting.SettingKey, setting.SettingValue));
            }
            if (!Directory.Exists(dynamicViewRoot))
            {
                throw new MigrationException(String.Format(CultureInfo.InvariantCulture,
                    CCResources.DynamicViewRootDoesNotExist, setting.SettingKey, setting.SettingValue));
            }
            return dynamicViewRoot;
        }

    }

    public class ClearfsimportConfiguration
    {
        /// <summary>
        /// clearfsimport command argument -unco 
        /// </summary>
        public bool Unco { get; set; }

        /// <summary>
        /// clearfsimport command argument -master
        /// </summary>
        public bool Master { get; set; }

        /// <summary>
        /// Batch size for clearfsimport command.
        /// </summary>
        public int BatchSize { get; set; }

        public bool ParseOutput { get; set; }

        /// <summary>
        /// Default constructor - use the default value for clearfsimport command. 
        /// </summary>
        public ClearfsimportConfiguration()
        {
            Unco = false;
            Master = false;
            BatchSize = 1000;
            ParseOutput = true;
        }
    }

}
