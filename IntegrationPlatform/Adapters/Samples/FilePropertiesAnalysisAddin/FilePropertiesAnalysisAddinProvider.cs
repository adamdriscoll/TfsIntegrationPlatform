// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace FilePropertiesAnalysisAddin
{
    /// <summary>
    /// The Provider that hosts the DefaultFileSystemLabelAddin
    /// </summary>
    [ProviderDescription(AddinGuid, AddinName, AddinVersion)]
    public class FilePropertiesAnalysisAddinProvider : IProvider
    {
        private const string AddinGuid = "ECE9273F-1048-40e9-B77F-E3F369627274";
        private const string AddinName = "FileProperties AnalysisAddin Provider";
        private const string AddinVersion = "1.0.0.0";

        private static IAddin s_defaultFilePropertiesAddin = new FilePropertiesAnalysisAddin();

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAddin))
            {
                return s_defaultFilePropertiesAddin;
            }

            return null;
        }

        #endregion


    }
}
