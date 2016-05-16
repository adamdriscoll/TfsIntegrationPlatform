//------------------------------------------------------------------------------
// <copyright file="SharePointHelpers.cs" company="Microsoft Corporation">
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
    using System.Globalization;
    using System.Net;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using System.Xml.XPath;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;

    /// <summary>
    /// Helper class for working with SharePoint
    /// </summary>
    internal static class SharePointHelpers
    {
        internal static ContentType ToWellKnownContentType(this SharePointItemType itemType)
        {
            switch (itemType)
            {
                case SharePointItemType.File:
                    {
                        return WellKnownContentType.VersionControlledFile;
                    }
                case SharePointItemType.Directory:
                    {
                        return WellKnownContentType.VersionControlledFolder;
                    }
            }

            throw new Exception("Unknown SharePoint type given");
        }

        /// <summary>
        /// Converts the XElement to an XmlNode
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        internal static XmlNode ToXmlNode(this XNode element)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(element.ToString(SaveOptions.DisableFormatting));
            return doc.FirstChild;
        }

        /// <summary>
        /// Creates a simple node.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private static XmlNode CreateSimpleNode(string name)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(string.Format(CultureInfo.CurrentCulture, "<{0}/>", name));
            return doc.FirstChild;
        }

        /// <summary>
        /// Gets the query options.
        /// </summary>
        /// <value>The query options.</value>
        /// <remarks>http://msdn.microsoft.com/en-us/library/ms438338.aspx</remarks>
        public static XmlNode QueryOptionsXmlNode
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<QueryOptions><ViewAttributes Scope=\"RecursiveAll\" /></QueryOptions>");
                return doc.FirstChild;
            }
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>The query.</value>
        public static XmlNode QueryXmlNode
        {
            get
            {
                return CreateSimpleNode("Query");
            }
        }

        /// <summary>
        /// Renames the file or folder.
        /// </summary>
        /// <param name="viewId">The view id.</param>
        /// <param name="fileId">The file id.</param>
        /// <param name="fileRef">The file ref.</param>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        internal static XElement RenameFileOrFolder(string viewId, string fileId, string fileRef, string newName)
        {
            return new XElement("Batch",
                new XAttribute("OnError", "Continue"),
                new XAttribute("PreCalc", "TRUE"),
                new XAttribute("ListVersion", "0"),
                new XAttribute("ViewName", viewId),
                new XElement("Method",
                    new XAttribute("ID", 1),
                    new XAttribute("Cmd", "Update"),
                    new XElement("Field",
                        new XAttribute("Name", "ID"), fileId),
                    new XElement("Field",
                        new XAttribute("Name", "owshiddenversion"), 1),
                    new XElement("Field",
                        new XAttribute("Name", "FileRef"), fileRef),
                    new XElement("Field",
                        new XAttribute("Name", "BaseName"), newName)));
        }

        /// <summary>
        /// Creates the folder.
        /// </summary>
        /// <param name="viewId">The view id.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        internal static XElement CreateFolder(string viewId, string name)
        {
            return new XElement("Batch",
                new XAttribute("OnError", "Continue"),
                new XAttribute("PreCalc", "TRUE"),
                new XAttribute("ListVersion", "0"),
                new XAttribute("ViewName", viewId),
                new XElement("Method",
                    new XAttribute("ID", 1),
                    new XAttribute("Cmd", "New"),
                    new XElement("Field",
                        new XAttribute("Name", "ID"), "New"),
                    new XElement("Field",
                        new XAttribute("Name", "FSObjType"), 1),
                    new XElement("Field",
                        new XAttribute("Name", "BaseName"), name)));
        }

        /// <summary>
        /// Gets the view fields.
        /// </summary>
        /// <value>The view fields.</value>
        public static XmlNode ViewFieldsXmlNode
        {
            get
            {
                return CreateSimpleNode("ViewFields");
            }
        }

        /// <summary>
        /// Parses the items.
        /// </summary>
        /// <param name="rawListItems">The raw list items.</param>
        /// <param name="Credentials">The credentials.</param>
        /// <returns></returns>
        public static List<SharePointItem> ParseItems(XElement rawListItems, NetworkCredential Credentials)
        {
            XNamespace nameSpace = "#RowsetSchema";
            List<SharePointItem> listItems = new List<SharePointItem>();

            foreach (XElement row in rawListItems.Descendants(nameSpace + "row"))
            {
                try
                {
                    listItems.Add(new SharePointItem()
                    {
                        Id = row.Attribute("ows_ID").Value,
                        Modified = DateTime.ParseExact(row.Attribute("ows_Modified").Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                        Filename = row.Attribute("ows_LinkFilename").Value,
                        AbsoluteURL = row.Attribute("ows_EncodedAbsUrl").Value,
                        Created = DateTime.ParseExact(row.Attribute("ows_Created").Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                        Credentials = Credentials,
                        ItemType = row.Attribute("ows_ContentType").Value.Equals("Document", StringComparison.CurrentCultureIgnoreCase) ? SharePointItemType.File : SharePointItemType.Directory,
                        Version = row.Attribute("ows_owshiddenversion").Value
                    });
                }
                catch (FormatException)
                {
                    TraceManager.TraceInformation("\tOWS Modified: {0}", row.Attribute("ows_Modified").Value);
                    TraceManager.TraceInformation("\tOWS Created: {0}", row.Attribute("ows_Created").Value);
                    throw;
                }
            }

            return listItems;
        }
    }

}
