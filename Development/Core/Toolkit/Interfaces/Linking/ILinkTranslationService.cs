// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    /// <summary>
    /// Public interface called by an adapter to get information about the link handling
    /// provided by the peer adapter
    /// </summary>
    public interface ILinkTranslationService
    {
        /// <summary>
        /// Gets a reference to the LinkConfigurationLookupService
        /// </summary>
        LinkConfigurationLookupService LinkConfigurationLookupService { get; }

        /// <summary>
        /// Check whether or not the linkType passed in is a link type supported
        /// by its peer adapter
        /// </summary>
        /// <param name="linkTypeReferenceName">The reference name of a LinkType (a Guid string)</param>
        /// <returns>True if and only if the other side supports the specified link type</returns>
        bool LinkTypeSupportedByOtherSide(string linkTypeReferenceName);

    }
}
