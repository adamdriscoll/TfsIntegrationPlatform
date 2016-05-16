// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Data.EntityModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// ISyncCommandQueue and ISyncStateManager implementation that persists the sync orchestration information to 
    /// migration database.
    /// </summary>
    public class SqlSyncStateManager : ISyncCommandQueue, ISyncStateManager
    {
        private static SqlSyncStateManager m_instance;

        /// <summary>
        /// Gets a singleton instance of this class.
        /// </summary>
        /// <returns></returns>
        public static SqlSyncStateManager GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = new SqlSyncStateManager();
            }
            return m_instance;
        }

        private SqlSyncStateManager()
        { }

        #region ISyncStateManager Members

        /// <summary>
        /// Gets the current state of the state machine.
        /// </summary>
        /// <param name="ownerType">The type, session or session group, that this state machine represents.</param>
        /// <param name="ownerUniqueId">The GUID used as the unique Id for the subject session or session group.</param>
        /// <returns>The current state of the subject session or session group.</returns>
        public PipelineState GetCurrentState(OwnerType ownerType, Guid ownerUniqueId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                switch (ownerType)
                {
                    case OwnerType.Session:
                        var sessionQuery = context.RTSessionSet.Where(s => s.SessionUniqueId.Equals(ownerUniqueId));
                        RTSession session = sessionQuery.FirstOrDefault();

                        if (session != null && session.OrchestrationStatus.HasValue)
                        {
                            return (PipelineState)session.OrchestrationStatus.Value;
                        }
                        else
                        {
                            return PipelineState.Default;
                        }
                    case OwnerType.SessionGroup:
                        var sessionGroupQuery = context.RTSessionGroupSet.Where(sg => sg.GroupUniqueId.Equals(ownerUniqueId));
                        RTSessionGroup sessionGroup = sessionGroupQuery.FirstOrDefault();
                        
                        if (sessionGroup != null && sessionGroup.OrchestrationStatus.HasValue)
                        {
                            return (PipelineState)sessionGroupQuery.First().OrchestrationStatus.Value;
                        }
                        else
                        {
                            return PipelineState.Default;
                        }
                    default:
                        Debug.Assert(false, "Invalid OwnerType");
                        throw new InvalidOperationException("Invalid OwnerType");
                }
            }
        }

        /// <summary>
        /// Saves the current state of the state machine.
        /// </summary>
        /// <param name="ownerType">The type, session or session group, that this state machine represents.</param>
        /// <param name="ownerUniqueId">The GUID used as the unique Id for the subject session or session group.</param>
        /// <param name="currentState">The current state of the subject session or session group.</param>
        public void SaveCurrentState(OwnerType ownerType, Guid ownerUniqueId, PipelineState currentState)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                switch (ownerType)
                {
                    case OwnerType.Session:
                        var sessionQuery = context.RTSessionSet.Where(s => s.SessionUniqueId.Equals(ownerUniqueId));
                        Debug.Assert(sessionQuery.Count() == 1, "sessionQuery.Count() != 1");
                        if (sessionQuery.Count() != 1)
                        {
                            return;
                        }
                        sessionQuery.First().OrchestrationStatus = (int)currentState;
                        context.SaveChanges();
                        break;
                    case OwnerType.SessionGroup:
                        var sessionGroupQuery = context.RTSessionGroupSet.Where(sg => sg.GroupUniqueId.Equals(ownerUniqueId));
                        Debug.Assert(sessionGroupQuery.Count() == 1, "sessionGroupQuery.Count() != 1");
                        if (sessionGroupQuery.Count() != 1)
                        {
                            return;
                        }
                        sessionGroupQuery.First().OrchestrationStatus = (int)currentState;
                        context.SaveChanges();
                        break;
                    default:
                        Debug.Assert(false, "Invalid OwnerType");
                        return;
                }
            }
        }

        /// <summary>
        /// Resets the state machine to "Default" state.
        /// </summary>
        /// <returns></returns>
        public bool Reset(OwnerType ownerType, Guid ownerUniqueId)
        {
            PipelineState defaultState = PipelineState.Default;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                switch (ownerType)
                {
                    case OwnerType.Session:
                        var sessionQuery = context.RTSessionSet.Where(s => s.SessionUniqueId.Equals(ownerUniqueId));
                        Debug.Assert(sessionQuery.Count() == 1, "sessionQuery.Count() != 1");
                        if (sessionQuery.Count() != 1)
                        {
                            return false;
                        }
                        sessionQuery.First().OrchestrationStatus = (int)defaultState;
                        context.SaveChanges();
                        break;
                    case OwnerType.SessionGroup:
                        var sessionGroupQuery = context.RTSessionGroupSet.Where(sg => sg.GroupUniqueId.Equals(ownerUniqueId));
                        Debug.Assert(sessionGroupQuery.Count() == 1, "sessionGroupQuery.Count() != 1");
                        if (sessionGroupQuery.Count() != 1)
                        {
                            return false;
                        }
                        sessionGroupQuery.First().OrchestrationStatus = (int)defaultState;
                        context.SaveChanges();
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the state of the state machines of the session group and its child sessions.
        /// </summary>
        /// <remarks>
        /// Call this method only at intialization time.
        /// </remarks>
        /// <param name="sessionGroupUniqueId"></param>
        /// <returns></returns>
        public bool TryResetSessionGroupStates(Guid sessionGroupUniqueId)
        {
            int defaultStateValue = (int)PipelineState.Default;

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionGroupQuery = context.RTSessionGroupSet.Where(sg => sg.GroupUniqueId.Equals(sessionGroupUniqueId));
                if (sessionGroupQuery.Count() != 1)
                {
                    return false;
                }
                // reset session group status
                var rtSessionGroup = sessionGroupQuery.First();
                rtSessionGroup.OrchestrationStatus = defaultStateValue;

                // reset child session status
                rtSessionGroup.Sessions.Load();
                foreach (var rtSession in rtSessionGroup.Sessions)
                {
                    rtSession.OrchestrationStatus = defaultStateValue;
                }

                context.SaveChanges();
                return true;
            }
        }

        #endregion

        #region ISyncCommandQueue Members

        /// <summary>
        /// Add a new sync command to the queue for processing.
        /// </summary>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        /// <param name="newCmd">The new command to be appended to the queue.</param>
        public void AddCommand(Guid sessionGroupId, PipelineSyncCommand newCmd)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var sessionGroupQuery =
                    from sg in context.RTSessionGroupSet
                    where sg.GroupUniqueId.Equals(sessionGroupId)
                    select sg;
                Debug.Assert(sessionGroupQuery.Count() == 1, "sessionGroupQuery.Count() != 1");
                if (sessionGroupQuery.Count() != 1)
                {
                    // [teyang] TODO add exception desc
                    throw new InvalidOperationException(); 
                }
                var rtSessionGroup = sessionGroupQuery.First();
                RTOrchestrationCommand command = RTOrchestrationCommand.CreateRTOrchestrationCommand(
                                                    0, // identity column id is auto-generated
                                                    (int)newCmd, 
                                                    (int)PipelineSyncCommandState.New);
                command.SessionGroup = rtSessionGroup;
                context.AddToRTOrchestrationCommandSet(command);
                context.TrySaveChanges();
            }
        }

        /// <summary>
        /// Gets the next active command to be processed.
        /// </summary>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        /// <returns>The next active command; NULL if there is a command being processed or no active command.</returns>
        public PipelineSyncCommand? GetNextActiveCommand(Guid sessionGroupId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int newStateValue = (int)PipelineSyncCommandState.New;
                int processingStateValue = (int)PipelineSyncCommandState.Processing;

                var cmdQuery = from c in context.RTOrchestrationCommandSet
                               where (c.Status == newStateValue || c.Status == processingStateValue)
                                  && c.SessionGroup.GroupUniqueId.Equals(sessionGroupId)
                               orderby c.Id
                               select c;

                if (cmdQuery.Count() == 0 ||
                    cmdQuery.First().Status == processingStateValue)
                {
                    return null;
                }

                RTOrchestrationCommand nextCmd = cmdQuery.First();
                nextCmd.Status = (int)PipelineSyncCommandState.Processing;

                context.TrySaveChanges();

                return (PipelineSyncCommand)nextCmd.Command;
            }
        }

        /// <summary>
        /// Marks a command to be processed.
        /// </summary>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        /// <param name="command">The command to be marked as processed.</param>
        public void MarkCommandProcessed(Guid sessionGroupId, PipelineSyncCommand command)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int commandValue = (int)command;
                int processingStateValue = (int)PipelineSyncCommandState.Processing;
                var cmdQuery = from c in context.RTOrchestrationCommandSet
                               where c.Command == commandValue
                                  && c.Status == processingStateValue
                                  && c.SessionGroup.GroupUniqueId.Equals(sessionGroupId)
                               select c;

                if (cmdQuery.Count() == 0)
                {
                    return;
                }

                foreach (var cmd in cmdQuery)
                {
                    cmd.Status = (int)PipelineSyncCommandState.Processed;
                }

                context.TrySaveChanges();
            }
        }

        /// <summary>
        /// Mark all commands as processed for a particular session group.
        /// </summary>
        /// <remarks>
        /// This method is called immediately before a session group is started. If the previous session run crashed,
        /// leaving some "processing" or "active" commands in the queue, this method is expected to clean them up.
        /// </remarks>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        public void ClearUpUnprocessedCommand(Guid sessionGroupId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                int newStateValue = (int)PipelineSyncCommandState.New;
                int processingStateValue = (int)PipelineSyncCommandState.Processing;

                var cmdQuery = from c in context.RTOrchestrationCommandSet
                               where (c.Status == newStateValue || c.Status == processingStateValue)
                                  && c.SessionGroup.GroupUniqueId.Equals(sessionGroupId)
                               orderby c.Id
                               select c;

                if (cmdQuery.Count() == 0)
                {
                    return;
                }

                foreach (var cmd in cmdQuery)
                {
                    cmd.Status = (int)PipelineSyncCommandState.Processed;
                }

                context.TrySaveChanges();
            }
        }

        #endregion
    }
}
