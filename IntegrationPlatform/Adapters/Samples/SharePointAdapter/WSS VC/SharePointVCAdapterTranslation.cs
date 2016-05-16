//------------------------------------------------------------------------------
// <copyright file="SharePointVCAdapterTranslation.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using System.Collections.ObjectModel;
    using Microsoft.TeamFoundation.Migration.BusinessModel;
    using Microsoft.TeamFoundation.Migration.Toolkit;
    using Microsoft.TeamFoundation.Migration.Toolkit.Services;
    using System.Globalization;

    /// <summary>
    /// This class handles the translation between SharePoint server paths and the internal path format.
    /// </summary>
    public class SharePointVCAdapterTranslation : IServerPathTranslationService
    {
        #region IServerPathTranslationService Members

        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointVCAdapterTranslation"/> class.
        /// </summary>
        public SharePointVCAdapterTranslation()
        {
            TraceManager.TraceInformation("WSSVC:Translation Server");
        }

        /// <summary>
        /// Case-sensitive translation from the adapter-specific server path to the canonical path
        /// </summary>
        /// <param name="serverPath"></param>
        /// <returns></returns>
        /// <remarks>
        /// Canonical Server Path Syntax
        /// • A canonical path uses forward-slash, i.e. '/', as the path delimiter
        /// • A canonical path must be an absolute path, i.e. starting with the path delimiter
        /// • A canonical path is case-sensitive
        /// Canonical Server Path Semantics
        /// • A valid canonical server path must start with a canonical filter path for the corresponding endpoint
        /// EXAMPLE:
        /// Filter string (endpoint-specific server path): $/TP-A/Root
        /// Filter string (translated canonical path): /TP-A/Root
        /// Valid Canonical Server Paths
        /// /TP-A/Root
        /// /TP-A/Root/foo
        /// /TP-A/Root/foo/foo1.txt
        /// Invalid Canonical Server Paths
        /// /TP-A/root
        /// /TP-B/Root/foo
        /// /TP-A/Root_1/foo
        /// </remarks>
        public string TranslateToCanonicalPathCaseSensitive(string serverPath)
        {
            TraceManager.TraceInformation("WSSVC:TranslationToCanonical - {0}", serverPath);
            string localPath = string.Format(CultureInfo.CurrentCulture, "/{0}", serverPath);
            
            TraceManager.TraceInformation("WSSVC:New:{0} -> {1}", serverPath, localPath);
            return localPath;
        }

        /// <summary>
        /// Translate a canonical path to the adapter-specific server path based on the "filter path"
        /// </summary>
        /// <param name="canonicalPath"></param>
        /// <param name="canonicalFilterPath"></param>
        /// <returns></returns>
        public string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath)
        {
            TraceManager.TraceInformation("WSSVC:TranslationFromCanonical - {0} - {1}", canonicalPath, canonicalFilterPath);
            string result = new Uri(canonicalPath.Substring(1)).AbsoluteUri;
            TraceManager.TraceInformation("WSSVC:TranslationFromCanonical:Result {0}", result);
            return result;
        }


        /// <summary>
        /// Service initialization. The platform calls this method, and passes the filter strings
        /// that're specific to the migration source, for which an instance of this service is created.
        /// </summary>
        /// <param name="filterItems"></param>
        public void Initialize(ReadOnlyCollection<FilterItem> filterItems)
        {
        }

        #endregion
    }
}
