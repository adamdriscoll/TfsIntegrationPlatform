// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    internal class BasicDeltaTableMaintenanceService : IDeltaTableMaintenanceService
    {
        private Dictionary<Guid, ServiceContainer> ServiceContainers { get; set; }

        public BasicDeltaTableMaintenanceService(Dictionary<Guid, ServiceContainer> serviceContainers)
        {
            ServiceContainers = serviceContainers;
        }

        public virtual void BatchMarkDeltaTableEntriesAsDeltaCompleted(Guid sourceId)
        {
            Debug.Assert(null != ServiceContainers);
            Debug.Assert(ServiceContainers.ContainsKey(sourceId));

            var changeGroupService = ServiceContainers[sourceId].GetService(typeof(ChangeGroupService)) as ChangeGroupService;
            Debug.Assert(null != changeGroupService);

            changeGroupService.BatchMarkDeltaTableEntriesAsDeltaCompleted();
        }
    }
}