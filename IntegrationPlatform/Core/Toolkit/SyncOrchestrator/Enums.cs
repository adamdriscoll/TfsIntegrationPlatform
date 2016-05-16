// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public enum PipelineSyncCommand
    {
        DEFAULT = 0,
        START = 1,
        PAUSE = 2,
        RESUME = 3,
        STOP_CURRENT_TRIP = 4,
        START_NEW_TRIP = 5,
        STOP = 6,
        FINISH = 7, // one-shot migration session only
        PAUSE_FOR_CONFLICT = 8,
    }

    public enum PipelineState
    {
        Default = 0,

        // primary states
        Running = 1,
        Paused = 2,
        StoppedSingleTrip = 3,
        Stopped = 4,

        // intermittent states
        Starting = 5,
        Pausing = 6,
        StoppingSingleTrip = 7,
        Stopping = 8,

        // primary state
        PausedByConflict = 9,

        // intermittent state
        PausingForConflict = 10,
    }

    public enum OwnerType
    {
        Session = 0,
        SessionGroup = 1,
    }

    public enum PipelineSyncCommandState
    {
        New = 0,
        Processing = 1,
        Processed = 2,
    }
}
