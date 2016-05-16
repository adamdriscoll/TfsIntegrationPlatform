// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// Interface that must be implemented by the VC adapters.
    /// </summary>
    public interface IServerPathTranslationService
    {
        /// <summary>
        /// Service initialization. The platform calls this method, and passes the filter strings
        /// that're specific to the migration source, for which an instance of this service is created.
        /// </summary>
        /// <param name="filterItems"></param>
        void Initialize(ReadOnlyCollection<FilterItem> filterItems);

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
        /// 
        /// Canonical Server Path Semantics
        /// • A valid canonical server path must start with a canonical filter path for the corresponding endpoint
        ///    EXAMPLE:
        ///      Filter string (endpoint-specific server path): $/TP-A/Root
        ///      Filter string (translated canonical path): /TP-A/Root
        ///      
        ///    Valid Canonical Server Paths
        ///      /TP-A/Root
        ///      /TP-A/Root/foo
        ///      /TP-A/Root/foo/foo1.txt
        ///    Invalid Canonical Server Paths
        ///      /TP-A/root
        ///      /TP-B/Root/foo
        ///      /TP-A/Root_1/foo
        /// </remarks>
        string TranslateToCanonicalPathCaseSensitive(string serverPath);

        /// <summary>
        /// Translate a canonical path to the adapter-specific server path based on the "filter path"
        /// </summary>
        /// <param name="canonicalPath"></param>
        /// <param name="canonicalFilterPath"></param>
        /// <returns></returns>
        string TranslateFromCanonicalPath(string canonicalPath, string canonicalFilterPath);
    }
}
