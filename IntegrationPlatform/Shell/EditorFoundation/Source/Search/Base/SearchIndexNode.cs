// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Represents a single node in the index's tree.
    /// </summary>
    /// <typeparam name="T">The type of object with which the node is associated.</typeparam>
    internal class SearchIndexNode<T>
    {
        #region Fields
        private readonly SearchIndexNode<T> parent;
        private readonly Dictionary<T, bool> searchItems;
        private readonly Dictionary<char, SearchIndexNode<T>> childNodes;
        #endregion

        #region Constructors
        public SearchIndexNode (SearchIndexNode<T> parent)
        {
            this.parent = parent;
            this.searchItems = new Dictionary<T, bool> ();
            this.childNodes = new Dictionary<char, SearchIndexNode<T>> ();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets this node's parent node
        /// </summary>
        public SearchIndexNode<T> Parent
        {
            get
            {
                return this.parent;
            }
        }

        /// <summary>
        /// Gets the search items at this node.
        /// </summary>
        public Dictionary<T, bool> SearchItems
        {
            get
            {
                return this.searchItems;
            }
        }

        /// <summary>
        /// Gets this node's child nodes
        /// </summary>
        public Dictionary<char, SearchIndexNode<T>> ChildNodes
        {
            get
            {
                return this.childNodes;
            }
        }
        #endregion
    }
}
