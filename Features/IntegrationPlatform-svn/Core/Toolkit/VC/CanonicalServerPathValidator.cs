// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.VC
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Canonical Server Path Syntax
    /// • A canonical path uses forward-slash, i.e. '/', as the path delimiter (note that "/" is a valid path)
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
    internal class CanonicalServerPathValidator
    {
        public const string Delimiter = "/";

        /// <summary>
        /// C'tor.
        /// </summary>
        /// <param name="canonicalFilterStrings">filter strings in the form of canonical server path; outer dict key-ed on migraiton source id, inner one key-ed the canonical path corresponding to outer keyed migration source</param>
        public CanonicalServerPathValidator(Dictionary<Guid, Dictionary<string, string>> canonicalFilterStrings)
        {
            CanonicalFilterStrings = canonicalFilterStrings;
        }

        public bool IsValidPath(Guid migrationSourceId, string canonicalPath)
        {
            if (!IsSyntaxCorrect(canonicalPath))
            {
                return false;
            }

            return IsSemanticsValid(migrationSourceId, canonicalPath);
        }

        public bool IsSyntaxCorrect(string canonicalPath)
        {
            if (string.IsNullOrEmpty(canonicalPath))
            {
                return false;
            }

            if (!canonicalPath.StartsWith(Delimiter, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public bool IsSemanticsValid(Guid migrationSourceId, string canonicalPath)
        {
            if (!CanonicalFilterStrings.ContainsKey(migrationSourceId))
            {
                return false;
            }

            foreach (var filterPair in CanonicalFilterStrings[migrationSourceId])
            {
                string fromFilter = filterPair.Key;
                
                if (canonicalPath.StartsWith(fromFilter, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        Dictionary<Guid, Dictionary<string, string>> CanonicalFilterStrings
        {
            get;
            set;
        }
    }
}
