// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace ChangeGroupLabelAnalysisAddin
{
    /// <summary>
    /// The Provider that hosts the DefaultChangeGroupLabelAddin
    /// </summary>
    [ProviderDescription(AddinGuid, AddinName, AddinVersion)]
    public class ChangeGroupLabelAnalysisAddinProvider : IProvider
    {
        private const string AddinGuid = "A4F53905-25B6-4311-AC0C-637DA6688F2B";
        private const string AddinName = "ChangeGroup Label AnalysisAddin Provider";
        private const string AddinVersion = "1.0.0.0";

        private static IAddin s_defaultChangeGroupLabelAddin = new ChangeGroupLabelAnalysisAddin();

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAddin))
            {
                return s_defaultChangeGroupLabelAddin;
            }

            return null;
        }

        #endregion


    }
}
