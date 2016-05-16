// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This class provides a mechanism to expose the status of a session group.
    /// </summary>
    public class SessionGroupStatus
    {
        Guid m_sessionGroupId;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sessionGroupId">The Unique Id of the session group to be attached to the new instance.</param>
        public SessionGroupStatus(Guid sessionGroupId)
        {
            m_sessionGroupId = sessionGroupId;
        }

        /// <summary>
        /// Gets the current status of the attached session group.
        /// </summary>
        public PipelineState CurrentStatus
        {
            get
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    try
                    {
                        // first, ask the wcf service if it has loaded this session group
                        MigrationServiceClient client = new MigrationServiceClient();
                        var runningGroupIds = client.GetRunningSessionGroups();
                        if (!runningGroupIds.Contains(m_sessionGroupId))
                        {
                            return WasRunBefore ? PipelineState.Stopped : PipelineState.Default;
                        }
                    }
                    catch (MigrationServiceEndpointNotFoundException)
                    {
                        return PipelineState.Stopped;
                    }

                    // next, get the group's current sync orchestration state
                    var sessionGroupQuery = context.RTSessionGroupSet.Where(g => g.GroupUniqueId.Equals(m_sessionGroupId));
                    int sessionGroupCount = sessionGroupQuery.Count();
                    if (sessionGroupCount == 0)
                    {
                        return PipelineState.Default; // we don't know about this group
                    }
                    else
                    {
                        Debug.Assert(sessionGroupCount == 1, "Multiple session group with same unique Id exist");

                        if (sessionGroupQuery.First().OrchestrationStatus.HasValue)
                        {
                            return (PipelineState)sessionGroupQuery.First().OrchestrationStatus.Value;
                        }
                        else
                        {
                            return PipelineState.Default;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether the session group is loaded in a sych orchestrator for execution.
        /// </summary>
        public bool IsLoadedForExecution
        {
            get
            {
                switch (CurrentStatus)
                {
                    case PipelineState.Default:
                    case PipelineState.Stopped:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Gets whether the session group has been run.
        /// </summary>
        public bool WasRunBefore
        {
            get
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    var sessionGroupQuery = context.RTSessionGroupSet.Where(g => g.GroupUniqueId.Equals(m_sessionGroupId));
                    if (sessionGroupQuery.Count() == 0)
                    {
                        return false; // we don't know about this group
                    }

                    int sessionGroupStorageId = sessionGroupQuery.First().Id;
                    var sessionGroupRunQuery = context.RTSessionGroupRunSet.Where(
                        gr => gr.Config.SessionGroup.Id == sessionGroupStorageId);

                    return sessionGroupRunQuery.Count() > 0;
                }
            }
        }
    }
}
