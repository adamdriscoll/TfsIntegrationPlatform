// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The base interface for all migration items.
    /// </summary>
    public interface IMigrationItem
    {
        /// <summary>
        /// Downloads the item from the source system to the provided path.
        /// </summary>
        /// <param name="localPath">The path to download the item to.</param>
        void Download(string localPath);

        /// <summary>
        /// A display name for the item.  This string is not gaurenteed to useful for parsing
        /// or to represent a meaningful path within the version control system or local file system.
        /// </summary>
        string DisplayName
        {
            get;
        }


    }

}
