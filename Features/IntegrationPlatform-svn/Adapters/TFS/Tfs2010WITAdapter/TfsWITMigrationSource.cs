// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)


namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// TfsWITMigrationSource represents a "Migration Source" in the session configuration.
    /// </summary>
    public class TfsWITMigrationSource
    {
        internal TfsWITMigrationSource(string uniqueId, TfsMigrationWorkItemStore workItemStore)
        {
            m_uniqueId = uniqueId;
            m_workItemStore = workItemStore;
        }

        public TfsMigrationWorkItemStore WorkItemStore
        {
            get 
            { 
                return m_workItemStore; 
            }
        }

        /// <summary>
        /// Get the unique id of the migration source
        /// </summary>
        internal string UniqueId
        {
            get 
            { 
                return m_uniqueId; 
            }
        }

        private string m_uniqueId;
        private TfsMigrationWorkItemStore m_workItemStore;
    }
}
