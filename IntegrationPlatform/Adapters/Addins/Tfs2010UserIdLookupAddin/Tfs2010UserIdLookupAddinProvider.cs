// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010UserIdLookupAddin
{
    /// <summary>
    /// The Provider that hosts the Tfs2008UserIdLookupAddin
    /// </summary>
    [ProviderDescription(AddinGuid, AddinName, AssemblyVersionInfo.VersionString)]
    public class Tfs2010UserIdLookupAddinProvider : IProvider
    {
        private const string AddinGuid = "EECC0227-8006-45f0-888D-10AB03019AD5";
        private const string AddinName = "TFS 2008 User Identity Lookup Add-In Provider";

        private static IAddin s_lookupAddin = new Tfs2010UserIdLookupAddin();

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAddin))
            {
                return s_lookupAddin;
            }

            return null;
        }

        #endregion


    }
}
