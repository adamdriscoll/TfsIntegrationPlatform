// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Provides data for the SearchComplete event.
    /// </summary>
    public class SearchCompleteEventArgs : EventArgs
    {
        #region Fields
        private readonly EditorSearchItem[] searchItems;
        #endregion

        #region Constructors
        internal SearchCompleteEventArgs (EditorSearchItem[] searchItems)
        {
            this.searchItems = searchItems;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the set of search items that matched the search criteria.
        /// </summary>
        public EditorSearchItem[] SearchItems
        {
            get
            {
                return this.searchItems;
            }
        }
        #endregion
    }
}
