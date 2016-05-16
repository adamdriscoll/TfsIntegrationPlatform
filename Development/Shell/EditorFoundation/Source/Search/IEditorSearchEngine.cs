// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Defines the public interface of an Editor Search Engine.
    /// </summary>
    public interface IEditorSearchEngine : ISearchEngine<EditorSearchItem>
    {
        /// <summary>
        /// Gets the status of the Search Engine.
        /// </summary>
        IndexingStatus Status { get; }

        /// <summary>
        /// Starts an asynchronous search operation.
        /// </summary>
        /// <param name="searchCriteria">The string(s) being searched for.</param>
        /// <param name="additionalConstraints">Additional constraints to filter the search results.</param>
        void SearchAsync (string searchCriteria, params Predicate<EditorSearchItem>[] additionalConstraints);

        /// <summary>
        /// Raised when the status of the Search Engine changes.
        /// </summary>
        event EventHandler StatusChanged;

        /// <summary>
        /// Raised when an asynchronous search is complete.
        /// </summary>
        event EventHandler<SearchCompleteEventArgs> SearchComplete;
    }
}
