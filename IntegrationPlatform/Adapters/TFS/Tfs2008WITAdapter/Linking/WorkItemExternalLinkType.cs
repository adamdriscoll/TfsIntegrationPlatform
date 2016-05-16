// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Linking
{
    [Serializable]
    public class WorkItemExternalLinkType : LinkType, ILinkHandler
    {
        private const string REFERENCE_NAME = "Microsoft.TeamFoundation.Migration.TFS.LinkType.WorkItemToExternal";
        //private const string FRIENDLY_NAME = "Team Foundation Server WorkItem-to-External-Artifact link type";
        private static readonly ArtifactType s_sourceArtifactType = new WorkItemArtifactType();
        private static readonly ArtifactType s_targetArtifactType = new ExternalArtifactType();
        private static readonly ExtendedLinkProperties s_extendedProperties = new ExtendedLinkProperties(ExtendedLinkProperties.Topology.Network);

        public WorkItemExternalLinkType(string extendedLinkTypeName)
            : base(REFERENCE_NAME + "." + extendedLinkTypeName.Trim().Replace(" ", string.Empty), 
                   extendedLinkTypeName, s_sourceArtifactType, 
                   s_targetArtifactType, s_extendedProperties)
        {
            if (string.IsNullOrEmpty(extendedLinkTypeName))
            {
                throw new ArgumentNullException("extendedLinkTypeName");
            }
        }

        public WorkItemExternalLinkType()
            //: base(REFERENCE_NAME, FRIENDLY_NAME, s_sourceArtifactType, s_targetArtifactType, s_extendedProperties)
        {}

        public void ExtractLinkChangeActions(TfsMigrationWorkItem source, List<LinkChangeGroup> linkChangeGroups, WorkItemLinkStore store)
        {
            if (null == source)
            {
                throw new ArgumentNullException("source");
            }

            if (null == source.WorkItem)
            {
                throw new ArgumentException("source.WorkItem is null");
            }

            var linkChangeGroup = new LinkChangeGroup(
                source.WorkItem.Id.ToString(CultureInfo.InvariantCulture), LinkChangeGroup.LinkChangeGroupStatus.Created, false);

            foreach (Link l in source.WorkItem.Links)
            {
                ExternalLink el = l as ExternalLink;

                if (el != null && IsMyLink(el))
                {
                    var link = new Toolkit.Linking.ArtifactLink(
                        source.WorkItem.Id.ToString(CultureInfo.InvariantCulture),
                        new Toolkit.Linking.Artifact(source.Uri, s_sourceArtifactType),
                        new Toolkit.Linking.Artifact(LinkingConstants.ExternalArtifactPrefix + el.LinkedArtifactUri, s_targetArtifactType),
                        el.Comment,
                        this);
                    linkChangeGroup.AddChangeAction(new LinkChangeAction(WellKnownChangeActionId.Add, link,
                                                                         LinkChangeAction.LinkChangeActionStatus.Created,
                                                                         false));
                }
            }

            linkChangeGroups.Add(linkChangeGroup);
        }

        public bool UpdateTfs(TfsUpdateDocument updateDoc, LinkChangeAction linkChangeAction)
        {
            if (null == updateDoc)
            {
                throw new ArgumentNullException("updateDoc");
            }

            if (null == linkChangeAction)
            {
                throw new ArgumentNullException("linkChangeAction");
            }

            if (!linkChangeAction.Link.LinkType.ReferenceName.StartsWith(REFERENCE_NAME, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Link type mismatch.");
            }

            string uri = linkChangeAction.Link.TargetArtifact.Uri.Substring(LinkingConstants.ExternalArtifactPrefix.Length);
            string comment = linkChangeAction.Link.Comment;

            if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Add))
            {
                updateDoc.AddExternalLink(FriendlyName, uri, comment);
            }
            else if (linkChangeAction.ChangeActionId.Equals(WellKnownChangeActionId.Delete))
            {
                Debug.Assert(updateDoc.WorkItem != null, "WorkItem is null in updateDoc");
                int? extId = ExtractFileLinkInfoExtId(updateDoc.WorkItem, uri);

                if (extId.HasValue)
                {
                    updateDoc.DeleteExternalLink(extId.Value);
                }
                else
                {
                    TraceManager.TraceInformation("Deleting link {0}-to-{1} failed - cannot find linked target artifact.",
                        linkChangeAction.Link.SourceArtifactId,
                        linkChangeAction.Link.TargetArtifact.Uri);
                    return false;
                }
            }
            else
            {
                throw new MigrationException(TfsWITAdapterResources.ErrorUnsupportedChangeAction);
            }

            return true;
        }

        private int? ExtractFileLinkInfoExtId(WorkItem workItem, string uri)
        {
            foreach (Link l in workItem.Links)
            {
                ExternalLink el = l as ExternalLink;

                if (el != null && IsMyLink(el))
                {
                    if (TFStringComparer.ArtiFactUrl.Equals(el.LinkedArtifactUri, uri))
                    {
                        return ReflectFileLinkInfoExtId(el);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks whether given link points to a generic external artifact.
        /// </summary>
        /// <param name="link">Link</param>
        /// <returns>True if the link points to a generic external artifact</returns>
        internal bool IsMyLink(
            ExternalLink link)
        {
            //return (!WorkItemChangeListLinkType.IsMyLink(link) &&
            //        !WorkItemLatestFileLinkType.IsMyLink(link) &&
            //        !WorkItemRevisionFileLinkType.IsMyLink(link));
            return TFStringComparer.ArtifactType.Equals(link.ArtifactLinkType.Name, FriendlyName);
        }

        internal static int? ReflectFileLinkInfoExtId(ExternalLink el)
        {
            try
            {
                object rawFieldValue = GetField(el, "m_fileInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                if (null == rawFieldValue)
                {
                    return null;
                }
                else
                {
                    object fiExtId = GetField(rawFieldValue, "ExtId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (fiExtId == null)
                    {
                        return null;
                    }
                    else
                    {
                        return (int)fiExtId;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static int? ReflectFileLinkInfoExtId(Hyperlink hl)
        {
            //Type type = hl.GetType();
            //FieldInfo fieldInfo = type.GetField("m_fileInfo", BindingFlags.Instance | BindingFlags.NonPublic);

            try
            {
                object rawFieldValue = GetField(hl, "m_fileInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                if (null == rawFieldValue)
                {
                    return null;
                }
                else
                {
                    object fiExtId = GetField(rawFieldValue, "ExtId", BindingFlags.Instance | BindingFlags.Public);
                    if (fiExtId == null)
                    {
                        return null;
                    }
                    else
                    {
                        return (int)fiExtId;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static object GetField(object o, string fieldName, BindingFlags bindingFlags)
        {
            FieldInfo field = GetFieldInfo(o.GetType(), fieldName, bindingFlags);

            if (field == null)
            {
                return null;
            }

            return field.GetValue(o);
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName, BindingFlags bindingFlags)
        {
            FieldInfo field = type.GetField(fieldName, bindingFlags);
            if (field == null && type.BaseType != null)
            {
                return GetFieldInfo(type.BaseType, fieldName, bindingFlags);
            }

            return field;
        }

        public override LinkChangeAction CreateLinkDeletionAction(string sourceItemUri, string targetArtifactUrl, string linkTypeReferenceName)
        {
            var link = new Toolkit.Linking.ArtifactLink(
                TfsWorkItemHandler.IdFromUri(sourceItemUri),
                new Toolkit.Linking.Artifact(sourceItemUri, s_sourceArtifactType),
                new Toolkit.Linking.Artifact(targetArtifactUrl, s_targetArtifactType),
                string.Empty,
                this);
            return new LinkChangeAction(WellKnownChangeActionId.Delete, link, LinkChangeAction.LinkChangeActionStatus.Created, false);
        }
    }
}