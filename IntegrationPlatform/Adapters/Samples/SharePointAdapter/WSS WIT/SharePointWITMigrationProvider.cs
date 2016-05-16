//------------------------------------------------------------------------------
// <copyright file="SharePointWITMigrationProvider.cs" company="Microsoft Corporation">
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
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using Microsoft.TeamFoundation.Migration.BusinessModel;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;

    /// <summary>
    /// This is the migration provider for the WSS WIT Adapter.
    /// </summary>
    public class SharePointWITMigrationProvider : MigrationProviderBase
    {
        private ConflictManager conflictManager;
        private ConfigurationService configurationService;

        #region IMigrationProvider Members

        /// <summary>
        /// Initializes the client.
        /// </summary>
        public override void InitializeClient()
        {
            TraceManager.TraceInformation("WSSWIT:MP:InitializeClient");
        }

        /// <summary>
        /// Initializes the services.
        /// </summary>
        /// <param name="analysisServiceContainer">The analysis service container.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "analysisServiceContainer")]
        public override void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            TraceManager.TraceInformation("WSSWIT:MP:InitializeServices");
            if (analysisServiceContainer == null)
            {
                throw new ArgumentNullException("analysisServiceContainer");
            }

            this.configurationService = (ConfigurationService)analysisServiceContainer.GetService(typeof(ConfigurationService));
            ChangeGroupService changeGroupService = (ChangeGroupService)analysisServiceContainer.GetService(typeof(ChangeGroupService));
            changeGroupService.RegisterDefaultSourceSerializer(new SharePointWITMigrationItemSerializer());
        }

        /// <summary>
        /// Processes the change group.
        /// </summary>
        /// <param name="changeGroup">The change group.</param>
        /// <returns></returns>
        public override ConversionResult ProcessChangeGroup(ChangeGroup changeGroup)
        {
            TraceManager.TraceInformation("WSSWIT:MP:ProcessChangeGroup");

            ConversionResult conversionResult = new ConversionResult(configurationService.MigrationPeer, configurationService.SourceId);

            using (SharePoint.Lists sharePointList = new SharePoint.Lists())
            {
                SharePointList list = new SharePointList();
                sharePointList.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/lists.asmx", configurationService.ServerUrl);
                list.Name = configurationService.MigrationSource.SourceIdentifier;
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
                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Updating/Inserting items to the list {0} on {1} with username {2}", list.Name, sharePointList.Url, username);

                XmlNode listViewNode = sharePointList.GetListAndView(list.Name, string.Empty);
                list.ListId = listViewNode.ChildNodes[0].Attributes["Name"].Value;
                list.ViewId = listViewNode.ChildNodes[1].Attributes["Name"].Value;

                foreach (MigrationAction action in changeGroup.Actions)
                {
                    System.Xml.Linq.XElement batchNode = list.Batch;
                    string sourceSystemId = changeGroup.Name;

                    Dictionary<string, object> fieldList = BuildFieldList(action);
                    string taskId = GetSharePointID(action);                    

                    switch (action.Action.ToString().ToUpperInvariant())
                    {
                        case "CB71D043-BEDE-4092-AA87-CF0F14586625": //ADD
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Building method to add WIT to SharePoint");
                                batchNode.AddInsertMethodToBatch(fieldList);
                                break;
                            }
                        case "E876681D-8FF1-4342-A0A1-DB91513116B5": //Edit
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Update WIT in SharePoint");
                                if (!string.IsNullOrEmpty(taskId))
                                {
                                    batchNode.AddUpdateMethodToBatch(taskId, fieldList);
                                }
                                else
                                {
                                    TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Problem with SharePoint ID - skipping update");
                                }
                                break;
                            }
                        case "DBF96ACF-871E-43aa-83E4-534BCC14D71F": // Add Attachment
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Add Attachment in SharePoint");
                                //batchNode.AddAttachement("id",file);
                                //todo: (RJM) Add attachments
                                break;
                            }
                        case "7FEB5531-4A7D-46c6-81EF-AF2B7CB997C8": // Delete Attachment
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Delete Attachment in SharePoint");
                                //todo: (RJM) Delete attachments
                                break;
                            }
                        case "45213A63-DE99-4eab-A255-1B477C8C52C9": // Delete 
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Delete WIT in SharePoint");
                                if (!string.IsNullOrEmpty(taskId))
                                {
                                    batchNode.AddDeleteMethodToBatch(taskId);
                                }
                                else
                                {
                                    TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Problem with SharePoint ID - skipping delete");
                                }
                                break;
                            }
                        default:
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Recieved action which we do not cater for: {0}", action.Action);
                                break;
                            }
                    }

                    if (batchNode.DescendantNodes().Count() > 0)
                    {
                        TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Inserting and updating SharePoint list items to {0}", list.ListId);
                        XmlNode resultsNode;
                        try
                        {
                            resultsNode = sharePointList.UpdateListItems(list.ListId, (XmlNode)batchNode.ToXPathNavigable());
                        }
                        catch (System.Web.Services.Protocols.SoapException err)
                        {
                            TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:SOAP Exception from SharePoint. Details {0}", err.Detail.InnerText);
                            TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Batch: {0}", batchNode);
                            throw;
                        }
                        if (action.Action == WellKnownChangeActionId.Add || action.Action == WellKnownChangeActionId.Edit)
                        {
                            XmlNamespaceManager rowsetNameSpace = new XmlNamespaceManager(resultsNode.OwnerDocument.NameTable);
                            rowsetNameSpace.AddNamespace("z", "#RowsetSchema");

                            XmlNode rowNode = resultsNode.SelectSingleNode("//z:row", rowsetNameSpace);
                            if ((rowNode != null) && (rowNode.Attributes["ows_ID"] != null))
                            {
                                string newSharePointId = rowNode.Attributes["ows_ID"].Value;
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:SharePoint ID is {0}", newSharePointId);
                                conversionResult.ItemConversionHistory.Add(new ItemConversionHistory(sourceSystemId, string.Empty, newSharePointId.ToString(), string.Empty));
                            }
                            else
                            {
                                TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Unabled to get SharePoint ID");
                            }
                        }

                        XmlNamespaceManager sharePointNameSpace = new XmlNamespaceManager(resultsNode.OwnerDocument.NameTable);
                        sharePointNameSpace.AddNamespace("s", resultsNode.NamespaceURI);
                        string errorCode = resultsNode.SelectSingleNode("//s:ErrorCode", sharePointNameSpace).InnerText;
                        if (errorCode != "0x00000000")
                        {
                            string errorMessage = resultsNode.SelectSingleNode("//s:ErrorText", sharePointNameSpace).InnerText;
                            throw new Exception(string.Format(CultureInfo.CurrentCulture, "Error inserting/updating SharePoint. Message: {1}{0}" +
                                "Code: {2}{0}" +
                                "Batch XML: {3}", Environment.NewLine, errorMessage, errorCode, batchNode));
                        }
                    }
                    else
                    {
                        TraceManager.TraceInformation("\tWSSWIT:MP:ProcessChangeGroup:Nothing to do to SharePoint");
                    }
                }
            }

            conversionResult.ChangeId = changeGroup.ReflectedChangeGroupId.Value.ToString(CultureInfo.CurrentCulture);
            return conversionResult;
        }

        /// <summary>
        /// Gets the SharePoint ID from the action description.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The ID</returns>
        private string GetSharePointID(IMigrationAction action)
        {
            TraceManager.TraceInformation("WSSWIT:MP:GetSharePointID");
            XmlNode workItemChangesNode = action.MigrationActionDescription.SelectSingleNode("/WorkItemChanges");
            string value = string.Empty;

            if (workItemChangesNode.Attributes["TargetWorkItemID"] == null)
            {
                TraceManager.TraceInformation("WSSWIT:MP:GetSharePointID:Cannot find work item id. XML is: {0}", workItemChangesNode.OuterXml);
            }
            else
            {
                value = workItemChangesNode.Attributes["TargetWorkItemID"].Value;
                TraceManager.TraceInformation("WSSWIT:MP:GetSharePointID:Value {0}", value);
            }

            return value;
        }

        /// <summary>
        /// Builds the field list from an action using the global variable fieldmap 
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        private static Dictionary<string, object> BuildFieldList(IMigrationAction action)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();

            XmlNodeList columns = action.MigrationActionDescription.SelectNodes("/WorkItemChanges/Columns/Column");

            foreach (XmlNode columnData in columns)
            {
                string fieldValue = columnData.FirstChild.InnerText;
                string fieldName = columnData.Attributes["ReferenceName"].Value;

                if (string.IsNullOrEmpty(fieldName) == false)
                {
                    fields.Add(fieldName, fieldValue);
                }
            }

            return fields;
        }

        /// <summary>
        /// Registers the conflict types.
        /// </summary>
        /// <param name="conflictManager">The conflict manager.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "conflictManager")]
        public override void RegisterConflictTypes(ConflictManager conflictManager)
        {
            TraceManager.TraceInformation("WSSWIT:MP:RegisterConflictTypes");
            this.conflictManager = conflictManager;
            this.conflictManager.RegisterConflictType(new GenericConflictType());
            this.conflictManager.RegisterConflictType(new SharePointWITGeneralConflictType(), SyncOrchestrator.ConflictsSyncOrchOptions.Continue);
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
        public override object GetService(System.Type serviceType)
        {
            TraceManager.TraceInformation("WSSWIT:MP:GetService");
            if (serviceType == typeof(IMigrationProvider))
            {
                return this;
            }

            return null;
        }

        #endregion
    }
}
