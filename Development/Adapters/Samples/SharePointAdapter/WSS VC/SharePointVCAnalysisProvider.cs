//------------------------------------------------------------------------------
// <copyright file="SharePointVCAnalysisProvider.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Net;
    using System.Xml.Linq;
    using System.Linq;
    using Microsoft.TeamFoundation.Integration.SharePointVCAdapter.SharePoint;
    using Microsoft.TeamFoundation.Migration.BusinessModel;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;

    /// <summary>
    /// 
    /// </summary>
    public sealed class SharePointVCAnalysisProvider : AnalysisProviderBase
    {
        private Dictionary<Guid, ChangeActionHandler> supportedChangeActions;
        private Collection<ContentType> supportedContentTypes;
        private ChangeGroupService changeGroupService;
        private ConflictManager conflictManagementService;
        private IServiceContainer analysisServiceContainer;
        private ChangeActionRegistrationService changeActionRegistrationService;
        private HighWaterMark<DateTime> highWaterMarkDelta;
        private HighWaterMark<int> highWaterMarkChangeset;
        private DateTime deltaTableStartTime;
        private ConfigurationService configurationService;

        #region IAnalysisProvider Members

        /// <summary>
        /// Generate the delta table.
        /// </summary>
        public override void GenerateDeltaTable()
        {
            TraceManager.TraceInformation("WSSVC:AP:GenerateDeltaTable");
            highWaterMarkDelta.Reload();
            TraceManager.TraceInformation("\tWSSVC:AP:Initial HighWaterMark {0} ", highWaterMarkDelta.Value);
            deltaTableStartTime = DateTime.Now;
            TraceManager.TraceInformation("\tWSSVC:AP:CutOff {0} ", deltaTableStartTime);
            GetSharePointUpdates();
            highWaterMarkDelta.Update(deltaTableStartTime);
            TraceManager.TraceInformation("\tWSSVC:AP:Updated HighWaterMark {0} ", highWaterMarkDelta.Value);
            changeGroupService.PromoteDeltaToPending();
        }

        /// <summary>
        /// Initialize method of the analysis provider.
        /// Please implement all the heavy-weight initialization logic here, e.g. server connection.
        /// </summary>
        public override void InitializeClient()
        {
            TraceManager.TraceInformation("WSSVC:AP:InitializeClient");
        }

        /// <summary>
        /// Initialize method of the analysis provider - acquire references to the services provided by the platform.
        /// </summary>
        /// <param name="serviceContainer">The service container.</param>
        public override void InitializeServices(IServiceContainer serviceContainer)
        {
            TraceManager.TraceInformation("WSSVC:AP:Initialize");
            this.analysisServiceContainer = serviceContainer;

            supportedContentTypes = new Collection<ContentType>();
            supportedContentTypes.Add(WellKnownContentType.VersionControlledFile);
            supportedContentTypes.Add(WellKnownContentType.VersionControlledFolder);

            SharePointVCChangeActionHandler handler = new SharePointVCChangeActionHandler(this);
            supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>();
            supportedChangeActions.Add(WellKnownChangeActionId.Add, handler.BasicActionHandler);
            supportedChangeActions.Add(WellKnownChangeActionId.Delete, handler.BasicActionHandler);
            supportedChangeActions.Add(WellKnownChangeActionId.Edit, handler.BasicActionHandler);

            configurationService = (ConfigurationService)analysisServiceContainer.GetService(typeof(ConfigurationService));

            highWaterMarkDelta = new HighWaterMark<DateTime>(Constants.HwmDelta);
            highWaterMarkChangeset = new HighWaterMark<int>("LastChangeSet");
            configurationService.RegisterHighWaterMarkWithSession(highWaterMarkDelta);
            configurationService.RegisterHighWaterMarkWithSession(highWaterMarkChangeset);
            changeGroupService = (ChangeGroupService)analysisServiceContainer.GetService(typeof(ChangeGroupService));
            changeGroupService.RegisterDefaultSourceSerializer(new SharePointVCMigrationItemSerializer());
        }

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        /// <param name="conflictManager"></param>
        public override void RegisterConflictTypes(ConflictManager conflictManager)
        {
            TraceManager.TraceInformation("WSSVC:AP:RegisterConflictTypes");
            conflictManagementService = conflictManager;
            conflictManagementService.RegisterConflictType(new GenericConflictType());
        }

        /// <summary>
        /// Registers the supported change actions.
        /// </summary>
        /// <param name="contentActionRegistrationService">The content action registration service.</param>
        public override void RegisterSupportedChangeActions(ChangeActionRegistrationService contentActionRegistrationService)
        {
            TraceManager.TraceInformation("WSSVC:AP:RegisterSupportedChangeActions");
            this.changeActionRegistrationService = contentActionRegistrationService;
            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in supportedChangeActions)
            {
                foreach (ContentType contentType in ((IAnalysisProvider)this).SupportedContentTypes)
                {
                    changeActionRegistrationService.RegisterChangeAction(supportedChangeAction.Key, contentType.ReferenceName, supportedChangeAction.Value);
                }
            }
        }

        /// <summary>
        /// Register adapter's supported content types.
        /// </summary>
        /// <param name="contentTypeRegistrationService"></param>
        public override void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            TraceManager.TraceInformation("WSSVC:AP:RegisterSupportedContentTypes");
        }

        /// <summary>
        /// List of change actions supported by the analysis provider.
        /// </summary>
        /// <value></value>
        public override Dictionary<Guid, Microsoft.TeamFoundation.Migration.Toolkit.Services.ChangeActionHandler> SupportedChangeActions
        {
            get { return supportedChangeActions; }
        }

        /// <summary>
        /// List of content types supported by this provider
        /// </summary>
        /// <value></value>
        public override Collection<ContentType> SupportedContentTypes
        {
            get { return supportedContentTypes; }
        }
        
        #endregion

        # region Helper Methods

        /// <summary>
        /// Gets the updates.
        /// </summary>
        private void GetSharePointUpdates()
        {
            ProcessLog writeLog = new ProcessLog();
            string viewName = string.Empty; //aware it is useless now, for future plans

            using (Lists lists = new Lists())
            {
                lists.Url = string.Format("{0}/_vti_bin/lists.asmx", configurationService.MigrationSource.ServerUrl);
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
                lists.Credentials = credentials;
                string documentLibraryTitle = configurationService.MigrationSource.SourceIdentifier;

                TraceManager.TraceInformation("WSSVC:GetPocUpdates - From {0} in {1} with {2}", documentLibraryTitle, lists.Url, username);
                XElement sharePointDocument;
                try
                {
                    sharePointDocument = XDocument.Parse(lists.GetListItems(documentLibraryTitle, viewName, SharePointHelpers.QueryXmlNode, SharePointHelpers.ViewFieldsXmlNode, "0", SharePointHelpers.QueryOptionsXmlNode, string.Empty).OuterXml).Root;
                }
                catch (System.Web.Services.Protocols.SoapException err)
                {
                    string errrorMessage = string.Format(CultureInfo.CurrentCulture, "WSSVC:SharePoint Exception Occured: {0}", err.Detail.InnerText);
                    TraceManager.TraceInformation(errrorMessage);
                    throw new Exception(errrorMessage, err);
                }

                List<SharePointItem> items = SharePointHelpers.ParseItems(sharePointDocument, credentials);

                foreach (SharePointItem item in items)
                {
                    TraceManager.TraceInformation("\tAnalysing {0} (Modified: {1})(Created: {2})(Version: {3})", item.AbsoluteURL, item.Modified, item.Created, item.Version);
                    bool newFile = (from i in writeLog.LogItems
                                    where (i.EncodedAbsUrl == item.AbsoluteURL) &&
                                    (i.Workspace == configurationService.Workspace)
                                    select i).Count() == 0;

                    if (!newFile)
                    {
                        bool alreadyInTarget = (from i in writeLog.LogItems
                                                where (i.EncodedAbsUrl == item.AbsoluteURL) &&
                                                (i.Version == item.Version) &&
                                                (i.Workspace == configurationService.Workspace)
                                                select i).Count() > 0;
                        if (alreadyInTarget)
                        {
                            TraceManager.TraceInformation("\tFile with this URL and version came from target and has not changed in SharePoint, skipping");
                            continue;
                        }
                    }

                    if (item.Modified.CompareTo(highWaterMarkDelta.Value) > 0 && item.Modified.CompareTo(deltaTableStartTime) < 0) // item has been modified since HWM & before deltra table start time
                    {
                        Guid actionGuid = Guid.Empty;

                        if (newFile)
                        {
                            // item created on the HWM or after it
                            TraceManager.TraceInformation("\tItem flagged as new.");
                            actionGuid = WellKnownChangeActionId.Add;
                        }
                        else
                        {
                            //item created before or equal to the hwm
                            TraceManager.TraceInformation("\tItem flagged as update needed.");
                            actionGuid = WellKnownChangeActionId.Edit;
                        }

                        TraceManager.TraceInformation("\tChangeSet:{0} - {1} ({2})", highWaterMarkChangeset.Value, item.Filename, item.AbsoluteURL);
                        string itemType = item.ItemType.ToWellKnownContentType().ReferenceName;
                        ChangeGroup cg = CreateChangeGroup(highWaterMarkChangeset.Value, 0);
                        cg.CreateAction(actionGuid, item, null, item.AbsoluteURL, item.Version, null, itemType, null);
                        cg.Save();
                        highWaterMarkChangeset.Update(highWaterMarkChangeset.Value + 1);

                    }
                }
            }
        }

        /// <summary>
        /// Creates the change group.
        /// </summary>
        /// <param name="changeset">The changeset.</param>
        /// <param name="executionOrder">The execution order.</param>
        /// <returns></returns>
        private ChangeGroup CreateChangeGroup(int changeset, long executionOrder)
        {
            TraceManager.TraceInformation("WSSVC:Creating Changeset: {0}", changeset);
            ChangeGroup group = changeGroupService.CreateChangeGroupForDeltaTable(changeset.ToString());
            group.Owner = null;
            group.Comment = string.Format("Changeset {0}", changeset);
            group.ChangeTimeUtc = DateTime.UtcNow;
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = executionOrder;
            return group;
        }

        # endregion

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
