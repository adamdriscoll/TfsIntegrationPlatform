// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This structure identifies a specific version of a specific item in a migration source
    /// The interpretation of the two string properties "ItemId" and "Version" are adadter dependent.
    /// </summary>
    public struct MigrationItemId
    {
        /// <summary>
        /// A string that uniquely identifies an item to be migrated on the server
        /// </summary>
        public string ItemId
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies a version of the item identified by the ItemId string.
        /// This may be null or empty if the version of the item can be either explicitly or implicitly determined by the ItemId string.
        /// </summary>
        public string ItemVersion
        {
            get;
            set;
        }
    }
}
