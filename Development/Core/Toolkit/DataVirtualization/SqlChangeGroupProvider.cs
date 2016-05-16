// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class SqlChangeGroupProvider : IItemsProvider<IMigrationAction>
    {
        RTChangeGroup m_runtimeChangeGroup;
        SqlChangeGroupManager m_sqlChangeGroupManager;
        
        public SqlChangeGroupProvider(
            RTChangeGroup runtimeChangeGroup, 
            SqlChangeGroupManager parentSqlChangeGroupManager)
        {
            if (null == runtimeChangeGroup)
            {
                throw new ArgumentNullException("runtimeChangeGroup");
            }

            m_runtimeChangeGroup = runtimeChangeGroup; 
            m_sqlChangeGroupManager = parentSqlChangeGroupManager;
        }

        public int Count()
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var query =
                    (from ca in context.RTChangeActionSet
                     group ca by ca.ChangeGroupId into g
                     where g.Key == m_runtimeChangeGroup.Id
                     select new
                     {
                         Count = g.Count()
                     });

                // SQL optimization
                // The foreach pattern here replaces code that issued multiple SQL queries:
                //      return query.Count() == 0 ? 0 : query.First().Count;

                foreach (var groupCount in query)
                {
                    return groupCount.Count;
                }
                return 0;
            }
        }

        public IList<IMigrationAction> LoadPage(int startIndex, int count)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var pagedActionQuery = (from ca in context.RTChangeActionSet
                                        orderby ca.ChangeActionId descending
                                        where ca.ChangeGroupId == m_runtimeChangeGroup.Id
                                        orderby ca.ChangeActionId
                                        select ca).Skip(startIndex).Take(count);

                return m_sqlChangeGroupManager.LoadPagedActions(pagedActionQuery);
            }
        }

        public void StorePage(int startIndex, IList<IMigrationAction> page)
        {
            // Verify that there is something to save and the first change action in the 
            // page has not been persisted.  This implementation relies on page level granularity
            // in save/load.
            if (page != null && page.Count != 0)
            {
                SqlMigrationAction sqlMigrationAction = (SqlMigrationAction) page[0];
                m_sqlChangeGroupManager.BatchSaveGroupedChangeActions(page);
            }
        }
    }
}
