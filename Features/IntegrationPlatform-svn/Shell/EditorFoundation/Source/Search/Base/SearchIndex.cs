// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Provides fast indexing for searching.
    /// </summary>
    /// <typeparam name="T">The type of object to which strings are mapped.</typeparam>
    internal class SearchIndex<T>
    {
        #region Fields
        private readonly SearchIndexNode<T> rootNode;
        private readonly Dictionary<char, Dictionary<SearchIndexNode<T>, bool>> nodeMap;
        #endregion

        #region Constructors
        public SearchIndex ()
        {
            this.rootNode = new SearchIndexNode<T> (null);
            this.nodeMap = new Dictionary<char, Dictionary<SearchIndexNode<T>, bool>> ();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Associate a search item with the specified search terms.
        /// </summary>
        /// <param name="searchItem">The item to which the terms map.</param>
        /// <param name="terms">The terms associated with the item.</param>
        public void AddTerms (T searchItem, params string[] terms)
        {
            lock (this)
            {
                foreach (string term in terms)
                {
                    if (!string.IsNullOrEmpty (term))
                    {
                        this.AddTerm (searchItem, term.ToLower ());
                    }
                }
            }
        }

        /// <summary>
        /// Disassociates a search item with the specified search terms.
        /// </summary>
        /// <param name="searchItem">The item to which the terms map.</param>
        /// <param name="terms">The terms no longer associated with the item.</param>
        public void RemoveTerms (T searchItem, params string[] terms)
        {
            lock (this)
            {
                foreach (string term in terms)
                {
                    if (!string.IsNullOrEmpty (term))
                    {
                        this.RemoveTerm (searchItem, term.ToLower ());
                    }
                }
            }
        }

        /// <summary>
        /// Clears the index.
        /// </summary>
        public void Clear ()
        {
            lock (this)
            {
                this.rootNode.ChildNodes.Clear ();
                this.nodeMap.Clear ();
            }
        }

        /// <summary>
        /// Finds all search items to which the specified term maps.
        /// </summary>
        /// <param name="term">The term for which to search.</param>
        /// <param name="wholeWord"><c>true</c> to search for whole word matches only, <c>false</c> otherwise.</param>
        /// <returns>An array of objects to which the specified term maps.</returns>
        public T[] FindSearchItems (string term, bool wholeWord)
        {
            lock (this)
            {
                char[] termCharacters = term.ToLower ().ToCharArray ();
                List<T> searchResults = new List<T> ();

                // For whole words, start at the root and only consider at most one node
                if (wholeWord)
                {
                    SearchIndexNode<T> matchingNode = this.FindSearchIndexNode (termCharacters, 0, this.rootNode);
                    if (matchingNode != null)
                    {
                        searchResults.AddRange (matchingNode.SearchItems.Keys);
                    }
                }
                // For substrings, start at all nodes associated with the first character of the search term
                else
                {
                    if (termCharacters.Length > 0)
                    {
                        Dictionary<SearchIndexNode<T>, bool> termStartNodes;
                        if (this.nodeMap.TryGetValue (termCharacters[0], out termStartNodes))
                        {                            
                            foreach (SearchIndexNode<T> searchIndexNode in termStartNodes.Keys)
                            {
                                SearchIndexNode<T> matchingNode = this.FindSearchIndexNode (termCharacters, 1, searchIndexNode);
                                if (matchingNode != null)
                                {
                                    // Since we are looking for substrings, include all descendants
                                    foreach (SearchIndexNode<T> descendentNode in this.EnumerateDescendents (matchingNode))
                                    {
                                        searchResults.AddRange (descendentNode.SearchItems.Keys);
                                    }
                                }
                            }
                        }
                    }
                }

                return searchResults.ToArray ();
            }
        }
        #endregion

        #region Private Methods
        private void AddTerm (T searchItem, string term)
        {
            this.AddTerm (searchItem, term.ToCharArray (), 0, this.rootNode);
        }

        // Recursively adds a term to the tree
        private void AddTerm (T searchItem, char[] partialTerm, int partialTermIndex, SearchIndexNode<T> currentNode)
        {
            char currentCharacter = partialTerm[partialTermIndex];

            SearchIndexNode<T> nextNode = null;
            if (!currentNode.ChildNodes.TryGetValue (currentCharacter, out nextNode))
            {
                // Add a new node to the tree
                nextNode = new SearchIndexNode<T> (currentNode);
                currentNode.ChildNodes[currentCharacter] = nextNode;

                // Keep a mapping of the character to the node, which is used for substring searches
                Dictionary<SearchIndexNode<T>, bool> searchIndexNodes = null;
                if (!this.nodeMap.TryGetValue (currentCharacter, out searchIndexNodes))
                {
                    searchIndexNodes = new Dictionary<SearchIndexNode<T>, bool> ();
                    this.nodeMap[currentCharacter] = searchIndexNodes;
                }
                searchIndexNodes.Add (nextNode, true);
            }

            // If we're down to one character, then we're done - add this search item to the node
            if (partialTerm.Length - partialTermIndex == 1)
            {
                nextNode.SearchItems[searchItem] = true;
            }
            // Otherwise, recurse
            else
            {
                this.AddTerm (searchItem, partialTerm, partialTermIndex + 1, nextNode);
            }
        }

        private void RemoveTerm (T searchItem, string term)
        {
            this.RemoveTerm (searchItem, term.ToCharArray (), 0, this.rootNode);
        }

        // Recursively removes a term from the tree
        private void RemoveTerm (T searchItem, char[] partialTerm, int partialTermIndex, SearchIndexNode<T> currentNode)
        {
            char currentCharacter = partialTerm[partialTermIndex];

            SearchIndexNode<T> nextNode = null;
            if (currentNode.ChildNodes.TryGetValue (currentCharacter, out nextNode))
            {
                // If we're down to one character, we've found the node
                if (partialTerm.Length - partialTermIndex == 1)
                {
                    // Remove the item
                    nextNode.SearchItems.Remove (searchItem);

                    // If there are no other items associated with this term, remove the node
                    if (nextNode.SearchItems.Count == 0)
                    {
                        currentNode.ChildNodes.Remove (currentCharacter);

                        // Also remove the mapping from character to node
                        this.nodeMap[currentCharacter].Remove (nextNode);
                        if (this.nodeMap[currentCharacter].Count == 0)
                        {
                            this.nodeMap.Remove (currentCharacter);
                        }
                    }
                }
                // Otherwise, recurse
                else
                {
                    this.RemoveTerm (searchItem, partialTerm, partialTermIndex + 1, nextNode);
                }
            }
        }

        // Recursively searches for a node matching the specified term
        private SearchIndexNode<T> FindSearchIndexNode (char[] partialTerm, int partialTermIndex, SearchIndexNode<T> currentNode)
        {
            // If we have recursed through all the characters in the term, then we have found the node
            if (partialTerm.Length - partialTermIndex == 0)
            {
                return currentNode;
            }

            // Otherwise, recurse
            SearchIndexNode<T> nextNode = null;

            if (currentNode.ChildNodes.TryGetValue (partialTerm[partialTermIndex], out nextNode))
            {
                return FindSearchIndexNode (partialTerm, partialTermIndex + 1, nextNode);
            }
            
            return null;
        }

        // Enumerates all descendent nodes as well as the specified node
        private IEnumerable<SearchIndexNode<T>> EnumerateDescendents (SearchIndexNode<T> searchIndexNode)
        {
            yield return searchIndexNode;
            foreach (SearchIndexNode<T> childNode in searchIndexNode.ChildNodes.Values)
            {
                yield return childNode;
                foreach (SearchIndexNode<T> descendentNode in this.EnumerateDescendents (childNode))
                {
                    yield return descendentNode;
                }
            }
        }
        #endregion
    }
}
