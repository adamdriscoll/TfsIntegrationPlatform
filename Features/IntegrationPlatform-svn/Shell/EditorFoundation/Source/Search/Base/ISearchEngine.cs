// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Defines the public interface of an Search Engine.
    /// </summary>
    public interface ISearchEngine<T>
    {
        /// <summary>
        /// Searches for objects that match the specified criteria.
        /// </summary>
        /// <param name="searchCriteria">The string(s) being searched for.</param>
        /// <param name="additionalConstraints">Additional constraints to filter the search results.</param>
        /// <returns>An array of objects that match the search criteria and constraints.</returns>
        T[] Search (string searchCriteria, params Predicate<T>[] additionalConstraints);
    }
}
