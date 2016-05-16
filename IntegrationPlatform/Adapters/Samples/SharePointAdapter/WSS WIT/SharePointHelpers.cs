//------------------------------------------------------------------------------
// <copyright file="SharePointHelpers.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    //todo: (RJM) Cleanup and unify the sharepoint helpers
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// Defines the commands which can be used in SharePoint method updates
    /// </summary>
    internal enum SharePointMethodCommand
    {
        /// <summary>
        /// New or insert an item
        /// </summary>
        New = 0,

        /// <summary>
        /// Update an existing item
        /// </summary>
        Update = 1,

        /// <summary>
        /// Delete an existing item
        /// </summary>
        Delete = 2
    }

    /// <summary>
    /// Helper class for working with SharePoint
    /// </summary>
    internal static class SharePointHelpers
    {
        /// <summary>
        /// Adds the method to batch for SharePoint
        /// </summary>
        /// <param name="batch">The batch.</param>
        /// <param name="command">The command.</param>
        /// <param name="sharePointId">The share point id.</param>
        /// <param name="fields">The fields.</param>
        /// <returns></returns>
        private static void AddMethodToBatch(this XContainer batch, SharePointMethodCommand command, string sharePointId, Dictionary<string, object> fields)
        {
            string Id = "New";
            if (!string.IsNullOrEmpty(sharePointId))
            {
                Id = sharePointId;
            }

            XElement methodElement = new XElement("Method",
                new XAttribute("ID", batch.Descendants().Count() + 1),
                new XAttribute("Cmd", command.ToSharePointString()),
                new XElement("Field",
                    new XAttribute("Name", "ID"),
                    Id));

            if (fields != null)
            {
                if (fields.Count == 0)
                {
                    return;
                }

                foreach (KeyValuePair<string, object> field in fields)
                {
                    methodElement.Add(new XElement("Field",
                        new XAttribute("Name", field.Key),
                        field.Value));
                }
            }

            batch.Add(methodElement);
        }

        /// <summary>
        /// Adds the method XMLNode to an existing batch XmlNode for insertion into SharePoint.
        /// </summary>
        /// <param name="batch">The batch XMLNode.</param>
        /// <param name="fields">The fields to send.</param>          
        internal static void AddInsertMethodToBatch(this XContainer batch, Dictionary<string, object> fields)
        {
            batch.AddMethodToBatch(SharePointMethodCommand.New, null, fields);
        }

        /// <summary>
        /// Adds the method XMLNode to an existing batch XmlNode for updating an item in SharePoint.
        /// </summary>
        /// <param name="batch">The batch XMLNode.</param>
        /// <param name="sharePointId">The SharePoint id for this item.</param>
        /// <param name="fields">The fields to send.</param>
        internal static void AddUpdateMethodToBatch(this XContainer batch, string sharePointId, Dictionary<string, object> fields)
        {
            batch.AddMethodToBatch(SharePointMethodCommand.Update, sharePointId, fields);
        }

        /// <summary>
        /// Adds the method XMLNode to an existing batch XmlNode for deleting an item.
        /// </summary>
        /// <param name="batch">The batch XMLNode.</param>
        /// <param name="sharePointId">The SharePoint id for this item.</param>
        internal static void AddDeleteMethodToBatch(this XContainer batch, string sharePointId)
        {
            batch.AddMethodToBatch(SharePointMethodCommand.Delete, sharePointId, null);
        }

        /// <summary>
        /// Converts the XElement to an IXPathNavigable
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        internal static IXPathNavigable ToXPathNavigable(this XNode element)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(element.ToString(SaveOptions.DisableFormatting));
            return doc.FirstChild;
        }

        /// <summary>
        /// Converts the SharePointMethodCommand into a valid string for use in SharePoint
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        internal static string ToSharePointString(this SharePointMethodCommand command)
        {
            string commandText = string.Empty;
            switch (command)
            {
                case SharePointMethodCommand.New:
                    {
                        commandText = "New";
                        break;
                    }
                case SharePointMethodCommand.Update:
                    {
                        commandText = "Update";
                        break;
                    }
                case SharePointMethodCommand.Delete:
                    {
                        commandText = "Delete";
                        break;
                    }
            }

            return commandText;
        }

        /// <summary>
        /// Creates the batch element.
        /// </summary>
        /// <param name="viewId">The view id.</param>
        /// <returns>A batch element</returns>
        internal static XElement CreateBatch(string viewId)
        {
            return new XElement("Batch",
                new XAttribute("OnError", "Continue"),
                new XAttribute("ListVersion", 1),
                new XAttribute("ViewName", viewId));
        }
       
        /// <summary>
        /// Gets the query options.
        /// </summary>
        /// <value>The query options.</value>
        internal static IXPathNavigable QueryOptions
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<QueryOptions/>");
                return doc.FirstChild;
            }
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>The query.</value>
        internal static IXPathNavigable Query
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<Query/>");
                return doc.FirstChild;
            }
        }

        /// <summary>
        /// Gets the view fields.
        /// </summary>
        /// <value>The view fields.</value>
        internal static IXPathNavigable ViewFields
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<ViewFields/>");
                return doc.FirstChild;
            }
        }

        /// <summary>
        /// Gets the row attribute value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns></returns>
        internal static string GetRowAttributeValue(this XElement row, string attributeName)
        {
            string result = string.Empty;
            if (row.Attribute(attributeName) != null)
            {
                result = row.Attribute(attributeName).Value;
            }

            return result;
        }

        /// <summary>
        /// Parses the items.
        /// </summary>
        /// <param name="rawListItems">The raw list items.</param>
        /// <returns>A parsed list of tasks</returns>
        internal static Collection<SharePointListItem> ParseItems(XContainer rawListItems)
        {
            if (rawListItems == null)
            {
                throw new ArgumentNullException("rawListItems");
            }

            XNamespace nameSpace = "#RowsetSchema";
            Collection<SharePointListItem> listItems = new Collection<SharePointListItem>();

            foreach (XElement row in rawListItems.Descendants(nameSpace + "row"))
            {
                SharePointListItem task = new SharePointListItem()
                {
                    DisplayName = row.GetRowAttributeValue("ows_Title"),
                    AuthorId = row.GetRowAttributeValue("ows_Author"),
                };

                if ((row.Attribute("ows_ID") != null && (string.IsNullOrEmpty(row.Attribute("ows_ID").Value) == false)))
                {
                    task.Id = row.Attribute("ows_ID").Value;
                    TraceManager.TraceInformation("\t{0}", task.Id);
                }

                TraceManager.TraceInformation("\tDebugging information:{0}", row.Attribute("ows_Created").Value);
                TraceManager.TraceInformation("\t{0}", row.Attribute("ows_Modified").Value);

                if (string.IsNullOrEmpty(row.Attribute("ows_Modified").Value) == false)
                {
                    DateTime value;
                    if (DateTime.TryParseExact(row.Attribute("ows_Modified").Value, "yyyy-MM-dd hh:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out value))
                    {
                        task.ModifiedOn = value;
                    }
                }

                TraceManager.TraceInformation("\t{0}", task.ModifiedOn);

                foreach (XAttribute attribute in row.Attributes())
                {
                    if (attribute.Name.Equals("ows_ID") ||
                        attribute.Name.Equals("ows_Title") ||
                        attribute.Name.Equals("ows_Modified") ||
                        attribute.Name.Equals("ows_Author"))
                    {
                        // already parsed so we don't need to do it again
                        continue;
                    }

                    string name = attribute.Name.LocalName.Remove(0, 4); // remove ows_
                    name = name.Replace("_", string.Empty).Replace("x0020", string.Empty);
                    task.Columns.Add(name, attribute.Value);
                }

                listItems.Add(task);
            }

            return listItems;
        }
    }

    /// <summary>
    /// Encapsulates the properties of a list within SharePoint
    /// </summary>
    internal class SharePointList
    {
        private XElement batch;

        /// <summary>
        /// Gets or sets the list id.
        /// </summary>
        /// <value>The list id.</value>
        public string ListId { get; set; }

        /// <summary>
        /// Gets or sets the view id.
        /// </summary>
        /// <value>The view id.</value>
        public string ViewId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the batch description for insert, update or deletion of list items.
        /// </summary>
        /// <value>The batch.</value>
        public XElement Batch
        {
            get
            {
                if (batch == null)
                {
                    batch = SharePointHelpers.CreateBatch(this.ViewId);
                }

                return batch;
            }
        }
    }

    /// <summary>
    /// Defines a sharepoint task as a migration item
    /// </summary>
    public class SharePointListItem : IMigrationItem
    {
        /// <summary>
        /// The default constructor for a SharePoint list item.
        /// </summary>
        public SharePointListItem()
        {
            this.Columns = new SimpleDictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the SharePoint item id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the date/time the item was last modified on.
        /// </summary>
        /// <value>The modified on.</value>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>The author id.</value>
        public string AuthorId { get; set; }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        public SimpleDictionary<string, object> Columns { get; set; }

        #region IMigrationItem Members

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Downloads the specified local path.
        /// </summary>
        /// <param name="localPath">The local path.</param>
        public void Download(string localPath)
        {
            TraceManager.TraceInformation("WSSWIT:MI:Download - {0}", localPath);
        }

        #endregion
    }
}
