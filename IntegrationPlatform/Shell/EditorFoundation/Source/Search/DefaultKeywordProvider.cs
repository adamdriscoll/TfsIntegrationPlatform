// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// A default keyword provider. This provider
    /// calls ToString on the object and splits it
    /// on spaces.
    /// </summary>
    public class DefaultKeywordProvider : IKeywordProvider
    {
        #region Public Methods
        /// <summary>
        /// Gets the set of keywords for the specified search object.
        /// </summary>
        /// <remarks>
        /// If the search object is an ICollection, then ToString is called on each item, and
        /// the result of each ToString is split on the space character. The result of all of
        /// these ToStrings and Splits produces the set of keywords.
        /// <para>
        /// If the search object is not an ICollection, then ToString is called on the object,
        /// and the result is split on the space character. The result is returned as the set of
        /// keywords.
        /// </para>
        /// </remarks>
        /// <param name="searchObject">The search object from which to mine search keywords.</param>
        /// <returns>The set of keywords associated with the search object.</returns>
        public string[] GetKeywords (object searchObject)
        {
            if (searchObject is ICollection)
            {
                List<string> allKeywords = new List<string> ();
                foreach (object item in (IEnumerable)searchObject)
                {
                    allKeywords.AddRange (this.GetKeywordsInternal (item));
                }
                return allKeywords.ToArray ();
            }
            else if (searchObject != null)
            {
                return this.GetKeywordsInternal (searchObject);
            }
            return null;
        }
        #endregion

        #region Private Methods
        private string[] GetKeywordsInternal (object searchObject)
        {
            return searchObject.ToString ().Split (' ');
        }
        #endregion
    }
}
