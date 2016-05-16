// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class RelatedArtifactStoreBase : IDisposable
    {
        protected RuntimeEntityModel m_context = RuntimeEntityModel.CreateInstance();
        protected RTMigrationSource m_rtMigrationSource = null;

        public RelatedArtifactStoreBase(Guid migrationSourceId)
        {
            MigrationSourceId = migrationSourceId;
        }

        protected Guid MigrationSourceId
        {
            get;
            private set;
        }

        #region IDisposable
        public virtual void Dispose()
        {
            if (null != m_context)
            {
                m_context.Dispose();
            }
        } 
        #endregion

        protected RTMigrationSource RuntimeMigrationSource
        {
            get
            {
                if (m_rtMigrationSource == null)
                {
                    var migrationSourceQuery = m_context.RTMigrationSourceSet.Where(s => s.UniqueId.Equals(MigrationSourceId));
                    Debug.Assert(migrationSourceQuery.Count() == 1,
                    string.Format("Invalid: migrationSourceQuery.Count() == {0}", migrationSourceQuery.Count().ToString()));
                    m_rtMigrationSource = migrationSourceQuery.First();
                }

                return m_rtMigrationSource;
            }
        }

        protected IQueryable<RTRelatedArtifactsRecords> QueryByItem(
            string sourceItemUri)
        {
            return (from link in m_context.RTRelatedArtifactsRecordsSet
                    where link.MigrationSource.Id == RuntimeMigrationSource.Id
                    && link.ItemId.Equals(sourceItemUri)
                    select link);
        }

        protected void MarkRelationshipNoLongerExists(string sourceItemUri, string targetItemUri, string relationship)
        {
            var queryByItem = QueryByItem(sourceItemUri);
            var queryOnRelatedArtifactAndStatus =
                from link in queryByItem
                where link.RelationshipExistsOnServer && link.RelatedArtifactId.Equals(targetItemUri)
                && link.Relationship.Equals(relationship)
                select link;

            if (queryByItem.Count() > 0)
            {
                queryByItem.First().RelationshipExistsOnServer = false;
                queryByItem.First().OtherProperty = 0;
                m_context.TrySaveChanges();
            }
        }
    }
}
