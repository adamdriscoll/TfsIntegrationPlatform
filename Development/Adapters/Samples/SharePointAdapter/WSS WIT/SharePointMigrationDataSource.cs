//------------------------------------------------------------------------------
// <copyright file="WSSMigrationDataSource.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System.Net;

    /// <summary>
    /// 
    /// </summary>
    public class SharePointMigrationDataSource
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the name of the list.
        /// </summary>
        /// <value>The name of the list.</value>
        public string ListName { get; set; }

        /// <summary>
        /// Defines the user details
        /// </summary>
        public NetworkCredential Credentials { get; set; }
    }
}
