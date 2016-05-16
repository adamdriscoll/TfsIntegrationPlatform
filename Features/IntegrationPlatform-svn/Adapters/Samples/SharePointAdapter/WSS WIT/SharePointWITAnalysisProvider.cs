//------------------------------------------------------------------------------
// <copyright file="SharePointWITAnalysisProvider.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.TeamFoundation.Migration.BusinessModel;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;

    /// <summary>
    /// This is the analysis provider for the WSS WIT Adapter.
    /// </summary>
    public class SharePointWITAnalysisProvider : AnalysisProviderBase
    {
        private ConflictManager conflictManagerService;
        private ChangeGroupService changeGroupService;
        private IServiceContainer analysisServiceContainer;
        private Collection<ContentType> supportedContentTypes;
        private Dictionary<Guid, ChangeActionHandler> supportedChangeActions;
        private HighWaterMark<DateTime> highWaterMarkDelta;
        private HighWaterMark<int> highWaterMarkChangeSet;
        private ConfigurationService configurationService;

        #region IAnalysisProvider Members

        /// <summary>
        /// Creates the change group.
        /// </summary>
        /// <param name="changeset">The changeset.</param>
        /// <param name="executionOrder">The execution order.</param>
        /// <returns></returns>
        private ChangeGroup CreateChangeGroup(int changeset, long executionOrder)
        {
            ChangeGroup group = changeGroupService.CreateChangeGroupForDeltaTable(changeset.ToString(CultureInfo.CurrentCulture));
            group.Owner = null;
            group.Comment = string.Format(CultureInfo.CurrentCulture, "Changeset {0}", changeset);
            group.ChangeTimeUtc = DateTime.UtcNow;
            group.Status = ChangeStatus.Delta;
            group.ExecutionOrder = executionOrder;
            return group;
        }

        /// <summary>
        /// Gets the share point task updates.
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        private void GetSharePointTaskUpdates(string viewName)
        {
            TraceManager.TraceInformation("WSSWIT:AP:GetSharePointTaskUpdates");

            using (SharePoint.Lists sharePointList = new SharePoint.Lists())
            {
                sharePointList.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/lists.asmx", configurationService.ServerUrl);
                string listName = configurationService.MigrationSource.SourceIdentifier;
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
                sharePointList.Credentials = new System.Net.NetworkCredential(username, password);
                TraceManager.TraceInformation("\tWSSWIT:AP:Getting items from the list {0} on {1} with username {2}", listName, sharePointList.Url, username);
                XElement rawListItems = XDocument.Parse(sharePointList.GetListItems(listName, viewName, (XmlNode)SharePointHelpers.Query, (XmlNode)SharePointHelpers.ViewFields, "0", (XmlNode)SharePointHelpers.QueryOptions, string.Empty).OuterXml).Root;
                Collection<SharePointListItem> listItems = SharePointHelpers.ParseItems(rawListItems);
                foreach (SharePointListItem task in listItems)
                {
                    Guid actionGuid = WellKnownChangeActionId.Add;
                    if (highWaterMarkDelta.Value != DateTime.MinValue && task.ModifiedOn.CompareTo(highWaterMarkDelta.Value) > 0)
                    {
                        actionGuid = WellKnownChangeActionId.Edit;
                    }
                    else
                    {
                        actionGuid = WellKnownChangeActionId.Add;
                    }

                    ChangeGroup changeGroup = CreateChangeGroup(highWaterMarkChangeSet.Value, 0);
                    changeGroup.CreateAction(actionGuid, task, string.Empty, listName, string.Empty, string.Empty,
                        WellKnownContentType.WorkItem.ReferenceName, CreateFieldRevisionDescriptionDoc(task));
                    changeGroup.Save();
                    highWaterMarkChangeSet.Update(highWaterMarkChangeSet.Value + 1);
                }
            }
        }

        /// <summary>
        /// Creates the field revision description doc.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        private static XmlDocument CreateFieldRevisionDescriptionDoc(SharePointListItem task)
        {
            XElement columns = new XElement("Columns",
                    new XElement("Column",
                        new XAttribute("DisplayName", "Author"),
                        new XAttribute("ReferenceName", "Author"),
                        new XAttribute("Type", "String"),
                        new XElement("Value", task.AuthorId)),
                    new XElement("Column",
                        new XAttribute("DisplayName", "DisplayName"),
                        new XAttribute("ReferenceName", "DisplayName"),
                        new XAttribute("Type", "String"),
                        new XElement("Value", task.DisplayName)),
                    new XElement("Column",
                        new XAttribute("DisplayName", "Id"),
                        new XAttribute("ReferenceName", "Id"),
                        new XAttribute("Type", "String"),
                        new XElement("Value", task.Id.ToString())));

            foreach (KeyValuePair<string, object> column in task.Columns)
            {
                columns.Add(new XElement("Column",
                    new XAttribute("DisplayName", column.Key),
                        new XAttribute("ReferenceName", column.Key),
                        new XAttribute("Type", "String"),
                        new XElement("Value", column.Value)));
            }

            XElement descriptionDoc = new XElement("WorkItemChanges",
                new XAttribute("Revision", "0"),
                new XAttribute("WorkItemType", "SharePointItem"),
                new XAttribute("Author", task.AuthorId),
                new XAttribute("ChangeDate", task.ModifiedOn.ToString(CultureInfo.CurrentCulture)),
                new XAttribute("WorkItemID", task.Id.ToString()),
                columns);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(descriptionDoc.ToString());
            return doc;
        }

        /// <summary>
        /// Generates the delta table.
        /// </summary>
        public override void GenerateDeltaTable()
        {
            string viewName = this.configurationService.Filters[0].Path;
            TraceManager.TraceInformation("WSSWIT:AP:GenerateDeltaTable:View - {0}", viewName);
            highWaterMarkDelta.Reload();
            GetSharePointTaskUpdates(viewName);
            highWaterMarkDelta.Update(DateTime.Now);
            changeGroupService.PromoteDeltaToPending();
        }

        /// <summary>
        /// Initializes the client.
        /// </summary>
        public override void InitializeClient()
        {
            TraceManager.TraceInformation("WSSWIT:AP:InitializeClient");
        }

        /// <summary>
        /// Initializes the services.
        /// </summary>
        /// <param name="analysisService">The analysis service.</param>
        /// <exception cref="ArgumentNullException">If the analysisServiceContainer parameter is null</exception>
        public override void InitializeServices(IServiceContainer analysisService)
        {
            TraceManager.TraceInformation("WSSWIT:AP:InitializeServices");
            if (analysisService == null)
            {
                throw new ArgumentNullException("analysisService");
            }

            this.analysisServiceContainer = analysisService;
            this.configurationService = (ConfigurationService)analysisService.GetService(typeof(ConfigurationService));
            MigrationSource migrationSourceConfiguration = configurationService.MigrationSource;
            SharePointMigrationDataSource dataSourceConfig = InitializeMigrationDataSource();
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

            dataSourceConfig.Credentials = new System.Net.NetworkCredential(username, password);
            dataSourceConfig.Url = migrationSourceConfiguration.ServerUrl;
            dataSourceConfig.ListName = migrationSourceConfiguration.SourceIdentifier;

            this.supportedContentTypes = new Collection<ContentType>();
            this.supportedContentTypes.Add(WellKnownContentType.WorkItem);

            SharePointChangeActionHandlers handler = new SharePointChangeActionHandlers(this);
            this.supportedChangeActions = new Dictionary<Guid, ChangeActionHandler>();
            this.supportedChangeActions.Add(WellKnownChangeActionId.Add, handler.BasicActionHandler);
            this.supportedChangeActions.Add(WellKnownChangeActionId.Edit, handler.BasicActionHandler); 
            this.supportedChangeActions.Add(WellKnownChangeActionId.Delete, handler.BasicActionHandler);

            this.highWaterMarkDelta = new HighWaterMark<DateTime>(Constants.HwmDelta);
            this.highWaterMarkChangeSet = new HighWaterMark<int>("LastChangeSet");
            this.configurationService.RegisterHighWaterMarkWithSession(this.highWaterMarkDelta);
            this.configurationService.RegisterHighWaterMarkWithSession(this.highWaterMarkChangeSet);

            this.changeGroupService = (ChangeGroupService)analysisServiceContainer.GetService(typeof(ChangeGroupService));
            this.changeGroupService.RegisterDefaultSourceSerializer(new SharePointWITMigrationItemSerializer());
        }

        /// <summary>
        /// Initializes the migration data source.
        /// </summary>
        /// <returns></returns>
        private static SharePointMigrationDataSource InitializeMigrationDataSource()
        {
            return new SharePointMigrationDataSource();
        }

        /// <summary>
        /// Registers the conflict types.
        /// </summary>
        /// <param name="conflictManager">The conflict manager.</param>
        public override void RegisterConflictTypes(ConflictManager conflictManager)
        {
            TraceManager.TraceInformation("WSSWIT:AP:RegisterConflictTypes");
            this.conflictManagerService = (ConflictManager)analysisServiceContainer.GetService(typeof(ConflictManager));
            this.conflictManagerService.RegisterConflictType(new GenericConflictType());
            this.conflictManagerService.RegisterConflictType(new SharePointWITGeneralConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
        }

        /// <summary>
        /// Registers the supported change actions.
        /// </summary>
        /// <param name="changeActionRegistrationService">The change action registration service.</param>
        public override void RegisterSupportedChangeActions(ChangeActionRegistrationService changeActionRegistrationService)
        {
            TraceManager.TraceInformation("WSSWIT:AP:RegisterSupportedChangeActions");
            changeActionRegistrationService = (ChangeActionRegistrationService)this.analysisServiceContainer.GetService(typeof(ChangeActionRegistrationService));
            foreach (KeyValuePair<Guid, ChangeActionHandler> supportedChangeAction in this.supportedChangeActions)
            {
                foreach (ContentType contentType in ((IAnalysisProvider)this).SupportedContentTypes)
                {
                    changeActionRegistrationService.RegisterChangeAction(supportedChangeAction.Key, contentType.ReferenceName, supportedChangeAction.Value);
                }
            }
        }

        /// <summary>
        /// Registers the supported content types.
        /// </summary>
        /// <param name="contentTypeRegistrationService">The content type registration service.</param>
        public override void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService)
        {
            TraceManager.TraceInformation("WSSWIT:AP:RegisterSupportedContentTypes");
        }

        /// <summary>
        /// Gets the supported change actions.
        /// </summary>
        /// <value>The supported change actions.</value>
        public override Dictionary<Guid, ChangeActionHandler> SupportedChangeActions
        {
            get
            {
                return this.supportedChangeActions;
            }
        }

        /// <summary>
        /// Gets the supported content types.
        /// </summary>
        /// <value>The supported content types.</value>
        public override Collection<ContentType> SupportedContentTypes
        {
            get
            {
                return this.supportedContentTypes;
            }
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
            TraceManager.TraceInformation("WSSWIT:AP:GetService");
            if (serviceType == typeof(IAnalysisProvider))
            {
                return this;
            }

            return null;
        }

        #endregion
    }
}
