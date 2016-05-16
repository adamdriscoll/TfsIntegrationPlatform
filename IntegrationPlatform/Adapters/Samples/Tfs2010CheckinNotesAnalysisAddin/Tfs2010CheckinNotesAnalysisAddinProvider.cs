// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Tfs2010CheckinNotesAnalysisAddin
{
    /// <summary>
    /// The Provider that hosts the Tfs2010CheckinNotesAnalysisAddin
    /// </summary>
    [ProviderDescription(AddinGuid, AddinName, AddinVersion)]
    public class Tfs2010CheckinNotesAnalysisAddinProvider : IProvider
    {
        private const string AddinGuid = "DBE41FB0-83E1-41e8-B5E8-B18614AA99D8";
        private const string AddinName = "TFS 2010 Checkin Notes Analysis Addin Provider";
        private const string AddinVersion = "1.0.0.0";

        private static IAddin s_tfs2010CheckinNotesAddin = new Tfs2010CheckinNotesAnalysisAddin();

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IAddin))
            {
                return s_tfs2010CheckinNotesAddin;
            }

            return null;
        }

        #endregion


    }
}
