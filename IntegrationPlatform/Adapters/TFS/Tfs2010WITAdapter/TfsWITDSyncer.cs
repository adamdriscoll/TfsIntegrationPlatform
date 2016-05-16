// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public class TfsWITDSyncer : IWITDSyncer
    {
        private XmlDocument WITD { get; set; }
        private TfsMigrationWorkItemStore MigrationWITStore { get; set; }

        public TfsWITDSyncer(
            XmlDocument witd,
            TfsMigrationWorkItemStore migrationWITStore)
        {
            if (null == witd)
            {
                throw new ArgumentNullException("witd");
            }

            if (null == migrationWITStore)
            {
                throw new ArgumentNullException("migrationWITStore");
            }

            WITD = witd;
            MigrationWITStore = migrationWITStore;
        }

        #region IWITDSyncer Members

        public void DeleteNode(string xpathToRemovingNode)
        {
            if (string.IsNullOrEmpty(xpathToRemovingNode))
            {
                return;
            }

            XmlNodeList nodesToRemove = WITD.SelectNodes(xpathToRemovingNode);
            if (nodesToRemove == null)
            {
                return;
            }

            foreach (XmlNode node in nodesToRemove)
            {
                TraceManager.TraceInformation("Removing the node: {0}", node.OuterXml);
                XmlNode parent = node.ParentNode;
                parent.RemoveChild(node);
            }
        }

        public void InsertNode(string xpathToParentNode, string nodeContent)
        {
            InsertNode(nodeContent, xpathToParentNode, string.Empty);
        }

        public void InsertNode(string xpathToParentNode, string nodeContent, string xpathToCheckDuplicate)
        {
            if (string.IsNullOrEmpty(nodeContent)
                || string.IsNullOrEmpty(xpathToParentNode))
            {
                return;
            }

            if (!string.IsNullOrEmpty(xpathToCheckDuplicate))
            {
                XmlNode customField = WITD.SelectSingleNode(xpathToCheckDuplicate);
                if (null != customField)
                {
                    return;
                }
            }

            XmlNode fieldsNode = WITD.SelectSingleNode(xpathToParentNode);
            Debug.Assert(fieldsNode != null);

            XmlNode fieldNode = ImportNode(WITD, nodeContent);
            fieldsNode.AppendChild(fieldNode);

            TraceManager.TraceInformation("Inserting the node: {0}", fieldNode.OuterXml);
        }

        public void ReplaceNode(string xpathToReplacedNode, string xmlContentOfNewNode)
        {
            if (string.IsNullOrEmpty(xmlContentOfNewNode)
                || string.IsNullOrEmpty(xmlContentOfNewNode))
            {
                return;
            }

            XmlNode oldNode = WITD.SelectSingleNode(xpathToReplacedNode);
            if (null == oldNode)
            {
                return;
            }

            XmlNode parentNode = oldNode.ParentNode;
            parentNode.RemoveChild(oldNode);

            XmlNode newControlNode = ImportNode(WITD, xmlContentOfNewNode);
            parentNode.AppendChild(newControlNode);

            TraceManager.TraceInformation("Replacing the node: {0} with {1}", xpathToReplacedNode, newControlNode.OuterXml);
        }

        public void AddAttribute(string xpathToNode, string newAttribute, string attributeValue)
        {
            if (string.IsNullOrEmpty(xpathToNode)
                || string.IsNullOrEmpty(newAttribute)
                || string.IsNullOrEmpty(attributeValue))
            {
                return;
            }

            XmlNode oldNode = WITD.SelectSingleNode(xpathToNode);
            if (null == oldNode)
            {
                return;
            }

            XmlAttribute attribute = WITD.CreateAttribute(newAttribute);
            attribute.Value = attributeValue;

            oldNode.Attributes.Append(attribute);

            TraceManager.TraceInformation("Adding attribute to the node: Adding {0} = {1} to {2}", newAttribute, attributeValue, xpathToNode);
        }

        public void Sync()
        {
            MigrationWITStore.SyncWorkItemTypes(WITD);
        }

        #endregion

        private XmlNode ImportNode(XmlDocument ownerDocument, string xmlNodeContent)
        {
            XmlDocument tmpDoc = new XmlDocument();
            tmpDoc.LoadXml(xmlNodeContent);

            return ownerDocument.ImportNode(tmpDoc.DocumentElement, true);
        }

    }
}
