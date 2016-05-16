// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Provides resource values based on resource keys.
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Gets the resource with the specified key.
        /// </summary>
        object this[string key]
        {
            get;
        }

        /// <summary>
        /// Gets the active culture.
        /// </summary>
        CultureInfo ActiveCulture
        {
            get;
        }
    }
}
