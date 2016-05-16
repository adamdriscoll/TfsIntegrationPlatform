// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The interface for all diff items.
    /// </summary>
    public interface IVCDiffItem
    {
        /// <summary>
        /// ServerPath is the path to the file or folder in the native form understood by the server's adapter (not the canonical form)
        /// </summary>
        string ServerPath
        {
            get;
            set;
        }

        /// <summary>
        /// An MD5 hash value for the file
        /// </summary>
        byte[] HashValue
        {
            get;
            set;
        }

        /// <summary>
        /// Identifies the type of version control item (file or folder)
        /// </summary>
        VCItemType VCItemType
        {
            get;
            set;
        }

        /// <summary>
        /// Returns true if and only if the item identified by the implementation of IDiffItem is a sub item of the item identified by serverFolderPath
        /// </summary>
        /// <param name="serverFolderPath">The path of a folder on the VC server</param>
        /// <returns></returns>
        bool IsSubItemOf(string serverFolderPath);
    }

}
