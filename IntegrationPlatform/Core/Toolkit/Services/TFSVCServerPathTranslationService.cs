// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// The IServerPathTranslationService implementation for TFS VC server paths
    /// to translate between "$/dir0/dir1/..." (TFS) to "/dir0/dir1/..." (canonical)
    /// </summary>
    public class TFSVCServerPathTranslationService : IServerPathTranslationService
    {
        #region IServerPathTranslationService Members

        /// <summary>
        /// Case-sensitive translation from the adapter-specific server path to the canonical path
        /// </summary>
        /// <param name="serverPath"></param>
        /// <returns></returns>
        public string TranslateToCanonicalPathCaseSensitive(string serverPath)
        {
            if (string.IsNullOrEmpty(serverPath))
            {
                throw new ArgumentNullException("serverPath");
            }

            if (serverPath.Length == 1)
            {
                return "/";
            }
            else
            {
                return serverPath.Substring(1);
            }
        }

        /// <summary>
        /// Translate a canonical path to the adapter-specific server path based on the "filter path"
        /// </summary>
        /// <param name="canonicalPath"></param>
        /// <param name="canonicalFilterPath"></param>
        /// <returns></returns>
        public string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath)
        {
            return "$" + canonicalPath;
        }

        /// <summary>
        /// Service initialization. The platform calls this method, and passes the filter strings
        /// that're specific to the migration source, for which an instance of this service is created.
        /// </summary>
        /// <param name="filterItems"></param>
        public void Initialize(ReadOnlyCollection<FilterItem> filterItems)
        {
            return;
        }

        #endregion
    }
}
