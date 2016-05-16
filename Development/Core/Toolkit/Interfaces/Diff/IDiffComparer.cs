// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// An internal interface implemented by classes that compare server items for a diff operation
    /// to determine differences.
    /// </summary>
    internal interface IDiffComparer
    {
        /// <summary>
        /// Compare the contents of items in two servers; the type of items to compare is determined by the implementing
        /// class
        /// </summary>
        /// <param name="leftQualifier">Specifies more specific information about the data in the server to compare in the first server
        /// (for example a version number or time)</param>
        /// <param name="rightQualifier">Specifies more specific information about the data in the server to compare in the second server
        /// (for example a version number or time)</param>
        /// <returns></returns>
        bool VerifyContentsMatch(string leftQualifier, string rightQualifier);
    }
}
