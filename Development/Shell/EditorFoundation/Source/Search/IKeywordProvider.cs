// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Exposes a method that gets a list of keywords.
    /// with which the specified object should be associated.
    /// </summary>
    public interface IKeywordProvider
    {
        /// <summary>
        /// Gets the set of keywords for the specified search object.
        /// </summary>
        /// <param name="searchObject">The search object from which to mine search keywords.</param>
        /// <returns>The set of keywords associated with the search object.</returns>
        string[] GetKeywords (object searchObject);
    }
}
