// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class SyncStateMachine
    {
        private PipelineState m_currState;
        private SyncStateTransitionAlgorithm m_transitionAlgorithm;
        private OwnerType m_ownerType;
        private Guid m_ownerUniqueId;
        private ISyncStateManager m_syncStateManager;
        private object m_lock = new object();

        public static bool IsIntermittentState(PipelineState state)
        {
            switch (state)
            {
                case PipelineState.Default:
                case PipelineState.Paused:
                case PipelineState.PausedByConflict:
                case PipelineState.Running:                
                case PipelineState.Stopped:
                case PipelineState.StoppedSingleTrip:
                    return false;
                case PipelineState.Pausing:
                case PipelineState.PausingForConflict:
                case PipelineState.Starting:
                case PipelineState.Stopping:
                case PipelineState.StoppingSingleTrip:
                    return true;
                default:
                    Debug.Assert(false, "must never reach here");
                    return false;
            }
        }

        public SyncStateMachine(
            PipelineState currentState,
            SyncStateTransitionAlgorithm transitionAlgorithm,
            OwnerType ownerType,
            Guid ownerUniqueId,
            ISyncStateManager syncStateManager)
        {
            m_currState = currentState;
            TransitionAlgorithm = transitionAlgorithm;
            m_ownerType = ownerType;
            m_ownerUniqueId = ownerUniqueId;
            m_syncStateManager = syncStateManager;
        }

        public PipelineState CurrentState
        {
            get
            {
                lock (m_lock)
                {
                    return m_currState;
                }
            }
        }

        public void Reload()
        {
            PipelineState reloadedState = m_syncStateManager.GetCurrentState(m_ownerType, m_ownerUniqueId);
            lock (m_lock)
            {
                m_currState = reloadedState;
            }
        }

        public void Reset()
        {
            if (m_syncStateManager.Reset(m_ownerType, m_ownerUniqueId))
            {
                m_currState = PipelineState.Default;
            }
        }

        public SyncStateTransitionAlgorithm TransitionAlgorithm
        {
            get 
            { 
                return m_transitionAlgorithm; 
            }
            internal set 
            { 
                m_transitionAlgorithm = value; 
            }
        }

        public Guid OwnerUniqueId
        {
            get 
            { 
                return m_ownerUniqueId; 
            }
        }

        /// <summary>
        /// Try transitting from source State to destination State with the given Command
        /// </summary>
        /// <remarks>Must call CommandTransitFinsihed when this call returns TRUE and any transition specific actions have been taken.</remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool TryTransit(PipelineSyncCommand command)
        {
            lock (m_lock)
            {
                PipelineState newState = PipelineState.Default;
                bool retVal = false;
                switch (m_currState)
                {
                    case PipelineState.Paused:
                        retVal = TransitionAlgorithm.TransitAtPaused(command, out newState);
                        break;
                    case PipelineState.Pausing:
                        retVal = TransitionAlgorithm.TransitAtPausing(command, out newState);
                        break;
                    case PipelineState.PausingForConflict:
                        retVal = TransitionAlgorithm.TransitAtPausingForConflict(command, out newState);
                        break;
                    case PipelineState.PausedByConflict:
                        retVal = TransitionAlgorithm.TransitAtPausedByConflict(command, out newState);
                        break;
                    case PipelineState.Running:
                        retVal = TransitionAlgorithm.TransitAtStarted(command, out newState);
                        break;
                    case PipelineState.Starting:
                        retVal = TransitionAlgorithm.TransitAtStarting(command, out newState);
                        break;
                    case PipelineState.StoppingSingleTrip:
                        retVal = TransitionAlgorithm.TransitAtStoppingSingleTrip(command, out newState);
                        break;
                    case PipelineState.Stopped:
                        retVal = TransitionAlgorithm.TransitAtStopped(command, out newState);
                        break;
                    case PipelineState.StoppedSingleTrip:
                        retVal = TransitionAlgorithm.TransitAtStoppedSingleTrip(command, out newState);
                        break;
                    case PipelineState.Stopping:
                        retVal = TransitionAlgorithm.TransitAtStopping(command, out newState);
                        break;
                    case PipelineState.Default:
                        retVal = TransitionAlgorithm.TransitAtDefault(command, out newState);
                        break;
                    default:
                        retVal = false;
                        break;
                }

                if (retVal)
                {
                    m_currState = newState;
                    m_syncStateManager.SaveCurrentState(m_ownerType, m_ownerUniqueId, m_currState);
                }

                return retVal;
            }
        }

        /// <summary>
        /// Transit from an intermittent state, e.g. Stopping, to a stable state, e.g. Stopped
        /// </summary>
        /// <param name="command"></param>
        public void CommandTransitFinished(PipelineSyncCommand command)
        {
            lock (m_lock)
            {
                switch (command)
                {
                    case PipelineSyncCommand.FINISH:
                    case PipelineSyncCommand.STOP:
                        if (m_currState == PipelineState.Stopping)
                        {
                            m_currState = PipelineState.Stopped;
                            m_syncStateManager.SaveCurrentState(m_ownerType, m_ownerUniqueId, m_currState);
                        }
                        break;
                    case PipelineSyncCommand.PAUSE:
                        if (m_currState == PipelineState.Pausing)
                        {
                            m_currState = PipelineState.Paused;
                            m_syncStateManager.SaveCurrentState(m_ownerType, m_ownerUniqueId, m_currState);
                        }
                        break;
                    case PipelineSyncCommand.PAUSE_FOR_CONFLICT:
                        if (m_currState == PipelineState.PausingForConflict)
                        {
                            m_currState = PipelineState.PausedByConflict;
                            m_syncStateManager.SaveCurrentState(m_ownerType, m_ownerUniqueId, m_currState);
                        }
                        break;
                    case PipelineSyncCommand.RESUME:
                    case PipelineSyncCommand.START:
                    case PipelineSyncCommand.START_NEW_TRIP:
                        if (m_currState == PipelineState.Starting)
                        {
                            m_currState = PipelineState.Running;
                            m_syncStateManager.SaveCurrentState(m_ownerType, m_ownerUniqueId, m_currState);
                        }
                        break;
                    case PipelineSyncCommand.STOP_CURRENT_TRIP:
                        if (m_currState == PipelineState.StoppingSingleTrip)
                        {
                            m_currState = PipelineState.StoppedSingleTrip;
                            m_syncStateManager.SaveCurrentState(m_ownerType, m_ownerUniqueId, m_currState);
                        }
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
