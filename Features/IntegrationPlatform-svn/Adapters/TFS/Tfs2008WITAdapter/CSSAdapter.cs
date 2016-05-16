// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    class CSSAdapter
    {
        /***********************************
         * Example of an XML we are parsing
         ***********************************
             <StructureChanges MaxSequence="123" fMore="0">
                <!-- This lists all nodes that has changed between the sequence id we pass in and sequence 123.	-->
                <StructureElements>
                    <StructureElement Id="12" Name="foo" ParentId="..." StructureId="..." ProjectId="..." Deleted="false" ForwardingId="..."/>
                </StructureElements>
                <!-- This lists all projects that have been deleted between the sequence id we pass in and sequence 123.	-->
                <Projects>
                    <Project ProjectId="...project URI..." Deleted="true"/>
                </Projects>
            </StructureChanges>
        */

        ICommonStructureService CSS { get; set; }
        Guid SourceId { get; set; }

        public CSSAdapter(ICommonStructureService css, Guid migrationSourceId)
        {
            if (css == null)
            {
                throw new ArgumentNullException("css");
            }

            CSS = css;
            SourceId = migrationSourceId;
        }

        private static string GetAttributeString(
            XmlElement element,
            string attributeName,           // Name of attribute
            bool required,
            string defaultValue)            // Default value to give if attribute is not found
        {
            var attributeNode = element.GetAttributeNode(attributeName);

            // If the attribute is required but missing throw an exception
            if (required && attributeNode == null)
            {
                throw new InvalidOperationException();
            }

            // If the attribute is NOT required set the output to the default provided
            if (null == attributeNode)
            {
                return defaultValue;
            }
            else
            {
                return attributeNode.Value;
            }
        }

        internal XmlDocument GetTeamProjectSpecificCSSNodeChanges(Project teamProject, int startSeqId, out int maxSeqId)
        {
            bool fMore;

            List<XmlNode> changedNodes = new List<XmlNode>();

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationSource = context.RTMigrationSourceSet.Where(s => s.UniqueId.Equals(SourceId)).FirstOrDefault();
                Debug.Assert(null != migrationSource);

                do
                {
                    fMore = false;

                    string xmlStr = CSS.GetChangedNodes(startSeqId);
                    XmlDocument changeDoc = new XmlDocument();
                    changeDoc.LoadXml(xmlStr);

                    startSeqId = Convert.ToInt32(GetAttributeString(changeDoc.DocumentElement, "MaxSequence" /*$N18N$*/, true, "-1"), CultureInfo.InvariantCulture);
                    fMore = string.Equals("1", GetAttributeString(changeDoc.DocumentElement, "fMore" /*$N18N$*/, true, "0"), StringComparison.OrdinalIgnoreCase);

                    XmlNodeList nodesUnderProject = changeDoc.DocumentElement.SelectNodes(
                        string.Format("/StructureChanges/StructureElements/StructureElement[@ProjectId='{0}']", teamProject.Uri.AbsoluteUri));
                    foreach (XmlNode node in nodesUnderProject)
                    {
                        changedNodes.Add(node);

                        XmlElement changedNodeElem = node as XmlElement;
                        if (null != changedNodeElem)
                        {
                            bool deleted = IsStructureElementDeleted(changedNodeElem);
                            string nodeUri = GetStructureElementId(changedNodeElem);
                            var nodeCache = QueryCachedCSSNode(context, nodeUri);
                            if (!deleted)
                            {
                                NodeInfo nodeInfo = CSS.GetNode(nodeUri);
                                SetStructureElementPath(changedNodeElem, nodeInfo.Path);

                                if (nodeCache == null)
                                {
                                    nodeCache = CreateCachedCSSNode(migrationSource, nodeUri, nodeInfo.Path);
                                }
                                else
                                {
                                    SetStructureElementRenameFromPath(changedNodeElem, nodeCache.ItemData);
                                    nodeCache.ItemData = nodeInfo.Path;
                                }
                            }
                            else
                            {
                                if (nodeCache != null && !string.IsNullOrEmpty(nodeCache.ItemData))
                                {
                                    SetStructureElementPath(changedNodeElem, nodeCache.ItemData);
                                }

                                string forwardingId = GetStructureElementForwardingId(changedNodeElem);
                                try
                                {
                                    if (!string.IsNullOrEmpty(forwardingId))
                                    {
                                        var forwardingNodeInfo = CSS.GetNode(forwardingId);
                                        SetStructureElementForwardingNodePath(changedNodeElem, forwardingNodeInfo.Path);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                } while (fMore);

                context.TrySaveChanges();
            }

            maxSeqId = startSeqId;

            if (changedNodes.Count == 0)
            {
                return null;
            }
            else
            {
                XmlDocument doc = new XmlDocument();

                var structureChangesNode = doc.CreateElement("StructureChanges");
                var structureElementsNode = doc.CreateElement("StructureElements");
                structureChangesNode.AppendChild(structureElementsNode);
                doc.AppendChild(structureChangesNode);

                foreach (XmlNode elementNode in changedNodes)
                {
                    structureElementsNode.AppendChild(doc.ImportNode(elementNode, true));
                }

                return doc;
            }
        }

        private static RTMigrationItem CreateCachedCSSNode(
            RTMigrationSource migrationSource, 
            string nodeUri, 
            string path)
        {
            var nodeCache = RTMigrationItem.CreateRTMigrationItem(0, nodeUri, string.Empty);
            nodeCache.MigrationSource = migrationSource;
            nodeCache.ItemData = path;
            return nodeCache;
        }

        private RTMigrationItem QueryCachedCSSNode(RuntimeEntityModel context, string nodeUri)
        {
            var nodeCache = context.RTMigrationItemSet.Where(
                    i => i.MigrationSource.UniqueId.Equals(SourceId) && i.ItemId == nodeUri).FirstOrDefault();
            return nodeCache;
        }

        private void SetStructureElementForwardingNodePath(XmlElement structureElement, string forwardingNodePath)
        {
            structureElement.SetAttribute("ForwardingNodePath", forwardingNodePath);
        }

        private string GetStructureElementForwardingNodePath(XmlElement structureElement)
        {
            return GetAttributeString(structureElement, "ForwardingNodePath", false, string.Empty);
        }

        private static void SetStructureElementRenameFromPath(XmlElement structureElement, string renameFromPath)
        {
            structureElement.SetAttribute("RenameFromPath", renameFromPath);
        }

        private static string GetStructureElementRenameFromPath(XmlElement structureElement)
        {
            return GetAttributeString(structureElement, "RenameFromPath", false, string.Empty);
        }

        private static string GetStructureElementForwardingId(XmlElement structureElement)
        {
            return GetAttributeString(structureElement, "ForwardingId", false, string.Empty);
        }

        private static void SetStructureElementPath(XmlElement structureElement, string path)
        {
            structureElement.SetAttribute("Path", path);
        }

        private static string GetStructureElementId(XmlElement structureElement)
        {
            return GetAttributeString(structureElement, "Id", true, string.Empty);
        }

        private static string GetStructureElementName(XmlElement structureElement)
        {
            return GetAttributeString(structureElement, "Name", true, string.Empty);
        }

        private static string GetStructureElementPath(XmlElement structureElement)
        {
            return GetAttributeString(structureElement, "Path", false, string.Empty);
        }

        private static bool IsStructureElementDeleted(XmlElement structureElement)
        {
            return Convert.ToBoolean(GetAttributeString(structureElement, "Deleted", true, string.Empty));
        }

        internal void SyncCSSNodeChanges(
            Project teamProject,
            XmlDocument cssNodeChangesDoc,
            ConflictManager conflictManager)
        {
            XmlNodeList elemNodes = cssNodeChangesDoc.DocumentElement.SelectNodes("/StructureChanges/StructureElements/StructureElement");

            if (null == elemNodes || elemNodes.Count == 0)
            {
                return;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationSource = context.RTMigrationSourceSet.Where(s => s.UniqueId.Equals(SourceId)).FirstOrDefault();
                Debug.Assert(null != migrationSource);

                foreach (XmlNode node in elemNodes)
                {
                    XmlElement structElem = node as XmlElement;
                    string nodePath = GetStructureElementPath(structElem);

                    if (IsStructureElementDeleted(structElem))
                    {
                        if (string.IsNullOrEmpty(nodePath))
                        {
                            // don't know which node was deleted on source system
                            continue;
                        }

                        string remappedNodePath = RemapNodePath(nodePath, teamProject.Name);
                        if (remappedNodePath.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
                        {
                            remappedNodePath = remappedNodePath.Substring(0, remappedNodePath.Length - 1);
                        }

                        string forwardingNodePath = GetStructureElementForwardingNodePath(structElem);
                        if (string.IsNullOrEmpty(forwardingNodePath))
                        {
                            // don't know which forwarding node was used on source system
                            continue;
                        }
                        forwardingNodePath = RemapNodePath(forwardingNodePath, teamProject.Name);

                        string forwardingNodeUri = string.Empty;
                        try
                        {
                            forwardingNodeUri = CSS.GetNodeFromPath(forwardingNodePath).Uri;
                        }
                        catch (Exception e)
                        {
                            // Node does not exist
                            if (e.Message.StartsWith("TF200014:", StringComparison.InvariantCultureIgnoreCase))
                            {
                                TraceManager.TraceInformation("Renaming node failed: forwarding node '{0}' no longer exists", forwardingNodePath);
                            }
                            else
                            {
                                TraceManager.TraceException(e);
                            }
                            continue;
                        }

                        Debug.Assert(!string.IsNullOrEmpty(forwardingNodeUri), "forwardingNodeUri is null");

                        try
                        {
                            NodeInfo nodeToDeleteInfo = CSS.GetNodeFromPath(remappedNodePath);
                            TraceManager.TraceInformation("Deleting node: {0}", remappedNodePath);
                            CSS.DeleteBranches(new string[] { nodeToDeleteInfo.Uri }, forwardingNodeUri);
                        }
                        catch (Exception e)
                        {
                            // Node does not exist
                            if (e.Message.StartsWith("TF200014:", StringComparison.InvariantCultureIgnoreCase))
                            {
                                TraceManager.TraceInformation("Deleting node skipped: node '{0}' no longer exists", remappedNodePath);
                            }
                            else
                            {
                                TraceManager.TraceException(e);
                            }
                            continue;
                        }
                    }
                    else
                    {
                        Debug.Assert(!string.IsNullOrEmpty(nodePath), "nodePath is null");
                        string remappedNodePath = RemapNodePath(nodePath, teamProject.Name);
                        if (remappedNodePath.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
                        {
                            remappedNodePath = remappedNodePath.Substring(0, remappedNodePath.Length - 1);
                        }

                        string renamedFromPath = GetStructureElementRenameFromPath(structElem);
                        if (string.IsNullOrEmpty(renamedFromPath))
                        {
                            // add

                            try
                            {
                                NodeInfo nodeInfo = CSS.GetNodeFromPath(remappedNodePath);
                                var cachedNode = QueryCachedCSSNode(context, nodeInfo.Uri);
                                if (cachedNode == null)
                                {
                                    CreateCachedCSSNode(migrationSource, nodeInfo.Uri, nodeInfo.Path);
                                }
                                else
                                {
                                    cachedNode.ItemData = remappedNodePath;
                                }
                                continue;
                            }
                            catch
                            { }

                            TraceManager.TraceInformation("Creating node: {0}", remappedNodePath);

                            // Strip the last \Name off the path to get the parent path
                            string newPathParent = remappedNodePath.Substring(0, remappedNodePath.LastIndexOf('\\'));

                            // Grab the last \Name off the path to get the node name
                            string newPathName = remappedNodePath.Substring(remappedNodePath.LastIndexOf('\\') + 1);

                            // Lookup the parent node on the destination server so that we can get the parentUri
                            NodeInfo parentNode = CSS.GetNodeFromPath(newPathParent);

                            // Create the node
                            string newNodeUri = CSS.CreateNode(newPathName, parentNode.Uri);
                            CreateCachedCSSNode(migrationSource, newNodeUri, remappedNodePath);
                        }
                        else
                        {
                            // rename

                            string newNodeName = GetStructureElementName(structElem);                            
                            string preRenamNodePath = RemapNodePath(renamedFromPath, teamProject.Name);

                            try
                            {
                                TraceManager.TraceInformation("Renaming node: from '{0}' to '{1}'", preRenamNodePath, remappedNodePath);
                                NodeInfo renameNodeInfo = CSS.GetNodeFromPath(preRenamNodePath);
                                CSS.RenameNode(renameNodeInfo.Uri, newNodeName);

                                var cachedNode = QueryCachedCSSNode(context, renameNodeInfo.Uri);
                                if (cachedNode == null)
                                {
                                    CreateCachedCSSNode(migrationSource, renameNodeInfo.Uri, remappedNodePath);
                                }
                                else
                                {
                                    cachedNode.ItemData = remappedNodePath;
                                }
                            }
                            catch (Exception e)
                            {
                                // Node does not exist
                                if (e.Message.StartsWith("TF200014:", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    TraceManager.TraceInformation("Renaming node failed: {0} no longer exists", preRenamNodePath);
                                }
                            }
                        }
                    }
                }

                context.TrySaveChanges();
            }
        }

        protected string RemapNodePath(string pathToMap, string newTeamProjectName)
        {
            return "\\" + newTeamProjectName + pathToMap.Substring(pathToMap.IndexOf('\\', 1));
        }
    }
}
