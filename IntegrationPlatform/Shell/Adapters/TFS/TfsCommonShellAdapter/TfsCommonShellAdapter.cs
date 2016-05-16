// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.Properties;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public abstract class TfsCommonShellAdapter : IPlugin
    {
        private static List<IConflictTypeView> s_conflictTypes;

        static TfsCommonShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new GenericConflictType().ReferenceName,
                FriendlyName = Resources.GenericConflictTypeFriendlyName,
                Description = Resources.GenericConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new CyclicLinkReferenceConflictType().ReferenceName,
                FriendlyName = Resources.CyclicLinkReferenceConflictTypeFriendlyName,
                Description = Resources.CyclicLinkReferenceConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ChainOnConflictConflictType().ReferenceName,
                FriendlyName = Resources.ChainOnConflictConflictTypeFriendlyName,
                Description = Resources.ChainOnConflictConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new ChainOnBackloggedItemConflictType().ReferenceName,
                FriendlyName = Resources.ChainOnBackloggedItemConflictTypeFriendlyName,
                Description = Resources.ChainOnBackloggedItemConflictTypeDescription
            });
        }

        #region IPlugin Members

        public virtual void OnContextEnter(object contextInstance)
        {
            // do nothing
        }

        public virtual void OnContextLeave(object contextInstance)
        {
            // do nothing
        }

        public abstract IMigrationSourceView GetMigrationSourceView();

        public virtual IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return s_conflictTypes;
        }

        public abstract ExecuteFilterStringExtension FilterStringExtension { get; }

        protected virtual Dictionary<string, string> GetMigrationSourceProperties(MigrationSource migrationSource)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties["Server URL"] = migrationSource.ServerUrl;
            properties["Team Project"] = migrationSource.SourceIdentifier;

            return properties;
        }

        #endregion
    }
}
