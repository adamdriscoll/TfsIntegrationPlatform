// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public class CCTranslationService : IServerPathTranslationService
    {
        #region IServerPathTranslationService Members

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
                return serverPath.Replace(ClearCasePath.Separator, ClearCasePath.UnixSeparator);
            }
        }

        public string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath)
        {
            // Return a windows style clearcasepath from a unix style CanonicalPath.
            return canonicalPath.Replace(ClearCasePath.UnixSeparator, ClearCasePath.Separator);
        }

        public void Initialize(ReadOnlyCollection<FilterItem> filterItems)
        {
            return;
        }

        #endregion
    }
}

