// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{
    /// <summary>
    /// FiltersElement class
    /// </summary>
    public partial class FiltersElement
    {
        /// <summary>
        /// Get the filter items specific to a migration source.
        /// </summary>
        Dictionary<Guid, List<FilterItem>> m_filters = new Dictionary<Guid, List<FilterItem>>();
        [XmlIgnore]
        public ReadOnlyCollection<FilterItem> this[Guid migrationSourceUniqueId]
        {
            get
            {
                if (!m_filters.ContainsKey(migrationSourceUniqueId))
                {
                    PartitionFilterPairs(migrationSourceUniqueId);
                }
                return m_filters[migrationSourceUniqueId].AsReadOnly();
            }
        }

        private void PartitionFilterPairs(Guid migrationSourceUniqueId)
        {
            List<FilterItem> filterItems = new List<FilterItem>();
            foreach (FilterPair filterPair in this.FilterPair)
            {
                foreach (FilterItem filterItem in filterPair.FilterItem)
                {
                    if (migrationSourceUniqueId.Equals(new Guid(filterItem.MigrationSourceUniqueId)))
                    {
                        if (!filterItems.Contains(filterItem))
                        {
                            filterItems.Add(filterItem);
                        }
                    }
                }
            }
            m_filters.Add(migrationSourceUniqueId, filterItems);
        }
    }
}
