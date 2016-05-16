//------------------------------------------------------------------------------
// <copyright file="SharePointWriteUtil.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using Microsoft.TeamFoundation.Integration.SharePointVCAdapter.SharePointCopyService;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Integration.SharePointVCAdapter.SharePoint;
    using System.Xml.Linq;
    using System.Linq;
    using System.Xml;
    using System.Collections.Generic;
    using System.Web;

    /// <summary>
    /// Defines functions for writing to SharePoint
    /// </summary>
    public class SharePointWriteUtil
    {
        private string ServerUrl;
        private NetworkCredential Credentials;
        private string Workspace;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointWriteUtil"/> class.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <param name="credentials">The credentials.</param>
        public SharePointWriteUtil(string serverUrl, NetworkCredential credentials, string workspace)
        {
            this.ServerUrl = serverUrl;
            this.Credentials = credentials;
            this.Workspace = workspace;
        }

        /// <summary>
        /// Gets or sets the name of the document library.
        /// </summary>
        /// <value>The name of the document library.</value>
        public string DocumentLibraryName { get; set; }

        /// <summary>
        /// Creates the folder.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <returns></returns>
        public string CreateFolder(string list, string folderPath, ProcessLog writeLog)
        {
            TraceManager.TraceInformation("WSSVC:CreateFolder:Adding folder <{0}> to SharePoint.", folderPath);
            Uri folderUri = new Uri(folderPath);
            string folderName = HttpUtility.UrlDecode(folderUri.Segments.Last().Trim());
            TraceManager.TraceInformation("\tWSSVC:CreateFolder:Idenfied folder name: {0}", folderName);
            if (!(string.IsNullOrEmpty(folderPath.Trim()) || string.IsNullOrEmpty(folderName)))
            {
                return UpdateListItems(list, SharePointHelpers.CreateFolder(GetDefaultListView(list), folderName).ToXmlNode(), writeLog);
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the default list view.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public string GetDefaultListView(string list)
        {
            using (Lists listsWebService = new Lists())
            {
                listsWebService.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/lists.asmx", this.ServerUrl);
                listsWebService.Credentials = this.Credentials;
                System.Xml.XmlNode ndListView = listsWebService.GetListAndView(list, string.Empty);
                return ndListView.ChildNodes[1].Attributes["Name"].Value;
            }
        }

        public string GetItemID(string documentLibrary, string encodedAbsUrl)
        {
            TraceManager.TraceInformation("\tGetItemId:{0}:{1}", documentLibrary, encodedAbsUrl);
            using (Lists listsWebService = new Lists())
            {
                listsWebService.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/lists.asmx", this.ServerUrl);
                listsWebService.Credentials = this.Credentials;
                XNamespace rowsetSchemaNameSpace = "#RowsetSchema";

                XDocument sharePointListItems = XDocument.Parse(listsWebService.GetListItems(documentLibrary, string.Empty, SharePointHelpers.QueryXmlNode, SharePointHelpers.ViewFieldsXmlNode, "0", SharePointHelpers.QueryOptionsXmlNode, string.Empty).OuterXml);
                var files = from row in sharePointListItems.Descendants(rowsetSchemaNameSpace + "row")
                            where row.Attribute("ows_EncodedAbsUrl").Value == encodedAbsUrl
                            select row;

                if (files.Count() > 0)
                {
                    return files.First().Attribute("ows_ID").Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Updates the list items.
        /// </summary>
        /// <param name="listName">Name of the list.</param>
        /// <param name="updates">The updates.</param>
        /// <returns></returns>
        public string UpdateListItems(string listName, XmlNode updates, ProcessLog process)
        {
            TraceManager.TraceInformation("WSSVC:UpdateListItems:List {0}", listName);
            string resultId = string.Empty;
            using (Lists listsWebService = new Lists())
            {
                listsWebService.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/lists.asmx", this.ServerUrl);
                listsWebService.Credentials = this.Credentials;

                XDocument resultsNode = XDocument.Parse(listsWebService.UpdateListItems(listName, updates).OuterXml);
                TraceManager.TraceInformation("WSSVC:UpdateListItems:Action completed");

                XNamespace sharePointNamespace = "http://schemas.microsoft.com/sharepoint/soap/";
                XElement result = (from r in resultsNode.Descendants(sharePointNamespace + "Result")
                                   select r).First();

                string errorId = result.Descendants(sharePointNamespace + "ErrorCode").First().Value;
                if (!errorId.Equals("0x00000000"))
                {
                    string errorText = result.Descendants(sharePointNamespace + "ErrorText").First().Value;
                    TraceManager.TraceInformation("WSSVC:UpdateListItems:Error detected {0}:{1}", errorId, errorText);
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, "{0} ({1})", errorText, errorId));
                }

                XNamespace rowsetSchemeXNamespace = "#RowsetSchema";

                XElement row = (from r in resultsNode.Descendants(rowsetSchemeXNamespace + "row")
                                where r.Attribute("ows_ID") != null
                                select r).First();

                resultId = row.Attribute("ows_ID").Value;
                process.Add(new Item() { EncodedAbsUrl = row.Attribute("ows_EncodedAbsUrl").Value, Version = row.Attribute("ows_owshiddenversion").Value, Workspace = this.Workspace });
            }

            TraceManager.TraceInformation("WSSVC:UpdateListItems:Result ID: {0}", resultId);
            return resultId;
        }

        /// <summary>
        /// Adds the file.
        /// </summary>
        /// <param name="targetUrl">The local path.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="documentLibrary">The document library.</param>
        /// <returns>The ID of the file in SharePoint</returns>
        public string AddFile(string targetUrl, string fileName, string documentLibrary, ProcessLog p)
        {
            string itemId = string.Empty;

            TraceManager.TraceInformation("\tWSSVC:AddFile:Adding file <{0}> to SharePoint {1}", fileName, targetUrl);
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("\t\tCannot find file to upload", fileName);
            }

            FieldInformation[] fields = new FieldInformation[] { new FieldInformation() };

            byte[] fileContents = File.ReadAllBytes(fileName);
            string[] destinationUrls = { targetUrl };
            CopyResult[] results;

            using (Copy copyWebService = new Copy())
            {
                copyWebService.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/copy.asmx", this.ServerUrl);
                copyWebService.Credentials = this.Credentials;
                copyWebService.CopyIntoItems(targetUrl, destinationUrls, fields, fileContents, out results);

                foreach (CopyResult result in results)
                {
                    if (result.ErrorCode != CopyErrorCode.Success)
                    {
                        throw new Exception(string.Format("Failed to upload file {0} to SharePoint. Message: {1}", targetUrl, result.ErrorMessage));
                    }

                    using (Lists listsWebService = new Lists())
                    {
                        listsWebService.Url = string.Format(CultureInfo.CurrentCulture, "{0}/_vti_bin/lists.asmx", this.ServerUrl);
                        listsWebService.Credentials = this.Credentials;
                        XNamespace rowsetSchemaNameSpace = "#RowsetSchema";

                        XDocument sharePointListItems = XDocument.Parse(listsWebService.GetListItems(documentLibrary, string.Empty, SharePointHelpers.QueryXmlNode, SharePointHelpers.ViewFieldsXmlNode, "0", SharePointHelpers.QueryOptionsXmlNode, string.Empty).OuterXml);
                        XElement fileElement = (from row in sharePointListItems.Descendants(rowsetSchemaNameSpace + "row")
                                                where (row.Attribute("ows__CopySource") != null) &&
                                                (row.Attribute("ows__CopySource").Value == targetUrl)
                                                select row).First();

                        itemId = fileElement.Attribute("ows_ID").Value;
                        p.Add(new Item() { EncodedAbsUrl = fileElement.Attribute("ows_EncodedAbsUrl").Value, Version = fileElement.Attribute("ows_owshiddenversion").Value, Workspace = this.Workspace });
                    }
                }
            }

            return itemId;
        }

        internal void Delete(string path)
        {
            TraceManager.TraceInformation("\tDeleting: {0}", path);
            WebRequest request = WebRequest.Create(path);
            request.Credentials = this.Credentials;
            request.Method = "DELETE";
            try
            {
                request.GetResponse().Close();
            }
            catch (WebException err)
            {
                TraceManager.TraceInformation("\tException during delete: {0}\n{1}\n{2}", err.Message, err.Status, err.Response.ToString());
            }
        }
    }
}
