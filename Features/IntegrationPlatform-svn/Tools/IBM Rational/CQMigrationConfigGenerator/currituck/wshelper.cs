// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Web.Services;
using System.Web.Services.Protocols;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Proxy;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;


namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    class WSHelper
    {
        #region Static Members
        private static ClientService m_clientService;
        private static string m_teamSystemName = String.Empty;
        #endregion

        #region Private Members
        private WebServiceType m_callType;
        private WSPackage m_pkg;
        private MetadataTableHaveEntry[] m_metadataEntries = null;
        private string m_callerID = String.Empty;
        private IMetadataRowSets m_mdData;
        private Hashtable m_addedFields = new Hashtable(TFStringComparer.WIConverterFieldRefName);
        private Hashtable m_addedDescFields = new Hashtable(TFStringComparer.WIConverterFieldRefName);        
        #endregion

        internal static ClientService ClientService
        {
            get { return m_clientService; }
            set { m_clientService = value; }
        }

        internal enum WebServiceType
        {
            InsertWorkItem,
            UpdateWorkItem
        }

        internal WSHelper(WebServiceType type)
        {
            m_callType = type;
            m_pkg = new WSPackage();

            switch (m_callType)
            {
                case WebServiceType.InsertWorkItem:
                    m_pkg.WorkItemOperations = new WSInsertWorkItem[1];
                    m_pkg.WorkItemOperations[0] = new WSInsertWorkItem();
                    break;

                case WebServiceType.UpdateWorkItem:
                    m_pkg.WorkItemOperations = new WSUpdateWorkItem[1];
                    m_pkg.WorkItemOperations[0] = new WSUpdateWorkItem();
                    break;

                default:
                    Debug.Assert(false, "Invalid Call Type for creating Web Service Helper Object");
                    break;
            }

            //set bypass rules
            m_pkg.WorkItemOperations[0].BypassRules = 1;

            // check whether setting product is required
            m_pkg.Product = ClientService.Url;
        }

        internal void AddColumn(string name, string value)
        {
            AddColumn(name, value, null);
        }

        internal void AddColumn(string name, string value, string type)
        {
            if (m_pkg.WorkItemOperations[0].Columns == null)
            {
                m_pkg.WorkItemOperations[0].Columns = new WSColumns();
                m_pkg.WorkItemOperations[0].Columns.Column = new ArrayList();
            }
            ArrayList columns = m_pkg.WorkItemOperations[0].Columns.Column;
            if (m_addedFields[name] == null)
            {
                // fresh field
                WSColumn col = new WSColumn();
                col.Column1 = name;
                col.Value = value;
                col.Type = type;
                columns.Add(col);

                // add to processed fields
                m_addedFields.Add(name, String.Empty);
            }
            else
            {
                // existing field..  update the existing value
                foreach (WSColumn col in columns)
                {
                    if (TFStringComparer.XmlAttributeValue.Equals(col.Column1, name))
                    {
                        col.Value = value;
                        if (type != null)
                        {
                            col.Type = type;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Add a attachment in web service helper package
        /// </summary>
        /// <param name="file">File Name</param>
        /// <param name="comment">Attachment comment</param>
        /// <param name="isLinkedFile">True if its Hyperlink, False if physical file</param>
        /// <param name="areaNodeUri">Area Node URI for physical file attachment</param>
        internal void AddAttachment(string file, 
                                    string comment, 
                                    bool isLinkedFile, 
                                    string areaNodeUri)
        {
            if (isLinkedFile)
            {
                // add it as link in Currituck
                if (m_pkg.WorkItemOperations[0].InsertResourceLink == null)
                {
                    m_pkg.WorkItemOperations[0].InsertResourceLink = new ArrayList();
                }

                WSInsertResourceLink link = new WSInsertResourceLink();
                link.FieldName = "System.LinkedFiles";
                link.Comment = comment;
                link.Location = file;
                m_pkg.WorkItemOperations[0].InsertResourceLink.Add(link);
            }
            else
            {
                if (m_pkg.WorkItemOperations[0].InsertFile == null)
                {
                    m_pkg.WorkItemOperations[0].InsertFile = new ArrayList();
                }

                // insert attachment
                FileInfo fileInfo = new FileInfo(file);
                Guid guid = Guid.NewGuid();

                WSInsertFile attachItem = new WSInsertFile();
                attachItem.FileName = guid.ToString();
                attachItem.Comment = comment;
                
                try
                {
                    attachItem.CreationDate = fileInfo.CreationTimeUtc;
                }
                catch (ArgumentOutOfRangeException outOfRangeEx)
                {
                    attachItem.CreationDate = HandleCorruptFileTime(file, outOfRangeEx);
                }

                try
                {
                    attachItem.LastWriteDate = fileInfo.LastWriteTimeUtc;
                }
                catch (ArgumentOutOfRangeException outOfRangeEx)
                {
                    attachItem.LastWriteDate = HandleCorruptFileTime(file, outOfRangeEx);
                }
        
                attachItem.FileSize = fileInfo.Length;
                attachItem.OriginalName = fileInfo.Name;
                attachItem.AreaNodeUri = areaNodeUri;
                attachItem.FileGuid = guid;
                attachItem.FileNameWithPath = file;

                UploadFile(attachItem.FileGuid, attachItem.FileNameWithPath, attachItem.AreaNodeUri);
                m_pkg.WorkItemOperations[0].InsertFile.Add(attachItem);
            }
        }

        /// <summary>
        /// Handles Date time out of range exception and returns default date time value in UTC.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="outOfRangeEx"></param>
        private DateTime HandleCorruptFileTime(string fileName, ArgumentOutOfRangeException outOfRangeEx)
        {
            // The .Net minimum time is too low and may cause problems in some C++ programs that deal with UTC times.
            // Setting it to the highest may be problematic too – file creation time being higher than the current time.
            DateTime defaultDate = new DateTime(1970, 1, 1);
            WSUpdateWorkItem wsUpdate = m_pkg.WorkItemOperations[0] as WSUpdateWorkItem;
            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, CommonResource.CorruptFileDateTime,
                wsUpdate.WorkItemID, fileName, defaultDate);
            Logger.WriteException(LogSource.Common, outOfRangeEx);
            return defaultDate.ToUniversalTime();
        }

        internal void UploadFile(Guid guid, string fileName, string areaNodeUri)
        {
            // first Uplodad the attachment using UploadFile web service
            FileAttachment fileAttach = new FileAttachment();
            fileAttach.FileNameGUID = guid;
            fileAttach.LocalFile = File.OpenRead(fileName);
            fileAttach.AreaNodeUri = areaNodeUri;

            try
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Attaching new File : {0}", fileName);
                m_clientService.UploadFile(fileAttach);
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "File {0} Uploading done", fileName);
            }
            catch (SoapException soapEx)
            {
                // attachment failed
                Logger.WriteException(LogSource.WorkItemTracking, soapEx);
                throw new ConverterException(soapEx.Message, soapEx);
            }
            catch (WebException webEx)
            {
                // insert retry logic if required
                Logger.WriteException(LogSource.WorkItemTracking, webEx);
                throw new ConverterException(webEx.Message, webEx);
            }
            catch (Exception ex)
            {
                // insert retry logic if required
                Logger.WriteException(LogSource.WorkItemTracking, ex);
                throw new ConverterException(ex.Message, ex);
            }
            finally
            {
                fileAttach.LocalFile.Close();
            }
        }

        internal void AddLink(int id, string comment)
        {
            if (id <= 0)
            {
                AddDescriptiveField(VSTSConstants.HistoryFieldRefName, String.Concat(VSTSUtil.ConverterComment, comment), true);
                return;
            }

            if (m_pkg.WorkItemOperations[0].CreateRelation == null)
            {
                m_pkg.WorkItemOperations[0].CreateRelation = new ArrayList();
            }

            // insert attachment
            WSCreateRelation linkItem = new WSCreateRelation();
            linkItem.WorkItemID = id;
            linkItem.Comment = comment;
            m_pkg.WorkItemOperations[0].CreateRelation.Add(linkItem);
        }

        internal void AddDescriptiveField(string name, string value, bool isHtml)
        {
            if (m_pkg.WorkItemOperations[0].InsertText == null)
            {
                m_pkg.WorkItemOperations[0].InsertText = new ArrayList();
            }

            if (m_addedDescFields[name] == null)
            {
                WSInsertText descText = new WSInsertText();
                descText.FieldName = name;
                descText.FieldDisplayName = name;
                descText.Value = value;
                m_addedDescFields.Add(name, String.Empty);
                m_pkg.WorkItemOperations[0].InsertText.Add(descText);
            }
            else
            {
                // existing descriptive field..  append to the existing value
                foreach (WSInsertText descFld in m_pkg.WorkItemOperations[0].InsertText)
                {
                    if (TFStringComparer.WorkItemFieldFriendlyName.Equals(descFld.FieldName, name))
                    {
                        if (isHtml)
                        {
                            descFld.Value = String.Concat(descFld.Value, "<BR>", value);
                        }
                        else
                        {
                            descFld.Value = String.Concat(descFld.Value, Environment.NewLine, value);
                        }
                        break;
                    }
                }
            }
        }

        internal int Save()
        {
            // execute the call
            string webLogXmlFile = String.Empty;
            try
            {
                // generate the file name
                do
                {
                    webLogXmlFile = Path.Combine(CommonConstants.TempPath, String.Concat(Environment.TickCount.ToString(), ".xml"));
                }
                while (File.Exists(webLogXmlFile)); // make sure that this file does not exist

                using (TextWriter tw = new StreamWriter(webLogXmlFile, false, Encoding.UTF8))
                {
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Generating Web Log XML file: {0}", webLogXmlFile);
                    XmlSerializer sr = new XmlSerializer(typeof(WSPackage));
                    sr.Serialize(tw, m_pkg);
                }
            }
            catch (SerializationException ex)
            {
                // serializtion failed.. some bug...
                Logger.WriteException(LogSource.WorkItemTracking, ex);
                throw new ConverterException(UtilityMethods.Format(
                    VSTSResource.SerializingWSFailed, ex.Message), ex);
            }

            XmlDocument doc = UtilityMethods.LoadFileAsXmlDocument(webLogXmlFile);

            // dump the web service call in Logger before execution in Verbose mode
            Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, doc.DocumentElement.OuterXml);

            try
            {
                XmlElement el;
                string reqId = ClientService.NewRequestId();

                // start perf counter
                switch (m_callType)
                {
                    case WebServiceType.InsertWorkItem:
                        break;
                    case WebServiceType.UpdateWorkItem:
                        break;
                }

                ClientService.Update(reqId,
                                    doc.DocumentElement,
                                    out el,
                                    m_metadataEntries,
                                    out m_callerID,
                                    out m_mdData);

                // print response for now
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, (el.OuterXml));
                if (m_callType == WebServiceType.InsertWorkItem)
                {
                    // get the new work item id
                    XmlNode result = el.SelectSingleNode(WebServiceType.InsertWorkItem.ToString());
                    string id = result.Attributes["ID"].Value;
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Created new work item [{0}]", id);
                    return int.Parse(id, CultureInfo.InvariantCulture);
                }
                else
                {
                    // get the new work item id
                    XmlNode result = el.SelectSingleNode(WebServiceType.UpdateWorkItem.ToString());
                    string id = result.Attributes["ID"].Value;
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Updated existing work item [{0}]", id);
                    return int.Parse(id, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException(LogSource.WorkItemTracking, ex);
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, doc.DocumentElement.OuterXml);
                throw new ConverterException(UtilityMethods.Format(
                    VSTSResource.WISaveFailed, ex.Message), ex);
            }
            finally
            {
                // delete the web log xml file if exist
                if (File.Exists(webLogXmlFile))
                {
                    Logger.Write(LogSource.WorkItemTracking, TraceLevel.Verbose, "Removing Web Log XML file: {0}", webLogXmlFile);
                    try
                    {
                        File.Delete(webLogXmlFile);
                    }
                    catch (Exception)
                    {
                        // if for some reason, cannot delete file, log and move ahead
                        Logger.Write(LogSource.WorkItemTracking, TraceLevel.Warning, "Could not delete file: {0}", webLogXmlFile);
                    }
                }
                // stop perf counter
                switch (m_callType)
                {
                    case WebServiceType.InsertWorkItem:
                        break;
                    case WebServiceType.UpdateWorkItem:
                        break;
                }
            }
        }

        /// <summary>
        /// Set WorkItem ID and Revision field
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="rev">Revision</param>
        internal void SetWorkItemAndRevision(int id, int rev)
        {
            // only for Update case
            WSUpdateWorkItem wsUpdate = m_pkg.WorkItemOperations[0] as WSUpdateWorkItem;
            Debug.Assert(wsUpdate != null, "Web service helper not initialized for Update call");
            wsUpdate.Revision = rev;
            wsUpdate.WorkItemID = id;
        }

        #region Connection setup
        internal static void SetupProxy(string tfsName)
        {
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(tfsName);
            m_clientService = new ClientService(tfs);
            string wsUrl = string.Empty;
            string configUrl = string.Empty;

            m_teamSystemName = tfsName;

            // Get the middle tier URL from BIS
            GetMiddleTierUrls(tfsName, out wsUrl, out configUrl);
            m_clientService.Url = wsUrl;
            m_clientService.ConfigurationUrl = configUrl;
            m_clientService.AttachmentsUrl = GetRegistrationExtendedAttribute(BisData.AttachmentServerUrl, tfsName);

            if (m_clientService.AttachmentsUrl != null &&
                !TFStringComparer.ServerUrl.StartsWith(m_clientService.AttachmentsUrl, "http"))
            {
                m_clientService.AttachmentsUrl = tfs.Uri + m_clientService.AttachmentsUrl;
            }
        }

        //
        // Gets web service URL for the given TFS server
        //
        private static void GetMiddleTierUrls(
            string teamSystemName,                  // TFS server name
            out string serverUrl,                   // Server URL
            out string configurationSettingsUrl)    // Configuration Settings URL
        {
            Debug.Assert(teamSystemName != null, "TFS server name cannot be null!");
            Debug.Assert(teamSystemName.Length > 0, "TFS server name cannot be empty!");

            //
            // Get registration info from BIS. If there's an connectivity issue (or similar exception) 
            //  with BIS, we shouldn't catch and change the exception, we should let the BIS error 
            //  bubble up since there's not much can do to handle that situation.  Their exception 
            //  should best describe the situation.
            //
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(teamSystemName);

            IRegistration regProxy = (IRegistration) tfs.GetService(typeof(IRegistration));
            RegistrationEntry[] regEntries =
                regProxy.GetRegistrationEntries(BisData.Tool);

            Debug.Assert(regEntries.Length == 1, "Only one registration entry must exist for the tool!");
            RegistrationEntry toolEntry = regEntries[0];

            // Find the right service interface
            ServiceInterface si = null;
            ServiceInterface configurationServiceInterface = null;
            int foundCount = 0;
            for (int i = 0; i < toolEntry.ServiceInterfaces.Length; i++)
            {
                string name = toolEntry.ServiceInterfaces[i].Name;
                if (si == null &&
                    TFStringComparer.ServiceInterface.Equals(name, ServiceInterfaces.WorkItem))
                {
                    si = toolEntry.ServiceInterfaces[i];
                    foundCount++;
                }
                else if (configurationServiceInterface == null &&
                    TFStringComparer.ServiceInterface.Equals(name, BisData.ConfigurationServerUrl))
                {
                    configurationServiceInterface = toolEntry.ServiceInterfaces[i];
                    foundCount++;
                }

                if (foundCount == 2)
                {
                    break;
                }
            }

            //
            // If either of these two situations occur, our middle tier location did not get properly registered
            // and we should call attention to that since the app cannot continue and corrective action should
            // be take by the admin.
            //
            if (si == null)
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, "Could not get service interface for WI");
                throw new ConverterException(VSTSResource.ErrorBisMiddleTierNotRegistered);
            }

            serverUrl = si.Url;
            if (String.IsNullOrEmpty(serverUrl))
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, "Could not get service interface URL for WI");
                throw new ConverterException(VSTSResource.ErrorBisMiddleTierNotRegistered);
            }

            if (configurationServiceInterface == null)
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, "Could not get config service interface for WI");
                throw new ConverterException(VSTSResource.ErrorBisMiddleTierNotRegistered);
            }

            configurationSettingsUrl = configurationServiceInterface.Url;
            if (String.IsNullOrEmpty(configurationSettingsUrl))
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, "Could not get config service interface URL for WI");
                throw new ConverterException(VSTSResource.ErrorBisMiddleTierNotRegistered);
            }
        }


        private static string GetRegistrationExtendedAttribute(string extendedAttributeName, string teamSystemName)
        {
            Debug.Assert(teamSystemName != null, "TFS server name cannot be null!");
            Debug.Assert(teamSystemName.Length > 0, "TFS server name cannot be empty!");

            Debug.Assert(extendedAttributeName != null, "ExtendedAttributeName name cannot be null!");
            Debug.Assert(extendedAttributeName.Length > 0, "ExtendedAttributeName name cannot be empty!");

            string value = null;

            // Copied from estudio\bis\proxy\BisServices.cs
            IRegistration regProxy = (IRegistration)GetProxy(teamSystemName, typeof(IRegistration));
            RegistrationEntry[] regEntries =
                regProxy.GetRegistrationEntries(BisData.Tool);

            Debug.Assert(regEntries.Length == 1, "Only one registration entry must exist for the tool!");
            RegistrationEntry toolEntry = regEntries[0];

            foreach (RegistrationExtendedAttribute attr in toolEntry.RegistrationExtendedAttributes)
            {
                if (0 == RegistrationUtilities.Compare(attr.Name, extendedAttributeName))
                {
                    value = attr.Value;
                    break;
                }
            }

            // The value MUST be found!
            if (value == null)
            {
                Logger.Write(LogSource.WorkItemTracking, TraceLevel.Error, "Could not get tool entry for WI");
                throw new ConverterException(VSTSResource.ErrorBisMiddleTierNotRegistered);
            }

            return value;
        }

        // This method returns proxy interface by the given namespace and proxy type
        internal static object GetProxy(
            string teamSystemName,        // BIS namespace
            Type proxyType)             // Type of proxy to return
        {
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(teamSystemName);
            return tfs.GetService(proxyType);
        }

        private struct BisData
        {
            internal const string Tool = "WorkItemTracking";
            internal const string WorkitemArtifact = "Workitem";
            internal const string NodeArtifact = @"Node";
            internal const string AttachmentServerUrl = "AttachmentServerUrl";
            internal const string TFSServerNameSpace = "VSTF";
            internal const string ConfigurationServerUrl = "configurationsettingsurl";
        }

        internal static void SyncBisCSS(ProjectInfo project)
        {
            ClientService.SyncExternalStructures(ClientService.NewRequestId(), project.Uri);
        }

        internal static string GetAreaRootNodeUri(string projectUri)
        {
            string nodeUri = String.Empty;

            ICommonStructureService cssProxy = (ICommonStructureService)GetProxy(m_teamSystemName, typeof(ICommonStructureService));
            NodeInfo[] nodeInfos = cssProxy.ListStructures(projectUri);            

            if (nodeInfos == null)
            {
                throw new ConverterException(VSTSResource.InvalidStructureNode);                
            }
            
            foreach (NodeInfo nodeInfo in nodeInfos)
            {
                if (TFStringComparer.StructureType.Equals(nodeInfo.StructureType, StructureType.ProjectModelHierarchy))
                {
                    nodeUri = nodeInfo.Uri;
                    break;
                }
            }

            if (String.IsNullOrEmpty(nodeUri))
            {
                throw new ConverterException(VSTSResource.InvalidStructureNode);
            }

            return nodeUri;
        }

        #endregion
    }
}
