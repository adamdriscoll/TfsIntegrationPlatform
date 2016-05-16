//------------------------------------------------------------------------------
// <copyright file="SharePointVCMigrationProvider.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------
namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using Microsoft.TeamFoundation.Migration.BusinessModel;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;
    using System.Xml;

    /// <summary>
    /// This class handles the operations which effect SharePoint, such as add, edit etc...
    /// </summary>
    public sealed class SharePointVCMigrationProvider : MigrationProviderBase
    {
        private IServiceContainer migrationServiceContainer;
        private ChangeGroupService changeGroupService;
        private ConfigurationService configurationService;
        private ConflictManager conflictManagementService;
        private SharePointWriteUtil sharePointWriteUtilities;
        private string sourceIdentifier = string.Empty;

        #region IMigrationProvider Members

        /// <summary>
        /// Initialize method of the migration provider.
        /// Please implement all the heavey-weight initialization logic here, e.g. server connection.
        /// </summary>
        public override void InitializeClient()
        {
            TraceManager.TraceInformation("WSSVC:MP:InitializeClient");
        }

        /// <summary>
        /// Initializes the services.
        /// </summary>
        /// <param name="migrationService">The migration service.</param>
        public override void InitializeServices(IServiceContainer migrationService)
        {
            TraceManager.TraceInformation("WSSVC:MP:InitializevicesSer");
            this.migrationServiceContainer = migrationService;
            changeGroupService = (ChangeGroupService)this.migrationServiceContainer.GetService(typeof(ChangeGroupService));
            Debug.Assert(changeGroupService != null, "Change group service is not initialized");

            configurationService = (ConfigurationService)this.migrationServiceContainer.GetService(typeof(ConfigurationService));
            Debug.Assert(configurationService != null, "Configuration service is not initialized");
            changeGroupService.RegisterDefaultSourceSerializer(new SharePointVCMigrationItemSerializer());

            sourceIdentifier = configurationService.MigrationSource.SourceIdentifier;

            string username = string.Empty;
            string password = string.Empty;
            foreach (CustomSetting customSetting in configurationService.MigrationSource.CustomSettings.CustomSetting)
            {
                switch (customSetting.SettingKey)
                {
                    case "username":
                        {
                            username = customSetting.SettingValue;
                            break;
                        }
                    case "password":
                        {
                            password = customSetting.SettingValue;
                            break;
                        }
                }
            }

            NetworkCredential credentials = new NetworkCredential(username, password);

            sharePointWriteUtilities = new SharePointWriteUtil(configurationService.MigrationSource.ServerUrl, credentials, configurationService.Workspace);
        }

        /// <summary>
        /// Process a changegroup.
        /// </summary>
        /// <param name="changeGroup"></param>
        /// <returns></returns>
        public override ConversionResult ProcessChangeGroup(ChangeGroup changeGroup)
        {
            TraceManager.TraceInformation("WSSVC:MP:ProcessChangeGroup - {0}", changeGroup.Name);
            ProcessLog writeLog = new ProcessLog();

            Guid targetSideSourceId = configurationService.SourceId;
            Guid sourceSideSourceId = configurationService.MigrationPeer;
            ConversionResult convResult = new ConversionResult(sourceSideSourceId, targetSideSourceId);
            try
            {
                foreach (MigrationAction action in changeGroup.Actions)
                {
                    TraceManager.TraceInformation("\t> {0} - {1}", action.Path, action.SourceItem.GetType().ToString());
                    if (action.Action == WellKnownChangeActionId.Add || action.Action == WellKnownChangeActionId.Edit)
                    {
                        if (action.ItemTypeReferenceName == WellKnownContentType.VersionControlledFile.ReferenceName)
                        {
                            string sharePointFileId = string.Empty;
                            string fileName = Path.GetTempFileName();
                            File.Delete(fileName); // the download fails if the file exists
                            try
                            {
                                action.SourceItem.Download(fileName);
                                sharePointFileId = sharePointWriteUtilities.AddFile(action.Path, fileName, sourceIdentifier, writeLog);
                                convResult.ItemConversionHistory.Add(new ItemConversionHistory(changeGroup.Name, string.Empty, sharePointFileId, string.Empty));
                            }
                            finally
                            {
                                // cleanup temp file
                                if (File.Exists(fileName))
                                {
                                    File.Delete(fileName);
                                }
                            }
                        }

                        if (action.ItemTypeReferenceName == WellKnownContentType.VersionControlledFolder.ReferenceName)
                        {
                            string sharePointFileId = sharePointWriteUtilities.CreateFolder(sourceIdentifier, action.Path, writeLog);
                            convResult.ItemConversionHistory.Add(new ItemConversionHistory(changeGroup.Name, string.Empty, sharePointFileId, string.Empty));
                        }

                    }

                    if (action.Action == WellKnownChangeActionId.Delete)
                    {
                        if (action.ItemTypeReferenceName == WellKnownContentType.VersionControlledFile.ReferenceName ||
                            action.ItemTypeReferenceName == WellKnownContentType.VersionControlledFolder.ReferenceName)
                        {
                            sharePointWriteUtilities.Delete(action.Path);                         
                        }
                    }
                }
            }
            finally
            {
                writeLog.Save();
            }

            convResult.ChangeId = changeGroup.ReflectedChangeGroupId.ToString();
            return convResult;
        }

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        /// <param name="conflictManager"></param>
        public override void RegisterConflictTypes(ConflictManager conflictManager)
        {
            TraceManager.TraceInformation("WSSVC:MP:RegisterConflictTypes");
            conflictManagementService = conflictManager;
            conflictManagementService.RegisterConflictType(new GenericConflictType());
        }

        #endregion

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/>.
        /// -or-
        /// null if there is no service object of type <paramref name="serviceType"/>.
        /// </returns>
        public override object GetService(Type serviceType)
        {
            return (IServiceProvider)this;
        }

        #endregion
    }
}
