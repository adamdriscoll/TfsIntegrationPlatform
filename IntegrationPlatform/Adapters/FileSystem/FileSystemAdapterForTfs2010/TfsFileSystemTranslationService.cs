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

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    public class TfsFileSystemTranslationService : IServerPathTranslationService
    {
        #region IServerPathTranslationService Members

        public string TranslateToCanonicalPathCaseSensitive(string serverPath)
        {
            if (string.IsNullOrEmpty(serverPath))
            {
                throw new ArgumentNullException("serverPath");
            }

            return '/' + serverPath.Replace('\\', '/');

            
        }

        public string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath)
        {
            return canonicalPath.Substring(1).Replace('/', '\\');
        }

        public void Initialize(ReadOnlyCollection<FilterItem> filterItems)
        {
            return;
        }

        #endregion
    }
}

