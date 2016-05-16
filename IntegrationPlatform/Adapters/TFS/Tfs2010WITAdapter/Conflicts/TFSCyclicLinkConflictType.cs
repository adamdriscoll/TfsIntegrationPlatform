// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Linking;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class TFSCyclicLinkConflictType : ConflictType
    {
        public const string ConflictDetailsKey_SourceWorkItemID = "SourceWorkItemID";
        public const string ConflictDetailsKey_TargetWorkItemID = "TargetWorkItemID";
        public const string ConflictDetailsKey_LinkType = "LinkType";
        public const string ConflictDetailsKey_LinkClosure = "LinkClosure";

        private static readonly Guid s_conflictTypeReferenceName = new Guid("BF1277E9-A218-4a2d-8C3C-A9501D30ECD5");
        private static readonly string s_conflictTypeFriendlyName = "Circularity in link hierarchy conflict type";
        internal const string CircularityLinkHierarchyViolationMessage = "AddLink: The specified link type enforces noncircularity in its hierarchy.";

        public static MigrationConflict CreateConflict(
            LinkChangeAction conflictedAction,
            Exception linkSubmissionException,
            NonCyclicReferenceClosure linkReferenceClosure)
        {
            string scopeHint = CreateScopeHint(linkSubmissionException, conflictedAction);
            string conflictDetails = CreateConflictDetails(linkSubmissionException, conflictedAction, linkReferenceClosure);
            MigrationConflict conflict = new MigrationConflict(new TFSCyclicLinkConflictType(), MigrationConflict.Status.Unresolved, conflictDetails, scopeHint);
            conflict.ConflictedLinkChangeAction = conflictedAction;

            return conflict;
        }

        private static string CreateScopeHint(
            Exception linkSubmissionException, 
            LinkChangeAction action)
        {
            string sourceItem;
            string targetItem;
            string linkType;
            ParseException(linkSubmissionException, action, out sourceItem, out targetItem, out linkType);

            return string.Format("/{0}/{1}/{2}", linkType, sourceItem, targetItem);
        }

        private static string CreateConflictDetails(
            Exception linkSubmissionException, 
            LinkChangeAction action, 
            NonCyclicReferenceClosure linkReferenceClosure)
        {
            string sourceItem;
            string targetItem;
            string linkType;
            ParseException(linkSubmissionException, action, out sourceItem, out targetItem, out linkType);

            return CyclicLinkConflictDetails.CreateConflictDetails(sourceItem, targetItem, linkType, linkReferenceClosure);
        }

        private static void ParseException(
            Exception linkSubmissionException, 
            LinkChangeAction action, 
            out string sourceItem, 
            out string targetItem, out string linkType)
        {
            /*
            * Example Exception:
            * System.Web.Services.Protocols.SoapException
            * 
            * Example Message
            * AddLink: The specified link type enforces noncircularity in its hierarchy. 
            * The target work item is ancestor of the source work item and cannot be its child: %SourceID="960";%, %TargetID="962";%, %LinkType="2";% 
            * ---> AddLink: The specified link type enforces noncircularity in its hierarchy. The target work item is ancestor of the source work item
            * and cannot be its child: %SourceID="960";%, %TargetID="962";%, %LinkType="2";%
            */
            Debug.Assert(linkSubmissionException is System.Web.Services.Protocols.SoapException,
                "linkSubmissionException is not System.Web.Services.Protocols.SoapException");

            sourceItem = action.Link.SourceArtifactId;
            targetItem = TfsWorkItemHandler.IdFromUri(action.Link.TargetArtifact.Uri);
            linkType = action.Link.LinkType.ReferenceName;
        }

        public TFSCyclicLinkConflictType()
            : base(new TFSCyclicLinkConflictHandler())
        {
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return s_conflictTypeReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return s_conflictTypeFriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_TFSCyclicLinkConflictType";
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new ManualConflictResolutionAction()); // fix target side hierarchy and retry
            AddSupportedResolutionAction(new SkipConflictedActionResolutionAction());
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_SourceWorkItemID);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_TargetWorkItemID);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_LinkType);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_LinkClosure);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);
                CyclicLinkConflictDetails details = new CyclicLinkConflictDetails(properties);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(string.Format(TfsWITAdapterResources.CyclicalLinkError, details.SourceWorkItemID, details.TargetWorkItemID, details.LinkTypeReferenceName));
                if (details.LinkReferenceClosure.Count > 0)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(TfsWITAdapterResources.LinkClosureHeader);
                    stringBuilder.AppendLine("SOURCE\tTARGET");
                    foreach (LinkDescription link in details.LinkReferenceClosure)
                    {
                        stringBuilder.AppendLine(string.Format("{0}\t{1}", link.SourceWorkItemId, link.TargetWorkItemId));
                    }
                }
                return stringBuilder.ToString();
            }
            catch (Exception)
            {
                return dtls;
            }
        }

        public CyclicLinkConflictDetails GetConflictDetails(MigrationConflict conflict)
        {
            if (!conflict.ConflictType.ReferenceName.Equals(this.ReferenceName))
            {
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(conflict.ConflictDetails))
            {
                throw new ArgumentNullException("conflict.ConflictDetails");
            }

            try
            {
                // V2 conflict details, i.e. using property bag
                return new CyclicLinkConflictDetails(conflict.ConflictDetailsProperties);
            }
            catch (Exception)
            {
                GenericSerializer<InvalidWorkItemLinkDetails> serializer =
                        new GenericSerializer<InvalidWorkItemLinkDetails>();
                InvalidWorkItemLinkDetails baseDetails = serializer.Deserialize(conflict.ConflictDetails) as InvalidWorkItemLinkDetails;
                return new CyclicLinkConflictDetails(baseDetails.SourceWorkItemID, baseDetails.TargetWorkItemID,
                    baseDetails.LinkTypeReferenceName, null);
            }
        }
    }

    [Serializable]
    public class LinkDescription
    {
        public string SourceWorkItemId { get; set; }
        public string TargetWorkItemId { get; set; }

        public LinkDescription()
        { }

        public LinkDescription(string sourceItemId, string targetItemId)
        {
            SourceWorkItemId = sourceItemId;
            TargetWorkItemId = targetItemId;
        }
    }

    [Serializable]
    public class CyclicLinkConflictDetails : InvalidWorkItemLinkDetails
    {
        public CyclicLinkConflictDetails()
        { }

        public CyclicLinkConflictDetails(
            string sourceItemId,
            string targetItemId,
            string linkTypeReferenceName,
            NonCyclicReferenceClosure linkReferenceClosure)
            : base(sourceItemId, targetItemId, linkTypeReferenceName)
        {
            LinkReferenceClosure = new List<LinkDescription>();

            if (null != linkReferenceClosure)
            {
                foreach (var link in linkReferenceClosure.Links)
                {
                    LinkReferenceClosure.Add(
                        new LinkDescription(link.SourceArtifactId, TfsWorkItemHandler.IdFromUri(link.TargetArtifact.Uri)));
                }
            }
        }

        public CyclicLinkConflictDetails(ConflictDetailsProperties detailsProperties)
        {
            string sourceItemId, targetItemId, linkType, closure;

            if (detailsProperties.Properties.TryGetValue(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_SourceWorkItemID, out sourceItemId)
                && detailsProperties.Properties.TryGetValue(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_TargetWorkItemID, out targetItemId)
                && detailsProperties.Properties.TryGetValue(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_LinkType, out linkType)
                && detailsProperties.Properties.TryGetValue(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_LinkClosure, out closure))
            {
                this.SourceWorkItemID = sourceItemId;
                this.TargetWorkItemID = targetItemId;
                this.LinkTypeReferenceName = linkType;

                GenericSerializer<List<LinkDescription>> serializer = new GenericSerializer<List<LinkDescription>>();

                if (string.IsNullOrEmpty(closure))
                {
                    this.LinkReferenceClosure = new List<LinkDescription>();
                }
                else
                {
                    this.LinkReferenceClosure = serializer.Deserialize(closure);
                }
            }
            else
            {
                throw new ArgumentException("detailsProperties do not contain all expected values for the conflict type");
            }
        }

        public List<LinkDescription> LinkReferenceClosure
        {
            get;
            set;
        }

        internal static string CreateConflictDetails(
            string sourceItem, 
            string targetItem, 
            string linkType,
            NonCyclicReferenceClosure linkReferenceClosure)
        {
            CyclicLinkConflictDetails dtls =
                new CyclicLinkConflictDetails(sourceItem, targetItem, linkType, linkReferenceClosure);

            return dtls.Properties.ToString();
        }

        [XmlIgnore]
        public ConflictDetailsProperties Properties
        {
            get
            {
                ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();
                detailsProperties.Properties.Add(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_SourceWorkItemID,
                    this.SourceWorkItemID);
                
                detailsProperties.Properties.Add(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_TargetWorkItemID,
                    this.TargetWorkItemID);

                detailsProperties.Properties.Add(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_LinkType,
                    this.LinkTypeReferenceName);    
        
                if (null == this.LinkReferenceClosure)
                {
                    this.LinkReferenceClosure = new List<LinkDescription>();
                }

                GenericSerializer<List<LinkDescription>> serializer = new GenericSerializer<List<LinkDescription>>();
                detailsProperties.Properties.Add(
                    TFSCyclicLinkConflictType.ConflictDetailsKey_LinkClosure,
                    serializer.Serialize(this.LinkReferenceClosure));
        
                return detailsProperties;
            }
        }
    }
}
