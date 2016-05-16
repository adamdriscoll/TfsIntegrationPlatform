// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    public delegate void WorkItemAddedEventHandler(object sender, WorkItemAddedEventArgs e);

    public class WorkItemAddedEventArgs
    {
        /// <summary>
        /// Gets the item id that was added
        /// </summary>
        public int WorkItemId
        {
            get;
            set;
        }

        public WorkItemAddedEventArgs(int workItemId)
        {
            WorkItemId = workItemId;
        }
    }
}
