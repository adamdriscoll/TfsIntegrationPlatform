// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration
{
    internal class MigrationDataCache
    {
        public void TryCleanup(ConfigurationChangeTracker changeTracker)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                switch (changeTracker.ImpactScope)
                {
                    case ConfigurationChangeTracker.ConfigChangeImpactScope.Session:
                        foreach (Guid sessionUniqueId in changeTracker.UpdatedSessionIds)
                        {
                            Trace.TraceInformation(
                                "Deleting cached data for session '{0}'", 
                                sessionUniqueId.ToString());
                            context.DeleteSessionCachedData(sessionUniqueId);
                        }
                        break;
                    case ConfigurationChangeTracker.ConfigChangeImpactScope.SessionGroup:
                        Trace.TraceInformation(
                            "Deleting cached data for session group '{0}'", 
                            changeTracker.UpdatedSessionGroupId.ToString());
                        context.DeleteSessionGroupCachedData(changeTracker.UpdatedSessionGroupId);
                        break;
                    default:
                        Trace.TraceInformation(
                            "ConfigurationChangeTracker did not detect any non-transient changes. No cached data will be deleted for session group '{0}'", 
                            changeTracker.UpdatedSessionGroupId.ToString());
                        break;
                }
            }
        }
    }
}
