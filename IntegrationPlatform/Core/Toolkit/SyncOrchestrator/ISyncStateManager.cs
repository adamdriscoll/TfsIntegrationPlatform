// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// ISyncStateManager mains the state transition information for a session group or session.
    /// </summary>
    internal interface ISyncStateManager
    {
        /// <summary>
        /// Gets the current state of the state machine.
        /// </summary>
        /// <param name="ownerType">The type, session or session group, that this state machine represents.</param>
        /// <param name="ownerUniqueId">The GUID used as the unique Id for the subject session or session group.</param>
        /// <returns>The current state of the subject session or session group.</returns>
        PipelineState GetCurrentState(OwnerType ownerType, Guid ownerUniqueId);

        /// <summary>
        /// Saves the current state of the state machine.
        /// </summary>
        /// <param name="ownerType">The type, session or session group, that this state machine represents.</param>
        /// <param name="ownerUniqueId">The GUID used as the unique Id for the subject session or session group.</param>
        /// <param name="currentState">The current state of the subject session or session group.</param>
        void SaveCurrentState(OwnerType ownerType, Guid ownerUniqueId, PipelineState currentState);

        /// <summary>
        /// Resets the state machine to "Default" state.
        /// </summary>
        /// <param name="ownerType">The type, session or session group, that this state machine represents.</param>
        /// <param name="ownerUniqueId">The GUID used as the unique Id for the subject session or session group.</param>
        /// <returns>TRUE if succeeds; FALSE otherwise.</returns>
        bool Reset(OwnerType ownerType, Guid ownerUniqueId);

        /// <summary>
        /// Resets the state of the state machines of the session group and its child sessions.
        /// </summary>
        /// <remarks>
        /// Call this method only at intialization time.
        /// </remarks>
        /// <param name="sessionGroupUniqueId"></param>
        /// <returns></returns>
        bool TryResetSessionGroupStates(Guid sessionGroupUniqueId);
    }
}
