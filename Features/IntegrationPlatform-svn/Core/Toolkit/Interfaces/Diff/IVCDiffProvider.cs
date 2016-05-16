// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// An interface that version control migration providers should implement to support diff operations
    /// used to validate the correctness of a migration.
    /// </summary>
    public interface IVCDiffProvider : IDiffProvider, IServiceProvider
    {
        /// <summary>
        /// Takes as input a "treeFilterSpecifier" string that specifies the a sub tree or tree view on the version control
        /// server that will be examined by calls to GetFolderSubDiffItems.  The return value of the method should be an IDiffItem 
        /// implementation with the Path to this sub tree or view in the form that should be passed to GetFolderSubDiffItems
        /// to get the top level items in the sub tree or view.
        /// The version argument (if not null or empty) specifies a version of the tree to use (the version string may 
        /// specify a numbered version, labeled version, time-stamped version or other version dependin on the type of endpoint
        /// and its adapter's implementation of IVCServerDiffProvider.
        /// The IVCServerDiffProvider implementation can perform any one-time initialization or cache for this tree
        /// as this will be called just once for the specified tree.
        /// Some adapter implementations may not need to perform any such initialization
        /// For other adapters, the treeFilterSpecifier may be a logical name that identies a sub-tree or view on a source control tree.
        /// </summary>
        /// <param name="treeFilterSpecifier">A string that identifies a source control sub tree (or view) as defined by the adapter implementation</param>
        /// <param name="version">The version of the item; if null or empty, the tip version is accessed</param>
        /// <returns>An IDiffItem for the root item of the version control tree.  
        /// It should return null if treeFilterSpecifier specifies a folder that does not exist</returns>
        IVCDiffItem InitializeForDiff(string treeFilterSpecifier, string version);

        /// <summary>
        /// Enumerate the diff items found in the folder represented by the IDiffItem passed in (do not enumerate recursively)
        /// </summary>
        /// <param name="folderDiffItem">An IDiffItem implementation with a VCItemType of Folder</param>
        /// <returns>An enumeration of IDiffItems each representing a file or folder in the folder specified by the diffItem argument</returns>
        IEnumerable<IVCDiffItem> GetFolderSubDiffItems(IVCDiffItem folderDiffItem);

        /// <summary>
        /// Give the IVCDiffProvider a chance to cleanup any reources allocated during InitializeForDiff()
        /// </summary>
        /// <param name="rootDiffItem">A root level folder IDiffItem returned by a call to InitializeForDiff</param>
        void Cleanup(IVCDiffItem rootDiffItem);
    }
}
