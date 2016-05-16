// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// Default server path translation. Expected input server path should already
    /// be in canonical path syntax and no transformation is taken.
    /// </summary>
    internal class UnixStyleServerPathTranslationService : IServerPathTranslationService
    {
        #region IServerPathTranslationService Members

        /// <summary>
        /// The input is returned as is, i.e. no translation will be made.
        /// </summary>
        /// <param name="serverPath"></param>
        /// <returns></returns>
        public string TranslateToCanonicalPathCaseSensitive(string serverPath)
        {
            if (string.IsNullOrEmpty(serverPath))
            {
                throw new ArgumentNullException("serverPath");
            }

            return serverPath;
        }

        /// <summary>
        /// The input is returned as is, i.e. no translation will be made.
        /// </summary>
        /// <param name="canonicalPath"></param>
        /// <param name="canonicalFilterPath"></param>
        /// <returns></returns>
        public string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath)
        {
            return canonicalPath;
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
