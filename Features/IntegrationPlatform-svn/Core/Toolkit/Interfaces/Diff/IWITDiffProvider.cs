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
    public interface IWITDiffProvider : IDiffProvider, IServiceProvider
    {
        /// <summary>
        /// The implementation can perform any one-time initialization here.
        /// Some adapter implementations may not need to perform any such initialization.
        /// It takes as optional arguments a filterString and a version that would be applied during subsequent queries.
        /// </summary>
        /// <param name="filterString">A string that specifies some filtering condition; if null or empty no additional filtering is applied</param>
        /// <param name="provideForContentComparison">If true, any IDiffItem returned by any method should include the contents of the work item for comparison purposed.
        /// If false, detailed content data can be left out: specifically implementations of the IWITDiffItem can return null for the
        /// XmlDocument WorkItemDetails as it is only needed when content comparison is required
        /// </param>
        void InitializeForDiff(string filterString, bool provideForContentComparison);

        /// <summary>
        /// Enumerate the diff items found based on the query passed in as well as the filterString and version passed
        /// to InitializeForDiff.  The return type is IEnumerable so that adapter implementations do not need download and keep 
        /// all of the IWITDiffItems in memory at once.
        /// </summary>
        /// <param name="queryCondition">A string that specifies a query condition used to select a subset of the work items defined by 
        /// the set that the filter string identified.  The caller may pass null or empty in which case the adapter should not add a condition.</param>
        /// <returns>An enumeration of IWITDiffItems each representing a work item to be compared by the WIT Diff operation</returns>
        IEnumerable<IWITDiffItem> GetWITDiffItems(string queryCondition);

        /// <summary>
        /// Return a IWITDiffItem representing a single work item as identified by the adapter specific workItemId string
        /// </summary>
        /// <param name="workItemId"></param>
        /// <returns></returns>
        IWITDiffItem GetWITDiffItem(string workItemId);

        /// <summary>
        /// IgnoreFieldInComparison is called to allow an adapter to specify that a named field should not be compared
        /// in the diff operation
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        bool IgnoreFieldInComparison(string fieldName);

        /// <summary>
        /// Give the IWITDiffProvider a chance to cleanup any reources allocated during InitializeForDiff()
        /// </summary>
        void Cleanup();
    }
}
