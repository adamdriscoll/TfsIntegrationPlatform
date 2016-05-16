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
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.VC
{
    /// <summary>
    /// VCServerPathTranslationService
    /// </summary>
    /// <remarks>
    /// Cloaked paths are not translated
    /// </remarks>
    internal class VCServerPathTranslationService : IVCServerPathTranslationService
    {
        Dictionary<Guid, IServerPathTranslationService> m_adapterPathTranslationServices = new Dictionary<Guid,IServerPathTranslationService>();

        // outer dict key-ed on migraiton source id, inner one key-ed the canonical path corresponding to outer keyed migration source
        Dictionary<Guid, Dictionary<string, string>> m_canonicalFilterStrings;

        // dict key-ed on migration source id, inner list contains the cloaked filter strings specific to the key-ed migration source
        Dictionary<Guid, List<string>> m_canonicalFilterStringsCloakedOnly;

        CanonicalServerPathValidator m_pathValidator;

        internal static char Separator = '/';

        public VCServerPathTranslationService(
            Guid leftMigrationSourceId,
            IServerPathTranslationService leftPathTranslationService,
            Guid rightMigrationSourceId,
            IServerPathTranslationService rightPathTranslationService,
            Collection<FilterPair> filterStrings)
        {
            m_canonicalFilterStrings = new Dictionary<Guid, Dictionary<string, string>>();
            m_pathValidator = new CanonicalServerPathValidator(m_canonicalFilterStrings);
            m_canonicalFilterStringsCloakedOnly = new Dictionary<Guid,List<string>>();

            m_adapterPathTranslationServices.Add(leftMigrationSourceId, leftPathTranslationService);
            m_adapterPathTranslationServices.Add(rightMigrationSourceId, rightPathTranslationService);

            TranslateFilterStringPairs(filterStrings, leftMigrationSourceId, rightMigrationSourceId);
        }

        #region IVCServerPathTranslationService Members

        public string Translate(Guid srcMigrationSourceId, string srcServerPath)
        {
            IServerPathTranslationService srcTranslationService = LookupService(srcMigrationSourceId);
            string canonicalPath = srcTranslationService.TranslateToCanonicalPathCaseSensitive(srcServerPath);

            if (!m_pathValidator.IsSyntaxCorrect(canonicalPath))
            {
                throw new InvalidServerPathException(string.Format(
                    MigrationToolkitResources.VCPathTranslationErrVCPathTranslationError_InvalidCanonicalPath,
                    canonicalPath));
            }

            string canonicalFilterPath;
            canonicalPath = ApplyFilterMapping(srcMigrationSourceId, canonicalPath, out canonicalFilterPath);

            foreach (Guid sourceId in m_adapterPathTranslationServices.Keys)
            {
                if (!sourceId.Equals(srcMigrationSourceId))
                {
                    IServerPathTranslationService destTranslationService = LookupService(sourceId);
                    return destTranslationService.TranslateFromCanonicalPath(canonicalPath, canonicalFilterPath);
                }
            }

            throw new ServerPathTranslationException(
                string.Format(
                MigrationToolkitResources.Culture,
                MigrationToolkitResources.VCPathTranslationError_CannotFindFilterString,
                srcServerPath));
        }

        #endregion

        private void TranslateFilterStringPairs(
            Collection<FilterPair> filterStrings, 
            Guid leftMigrationSourceId, 
            Guid rightMigrationSourceId)
        {
            m_canonicalFilterStringsCloakedOnly.Add(leftMigrationSourceId, new List<string>());
            m_canonicalFilterStringsCloakedOnly.Add(rightMigrationSourceId, new List<string>());

            IServerPathTranslationService leftPathTranslationService = LookupService(leftMigrationSourceId);
            IServerPathTranslationService rightPathTranslationService = LookupService(rightMigrationSourceId);

            foreach (FilterPair filterPair in filterStrings)
            {
                Debug.Assert(filterPair.FilterItem.Count == 2, "FilterPairs must have exactly two items");
                string leftCanonicalFilter = null;
                string rightCanonicalFilter = null;

                for (int i = 0; i < 2; ++i)
                {
                    FilterItem filterItem = filterPair.FilterItem[i];
                    string adapterSpecificFilterStr = filterItem.FilterString;
                    Guid filterOwnerMigrationSourceId = new Guid(filterItem.MigrationSourceUniqueId);

                    if (leftMigrationSourceId.Equals(filterOwnerMigrationSourceId))
                    {
                        leftCanonicalFilter = leftPathTranslationService.TranslateToCanonicalPathCaseSensitive(adapterSpecificFilterStr);

                        if (m_pathValidator.IsSyntaxCorrect(leftCanonicalFilter))
                        {
                            leftCanonicalFilter = RemoveTailingDelimiter(leftCanonicalFilter);
                        }
                        
                        if (filterPair.Neglect)
                        {
                            m_canonicalFilterStringsCloakedOnly[leftMigrationSourceId].Add(leftCanonicalFilter);
                        }
                    }
                    else if (rightMigrationSourceId.Equals(filterOwnerMigrationSourceId))
                    {
                        rightCanonicalFilter = rightPathTranslationService.TranslateToCanonicalPathCaseSensitive(adapterSpecificFilterStr);

                        if (m_pathValidator.IsSyntaxCorrect(rightCanonicalFilter))
                        {
                            rightCanonicalFilter = RemoveTailingDelimiter(rightCanonicalFilter);
                        }

                        if (filterPair.Neglect)
                        {
                            m_canonicalFilterStringsCloakedOnly[rightMigrationSourceId].Add(rightCanonicalFilter);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(leftCanonicalFilter) && !string.IsNullOrEmpty(rightCanonicalFilter))
                {
                    if (!m_canonicalFilterStrings.ContainsKey(leftMigrationSourceId))
                    {
                        m_canonicalFilterStrings.Add(leftMigrationSourceId, new Dictionary<string, string>());
                    }

                    if (!m_canonicalFilterStrings.ContainsKey(rightMigrationSourceId))
                    {
                        m_canonicalFilterStrings.Add(rightMigrationSourceId, new Dictionary<string, string>());
                    }

                    // there should be no 1:M filter mapping in the config
                    if (!m_canonicalFilterStrings[leftMigrationSourceId].ContainsKey(leftCanonicalFilter)
                        && !m_canonicalFilterStrings[rightMigrationSourceId].ContainsKey(rightCanonicalFilter))
                    {
                        m_canonicalFilterStrings[leftMigrationSourceId].Add(leftCanonicalFilter, rightCanonicalFilter);
                        m_canonicalFilterStrings[rightMigrationSourceId].Add(rightCanonicalFilter, leftCanonicalFilter);
                    }
                }
            }
        }

        private string RemoveTailingDelimiter(string canonicalFilter)
        {
            if (canonicalFilter.EndsWith(CanonicalServerPathValidator.Delimiter))
            {
                canonicalFilter = canonicalFilter.Substring(0, canonicalFilter.Length - 1);
            }

            return canonicalFilter;
        }

        private IServerPathTranslationService LookupService(Guid srcMigrationSourceId)
        {
            if (!m_adapterPathTranslationServices.ContainsKey(srcMigrationSourceId))
            {
                return null;
            }

            return m_adapterPathTranslationServices[srcMigrationSourceId];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcMigrationSourceId"></param>
        /// <param name="canonicalPath"></param>
        /// <param name="canonicalFilterPath">outputs the target/translated canonical filter path that replaces the original one</param>
        /// <returns>a canonical path with the filter path replacement applied</returns>
        private string ApplyFilterMapping(
            Guid srcMigrationSourceId, 
            string canonicalPath, 
            out string canonicalFilterPath)
        {
            if (!m_canonicalFilterStrings.ContainsKey(srcMigrationSourceId))
            {
                throw new ServerPathTranslationException(
                    string.Format(
                    MigrationToolkitResources.Culture,
                    MigrationToolkitResources.VCPathTranslationError_CannotFindFilterString, canonicalPath));
            }

            KeyValuePair<string, string>? mostSpecificFilterPair = null;

            foreach (var filterPair in m_canonicalFilterStrings[srcMigrationSourceId])
            {
                if (IsSubItem(canonicalPath, filterPair.Key))
                {
                    if (!mostSpecificFilterPair.HasValue ||
                        mostSpecificFilterPair.Value.Key.Length > filterPair.Key.Length)
                    {
                        mostSpecificFilterPair = filterPair;
                    }
                }
            }

            if (mostSpecificFilterPair.HasValue
                && !m_canonicalFilterStringsCloakedOnly[srcMigrationSourceId].Contains(mostSpecificFilterPair.Value.Key))
            {
                string fromFilter = mostSpecificFilterPair.Value.Key;
                string toFilter = mostSpecificFilterPair.Value.Value;

                canonicalFilterPath = toFilter;
                return canonicalFilterPath + canonicalPath.Substring(fromFilter.Length);
            }

            throw new ServerPathTranslationException(
                string.Format(
                MigrationToolkitResources.Culture,
                MigrationToolkitResources.VCPathTranslationError_CannotFindFilterString,
                canonicalPath));
        }

        internal static bool IsSubItem(String item, String parent)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            return item.StartsWith(parent, StringComparison.OrdinalIgnoreCase) &&
                   (item.Length == parent.Length || parent[parent.Length - 1] == Separator || item[parent.Length] == Separator);
        }
    }
}
