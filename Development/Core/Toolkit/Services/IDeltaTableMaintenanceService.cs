// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    internal interface IDeltaTableMaintenanceService
    {
        void BatchMarkDeltaTableEntriesAsDeltaCompleted(Guid sourceId);
    }
}