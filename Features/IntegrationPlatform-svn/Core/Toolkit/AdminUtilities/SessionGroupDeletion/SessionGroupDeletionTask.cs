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
using Microsoft.TeamFoundation.Migration.Toolkit.WCFServices;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This utility class encapsulates the logic to delete a session group from the migration DB
    /// </summary>
    /// <remarks>Callers must make sure that it is safe to delete the session group, e.g. it is not
    /// running at the time this taks is invoked.</remarks>
    public class SessionGroupDeletionTask
    {
        Guid m_sessionGroupUniqueId;
        RuntimeEntityModel m_context;

        public SessionGroupDeletionTask(Guid sessionGroupUniqueId)
        {
            m_sessionGroupUniqueId = sessionGroupUniqueId;
            m_context = RuntimeEntityModel.CreateInstance();
        }

        public void DeleteSessionGroup()
        {
            var rtSessionGroup = m_context.RTSessionGroupSet.Where(g => g.GroupUniqueId == m_sessionGroupUniqueId).FirstOrDefault();
            if (null == rtSessionGroup)
            {
                return;
            }

            switch (rtSessionGroup.State)
            {
                case (int)BusinessModelManager.SessionStateEnum.Completed:
                case (int)BusinessModelManager.SessionStateEnum.OneTimeCompleted:
                    StartDeletion(rtSessionGroup);
                    break;
                case (int)BusinessModelManager.SessionStateEnum.MarkedForDeletion:
                    break;
                default:
                    try
                    {
                        var pipeProxy = new MigrationServiceClient();
                        if (!pipeProxy.GetRunningSessionGroups().Contains(m_sessionGroupUniqueId))
                        {
                            StartDeletion(rtSessionGroup);
                        }
                        else
                        {
                            throw new SessionGroupDeletionException(
                                MigrationToolkitResources.ErrorDeletingActiveSessionGroup, m_sessionGroupUniqueId.ToString());
                        }
                    }
                    catch (MigrationServiceEndpointNotFoundException)
                    {
                        StartDeletion(rtSessionGroup);
                    }
                    break;
            }
        }

        private void StartDeletion(RTSessionGroup rtSessionGroup)
        {
            Debug.Assert(null != rtSessionGroup, "SessionGroup is NULL");
            rtSessionGroup.State = (int)BusinessModelManager.SessionStateEnum.MarkedForDeletion;
            m_context.TrySaveChanges();
        }

        public static Dictionary<Guid, string> GetDeletableSessionGroupUniqueIds()
        {
            Dictionary<Guid, string> retVal = new Dictionary<Guid, string>();

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int completedStatus = (int)BusinessModelManager.SessionStateEnum.Completed;
                int oneTimeCompletedStatus = (int)BusinessModelManager.SessionStateEnum.OneTimeCompleted;
                var rtGroups =
                    from g in context.RTSessionGroupSet
                    where (g.State == completedStatus || g.State == oneTimeCompletedStatus)
                    select g;

                foreach (var g in rtGroups)
                {
                    retVal[g.GroupUniqueId] = g.FriendlyName;
                }
            }

            return retVal;
        }
    }
}