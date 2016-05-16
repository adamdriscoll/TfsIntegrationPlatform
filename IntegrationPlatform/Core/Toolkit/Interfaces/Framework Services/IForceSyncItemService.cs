// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The interface to the Toolkit's ForceSyncItemService
    /// </summary>
    internal interface IForceSyncItemService
    {
        /// <summary>
        /// The Guid of the session to which this ForceSyncItemService applies
        /// </summary>
        Guid SessionId { get; set; }
        
        /// <summary>
        /// The Guid of the migration source to which this ForceSyncItemService applies
        /// </summary>
        Guid MigrationSourceid { get; set; }

        /// <summary>
        /// Return the set of items to force sync
        /// </summary>
        IEnumerable<string> GetItemsForForceSync();

        /// <summary>
        /// Update the status of the current set of force sync items for the context
        /// </summary>
        void MarkCurrentItemsProcessed();
    }
}
