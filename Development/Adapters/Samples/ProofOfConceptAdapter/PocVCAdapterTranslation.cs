// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Rangers.TFS.Migration.PocAdapter.VC
{
    public class PoCVCAdapterTranslation : IServerPathTranslationService
    {
        #region IServerPathTranslationService Members

        public PoCVCAdapterTranslation()
        {
        }

        public string TranslateToCanonicalPathCaseSensitive(string serverPath)
        {
            if (string.IsNullOrEmpty(serverPath))
            {
                throw new ArgumentNullException("serverPath", serverPath);
            }
            else
                if (serverPath.Length == 1)
                {
                    return "/";
                }
                else
                {
                    if (serverPath.Length > 2)
                    {
                        // This silly translator converts a D:\Path to /D/Path
                        StringBuilder workPath = new StringBuilder();
                        workPath.Append("/" + serverPath[0] + serverPath.Substring(2));
                        workPath = workPath.Replace(@"\", @"/");
                        return workPath.ToString();
                    }
                    else
                    {
                        throw new ArgumentNullException("serverPath");
                    }
                }
        }

        public string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath)
        {
            if (string.IsNullOrEmpty(canonicalPath))
            {
                throw new ArgumentNullException("canonicalPath", canonicalPath);
            }
            else
            {
                if (canonicalPath.Length > 2)
                {
                    // This silly translator converts a /D/Path to C:/Path
                    StringBuilder workPath = new StringBuilder();
                    workPath.Append(canonicalPath.Replace(@"/", @"\"));
                    workPath[0] = workPath[1];
                    workPath[1] = ':';
                    return workPath.ToString();
                }
                else
                {
                    throw new ArgumentNullException("canonicalPath", canonicalPath);
                }
            }
        }

        public void Initialize(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.TeamFoundation.Migration.BusinessModel.FilterItem> filterItems)
        {
        }

        #endregion
    }
}
