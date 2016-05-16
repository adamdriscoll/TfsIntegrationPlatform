// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace MigrationTestLibrary
{
    public interface IWITTestCaseAdapter : ITestCaseAdapter
    {
        /// <summary>
        /// WIT test case adapter should implement WorkItemAdded event and invoke listeners
        /// when a new work item gets created. WIT test library keeps track of all work item Ids 
        /// created by WIT test adapter by listening to this event
        /// </summary>
        event WorkItemAddedEventHandler WorkItemAdded;
        
        /// <summary>
        /// Work item ID column name in DB. For instance in TFS "[System.Id]" and "dbid" in CQ 
        /// </summary>
        string WorkItemIDColumnName { get; }

        /// <summary>
        /// Add a work item based on the given arguments
        /// </summary>
        int AddWorkItem(string type, string title, string description);

        /// <summary>
        /// Update a work item based on the action
        /// </summary>
        void UpdateWorkItem(int workItemId, WITChangeAction action);

        /// <summary>
        /// Update a link (Add, Edit or Delete)
        /// </summary>
        void UpdateWorkItemLink(int workItemId, WITLinkChangeAction action);

        /// <summary>
        /// Update attachments (Add, Edit or Delete)
        /// </summary>
        void UpdateAttachment(int workItemId, WITAttachmentChangeAction action);

        /// <summary>
        /// Returns a value of the given field name
        /// </summary>
        string GetFieldValue(int workItemId, string fieldName);

        /// <summary>
        /// Returns a link whose location is same as the given location
        /// </summary>
        WITLink GetHyperLink(int workItemId, string location);

        /// <summary>
        /// Returns the number of attachments
        /// </summary>
        int GetAttachmentCount(int workItemId);
    }
}
