// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class SyncStateTransitionAlgorithm
    {
        // to\from              Default     Started     Paused      StoppedSingle   Stopped Starting    Pausing     StoppingSingle  Stopping

        // Default              --          --          --          --              --      --          --          --              --

        // Started              --          RESUME      --          --              --      (internal)  --          --              --
        //                                  START
        //                                  START_NEW

        // Paused               --          --          PAUSE       --              --      --          (internal)  --              --

        // StoppedSingleTrip    --          --          --          --              --      --          --          (internal)      --

        // Stopped              --          --          --          --              FINISH                                          (internal)
        //                                                                          STOP                                         

        // Starting             START       --          RESUME      START_NEW       --      START       --          --              --
        //                      START_NEW                                                   START_NEW

        // Pausing              PAUSE       PAUSE       --          PAUSE           --      PAUSE       PAUSE       PAUSE           --

        // StoppingSingleTrip   STOP_CURR   STOP_CURR   STOP_CURR   STOP_CURR       --      STOP_CURR   STOP_CURR   STOP_CURR       --

        // Stopping             FINISH      FINISH      FINISH      FINISH          --      FINISH      FINISH      FINISH          FINISH
        //                      STOP        STOP        STOP        STOP                    STOP        STOP        STOP            STOP
        //                                                                                                                          STOP_CURR

        public bool TransitAtDefault(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.PAUSE:
                    currentState = PipelineState.Pausing;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    currentState = PipelineState.Starting;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    currentState = PipelineState.Starting;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    currentState = PipelineState.StoppingSingleTrip;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtStarted(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.PAUSE:
                    currentState = PipelineState.Pausing;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.RESUME:
                    currentState = PipelineState.Running;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.START:
                    currentState = PipelineState.Running;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    currentState = PipelineState.Running;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    currentState = PipelineState.StoppingSingleTrip;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtPaused(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    transitSucceeded = true;
                    currentState = PipelineState.Stopping;
                    break;
                case PipelineSyncCommand.PAUSE:
                    transitSucceeded = true;
                    currentState = PipelineState.Paused;
                    break;
                case PipelineSyncCommand.RESUME:
                    currentState = PipelineState.Starting;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.START:
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    break;
                case PipelineSyncCommand.STOP:
                    transitSucceeded = true;
                    currentState = PipelineState.Stopping;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    transitSucceeded = true;
                    currentState = PipelineState.StoppingSingleTrip;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtStoppedSingleTrip(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    transitSucceeded = true;
                    currentState = PipelineState.Stopping;
                    break;
                case PipelineSyncCommand.PAUSE:
                    transitSucceeded = true;
                    currentState = PipelineState.Pausing;
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    currentState = PipelineState.Starting;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP:
                    transitSucceeded = true;
                    currentState = PipelineState.Stopping;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    transitSucceeded = true;
                    currentState = PipelineState.StoppedSingleTrip;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtStopped(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    transitSucceeded = true;
                    currentState = PipelineState.Stopped;
                    break;
                case PipelineSyncCommand.PAUSE:
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    break;
                case PipelineSyncCommand.STOP:
                    transitSucceeded = true;
                    currentState = PipelineState.Stopped;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtStarting(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.PAUSE:
                    currentState = PipelineState.Pausing;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    currentState = PipelineState.Starting;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    currentState = PipelineState.Starting;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    currentState = PipelineState.StoppingSingleTrip;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }
        
        public virtual bool TransitAtPausing(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.PAUSE:
                    currentState = PipelineState.Pausing;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    break;
                case PipelineSyncCommand.STOP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    currentState = PipelineState.StoppingSingleTrip;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtStoppingSingleTrip(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.PAUSE:
                    currentState = PipelineState.Pausing;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    break;
                case PipelineSyncCommand.STOP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    currentState = PipelineState.StoppingSingleTrip;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }

        public virtual bool TransitAtStopping(PipelineSyncCommand command, out PipelineState currentState)
        {
            bool transitSucceeded = false;
            currentState = PipelineState.Default;
            switch (command)
            {
                case PipelineSyncCommand.FINISH:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.PAUSE:
                    break;
                case PipelineSyncCommand.RESUME:
                    break;
                case PipelineSyncCommand.START:
                    break;
                case PipelineSyncCommand.START_NEW_TRIP:
                    break;
                case PipelineSyncCommand.STOP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.STOP_CURRENT_TRIP:
                    currentState = PipelineState.Stopping;
                    transitSucceeded = true;
                    break;
                case PipelineSyncCommand.DEFAULT:
                default:
                    break;
            }
            return transitSucceeded;
        }
    }
}


