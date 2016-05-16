// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Provides a base class for search engines.
    /// </summary>
    /// <typeparam name="T">The type of object for which the search engine searches.</typeparam>
    public abstract class SearchEngine<T> : ISearchEngine<T>
    {
        #region Fields
        private readonly SearchIndex<T> searchIndex;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the SearchEngine class.
        /// </summary>
        public SearchEngine ()
        {
            this.searchIndex = new SearchIndex<T> ();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Searches for objects that match the specified criteria.
        /// </summary>
        /// <param name="searchCriteria">The string(s) being searched for.</param>
        /// <param name="additionalConstraints">Additional constraints to filter the search results.</param>
        /// <returns>An array of objects that match the search criteria and constraints.</returns>
        public T[] Search (string searchCriteria, params Predicate<T>[] additionalConstraints)
        {
            if (string.IsNullOrEmpty (searchCriteria))
            {
                throw new ArgumentException ("Search criteria must be specified", "searchCriteria");
            }

            Dictionary<T, bool> searchResults = new Dictionary<T, bool> ();
            foreach (string searchCriterion in searchCriteria.Split (' '))
            {
                foreach (T searchItem in this.searchIndex.FindSearchItems (searchCriterion, false))
                {
                    if (!searchResults.ContainsKey (searchItem))
                    {
                        bool skip = false;
                        foreach (Predicate<T> additionalConstraint in additionalConstraints)
                        {
                            if (!additionalConstraint (searchItem))
                            {
                                skip = true;
                                break;
                            }
                        }

                        if (!skip)
                        {
                            searchResults.Add (searchItem, true);
                        }
                    }
                }
            }

            T[] searchResultArray = new T[searchResults.Count];
            searchResults.Keys.CopyTo (searchResultArray, 0);
            return searchResultArray;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Associates search terms with a search item.
        /// </summary>
        /// <param name="searchItem">The search item that will be associated with the specified search terms.</param>
        /// <param name="searchTerms">The set of search terms.</param>
        protected void AddTerms (T searchItem, params string[] searchTerms)
        {
            this.searchIndex.AddTerms (searchItem, searchTerms);
        }

        /// <summary>
        /// Disassociates search terms with a search item.
        /// </summary>
        /// <param name="searchItem">The search item that will be disassociated with the specified search terms.</param>
        /// <param name="searchTerms">The set of search terms.</param>
        protected void RemoveTerms (T searchItem, params string[] searchTerms)
        {
            this.searchIndex.RemoveTerms (searchItem, searchTerms);
        }

        /// <summary>
        /// Clears the search index.
        /// </summary>
        protected void ClearIndex ()
        {
            this.searchIndex.Clear ();
        }
        #endregion
    }
}
