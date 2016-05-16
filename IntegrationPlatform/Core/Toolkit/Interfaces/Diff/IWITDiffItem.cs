// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The interface for WIT diff items.
    /// </summary>
    public interface IWITDiffItem
    {
        /// <summary>
        /// A string that uniquely identifies the work itme on the work item server (in the format specific to the adapter)
        /// </summary>
        string WorkItemId { get; }

        /// <summary>
        /// An XML document that fully describes a work item
        /// May be null in the case where the contents of work items do not need to be compared
        /// </summary>
        XmlDocument WorkItemDetails { get; }

        /// <summary>
        /// Returns the list of attachments for this item
        /// </summary>
        ReadOnlyCollection<IMigrationFileAttachment> Attachments { get; }

        /// <summary>
        /// Returns the list of URIs for links from this item
        /// </summary>
        ReadOnlyCollection<string> LinkUris { get; }

        /// <summary>
        /// Returns the number of links for this item
        /// </summary>
        int LinkCount { get; }

        /// <summary>
        /// Returns true if the corresponding work item has been modified since the specified time
        /// </summary>
        /// <param name="aUtcDateTime"></param>
        /// <returns></returns>
        bool HasBeenModifiedSince(DateTime aUtcDateTime);

        /// <summary>
        /// Returns true if the corresponding work item has been modified more recently than
        /// the work item identified by the MigrationItemId specified
        /// </summary>
        /// <param name="migrationItemId"></param>
        /// <returns></returns>
        bool HasBeenModifiedMoreRecentlyThan(MigrationItemId migrationItemId);

        /// <summary>
        /// Returns the latest version string for the work item represented by the IWITDiffItem
        /// </summary>
        string LatestVersion { get; }

        /// <summary>
        /// Returns the date changed for the last version of the work item represented by the IWITDiffItem
        /// that was changed by a user (not the Integration Platform).   
        /// Returns DateTime.MinValue if the work item has only been modified by the Integration Platform.
        /// </summary>
        DateTime ChangedByUserDate { get; }

        /// <summary>
        /// Returns true if the first version of this work item was created by a user rather than the Integration Platform
        /// </summary>
        bool CreatedByUser { get; }
    }

}
