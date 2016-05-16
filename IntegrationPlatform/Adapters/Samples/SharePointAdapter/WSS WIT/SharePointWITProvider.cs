//------------------------------------------------------------------------------
// <copyright file="SharePointWITProvider.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// This is the main class of the WSS WIT Adapter.
    /// </summary>
    [ProviderDescription("585FC30C-F8FB-4D5C-80DE-796F0B3553EF", "SharePoint TIP Adapter - WIT", "1.0.0.0")]
    public class SharePointWITProvider : IProvider
    {
        private SharePointWITAnalysisProvider analysisProvider;
        private SharePointWITMigrationProvider migrationProvider;

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/>.
        /// -or-
        /// null if there is no service object of type <paramref name="serviceType"/>.
        /// </returns>
        public object GetService(Type serviceType)
        {
            TraceManager.TraceInformation("WSSWIT:A:GetService ({0})", serviceType.FullName);
            if (serviceType == typeof(IAnalysisProvider))
            {
                if (analysisProvider == null)
                {
                    analysisProvider = new SharePointWITAnalysisProvider();
                }

                return analysisProvider;
            }

            if (serviceType == typeof(IMigrationProvider))
            {
                if (migrationProvider == null)
                {
                    migrationProvider = new SharePointWITMigrationProvider();
                }

                return migrationProvider;
            }

            return null;
        }

        #endregion
    }
}
