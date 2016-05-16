// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public interface ILinkProvider
    {
        /// <summary>
        /// List of change actions supported by this provider.
        /// </summary>
        ICollection<Guid> SupportedChangeActions
        {
            get; 
        }

        /// <summary>
        /// List of change actions supported by the other side. 
        /// </summary>
        ICollection<Guid> SupportedChangeActionsOther
        { 
            get;
            set;
        }

        /// <summary>
        /// List of link types supported by this provider.
        /// </summary>
        Dictionary<string, LinkType> SupportedLinkTypes
        { 
            get;
        }

        /// <summary>
        /// List of reference names of the link typs supported by the other side.
        /// </summary>
        ICollection<string> SupportedLinkTypeReferenceNamesOther
        {
            get; 
            set;
        }

        /// <summary>
        /// Initialize this provider.
        /// </summary>
        /// <param name="serviceContainer"></param>
        void Initialize(ServiceContainer serviceContainer);

        /// <summary>
        /// Generates a sized collection of linking delta information
        /// </summary>
        /// <param name="linkService"></param>
        /// <param name="maxDeltaSliceSize"></param>
        /// <returns></returns>
        ReadOnlyCollection<LinkChangeGroup> GenerateNextLinkDeltaSlice(LinkService linkService, int maxDeltaSliceSize);

        /// <summary>
        /// Submits a collection of links together
        /// </summary>
        /// <param name="linkChanges"></param>
        void SubmitLinkChange(LinkService linkService);

        /// <summary>
        /// Try getting a typed artifact from its Id
        /// </summary>
        /// <param name="artifactTypeReferenceName"></param>
        /// <param name="id"></param>
        /// <param name="artifact"></param>
        /// <returns></returns>
        bool TryGetArtifactById(string artifactTypeReferenceName, string id, out IArtifact artifact);

        /// <summary>
        /// Register adapter's supported link operations.
        /// </summary>
        void RegisterSupportedLinkOperations();

        /// <summary>
        /// Register adapter's supported link types.
        /// </summary>
        void RegisterSupportedLinkTypes();

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        void RegisterConflictTypes(ConflictManager conflictManager, Guid sourceId);

        /// <summary>
        /// Perform adapter specific link conflict analysis.
        /// </summary>
        /// <param name="linkService"></param>
        /// <param name="sessionId"></param>
        /// <param name="sourceId"></param>
        void Analyze(LinkService linkService, Guid sessionId, Guid sourceId);

        /// <summary>
        /// Tries to extract the tool-specific id from an artifact.
        /// </summary>
        /// <param name="artifact"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        bool TryExtractArtifactId(IArtifact artifact, out string id);

        /// <summary>
        /// Tries to retrieve the path and version information of a version controlled artifact.
        /// </summary>
        /// <param name="artifact"></param>
        /// <param name="path"></param>
        /// <param name="changeId"></param>
        /// <returns></returns>
        bool TryGetVersionControlledArtifactDetails(IArtifact artifact, out string path, out string changeId);

        /// <summary>
        /// Gets the Uri of a version controlled artifact from its path and change id.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="changeId"></param>
        /// <returns></returns>
        string GetVersionControlledArtifactUri(string path, string changeId);

        /// <summary>
        /// Gets the typed links of an artifact
        /// </summary>
        /// <param name="sourceArtifact"></param>
        /// <param name="linkType"></param>
        /// <returns>A collection of typed links of the artifact</returns>
        List<ILink> GetLinks(IArtifact sourceArtifact, LinkType linkType);

        /// <summary>
        /// Creates a closure of the typed links, in which the artifact appears
        /// </summary>
        /// <param name="linkType"></param>
        /// <param name="artifact"></param>
        /// <returns></returns>
        NonCyclicReferenceClosure CreateNonCyclicLinkReferenceClosure(LinkType linkType, IArtifact artifact);

        /// <summary>
        /// Gets the parents of an artifact in a specific typed link relationship
        /// </summary>
        /// <param name="linkType"></param>
        /// <param name="artifact"></param>
        /// <param name="parentArtifacts"></param>
        /// <returns></returns>
        bool TryGetSingleParentLinkSourceArtifacts(LinkType linkType, IArtifact artifact, out IArtifact[] parentArtifacts);
    }
}