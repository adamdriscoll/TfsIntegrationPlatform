// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// 
// Description: This class buils a initial cache for the existing work
//      items in the currituck database.
//      Thereafter, as soon as some work item is migrated, the 
//      corresponding entry is added in the cache
//    Basic assumption is that no other tool will add / edit the project
//    while PSConverter is up and running!
// 

using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using NVRelation = Microsoft.TeamFoundation.Converters.WorkItemTracking.WorkItemNameValueRelation;
/*
using System.IO;
using System.Text;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using Microsoft.TeamFoundation.Converters.Reporting;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.PS;
using ProductStudio;
*/


namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    /// <summary>
    /// Definition of VSTS work item in local cache
    /// </summary>
    internal class WorkItemInCache
    {
        internal int VSTSWorkItemId;
        internal string sourceItemId;
        internal bool IsMigrated; // whether the work item migrated completely
    }
}
