//------------------------------------------------------------------------------
// <copyright file="SharePointVCProvider.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;

    /// <summary>
    /// The primary class for the SharePoint version control adapter.
    /// </summary>
    [ProviderDescription("{7F3F91B2-758A-4B3C-BBA8-CE34AE1D48EE}", "SharePoint TIP Adapter - Version Control", "1.0.0.0")]
    public class SharePointVCProvider : IProvider
    {        
        private IAnalysisProvider analysisProvider;
        private IMigrationProvider migrationProvider;
        private IServerPathTranslationService transalationProvider;        

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
        object IServiceProvider.GetService(Type serviceType)
        {
            TraceManager.TraceInformation("WSSVC:Adapter:GetService - {0}", serviceType);

            if (serviceType == typeof(IAnalysisProvider))
            {
                if (analysisProvider == null)
                {
                    analysisProvider = new SharePointVCAnalysisProvider();
                }
                return analysisProvider;
            }
            
            if (serviceType == typeof(IMigrationProvider))
            {
                if (migrationProvider == null)
                {
                    migrationProvider = new SharePointVCMigrationProvider();
                }
                return migrationProvider;
            }

            if (serviceType == typeof(IServerPathTranslationService))
            {
                if (transalationProvider == null)
                {
                    transalationProvider = new SharePointVCAdapterTranslation();
                }
                return transalationProvider;
            }        

            return null;
        }

        #endregion

    }
}
