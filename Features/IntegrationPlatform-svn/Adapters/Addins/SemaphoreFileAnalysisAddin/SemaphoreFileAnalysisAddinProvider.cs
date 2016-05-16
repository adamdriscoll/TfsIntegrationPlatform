// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace SemaphoreFileAnalysisAddin
{
    /// <summary>
    /// The Provider that hosts the DefaultFileSystemPreAnalysisAddin
    /// </summary>
    [ProviderDescription(AddinGuid, AddinName, AddinVersion)]
    public class SemaphoreFileAnalysisAddinProvider : IProvider
    {
        private const string AddinGuid = "E8CEC3C5-5848-4b83-904F-4324094C3F78";
        private const string AddinName = "Semaphore File Analysis Addin Provider";
        private const string AddinVersion = "1.0.0.0";

        private static IAddin s_DefaultFileSystemPreAnalysisAddin = new SemaphoreFileAnalysisAddin();

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAddin))
            {
                return s_DefaultFileSystemPreAnalysisAddin;
            }

            return null;
        }

        #endregion


    }
}
