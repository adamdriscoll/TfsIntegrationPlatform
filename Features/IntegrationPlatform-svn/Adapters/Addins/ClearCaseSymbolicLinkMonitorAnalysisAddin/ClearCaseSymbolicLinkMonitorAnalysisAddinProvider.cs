// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace ClearCaseSymbolicLinkMonitorAnalysisAddin
{
    /// <summary>
    /// The Provider that hosts the ClearCaseSymbolicLinkMonitorAnalysisAddin
    /// </summary>
    [ProviderDescription(AddinGuid, AddinName, AddinVersion)]
    public class ClearCaseSymbolicLinkMonitorAnalysisAddinProvider : IProvider
    {
        private const string AddinGuid = "1CCE9B9A-E18E-4D04-AD01-1B6046929426";
        private const string AddinName = "ClearCase SymbolicLink Monitor AnalysisAddin Provider";
        private const string AddinVersion = "1.0.0.0";

        private static IAddin s_clearCaseSymbolicLinkMonitor = new ClearCaseSymbolicLinkMonitorAnalysisAddin();

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAddin))
            {
                return s_clearCaseSymbolicLinkMonitor;
            }

            return null;
        }

        #endregion
    }
}
