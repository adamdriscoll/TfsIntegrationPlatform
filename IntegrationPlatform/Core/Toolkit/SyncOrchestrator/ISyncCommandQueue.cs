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
    /// Synchronization command queue
    /// </summary>
    internal interface ISyncCommandQueue
    {
        /// <summary>
        /// Add a new sync command to the queue for processing.
        /// </summary>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        /// <param name="newCmd">The new command to be appended to the queue.</param>
        void AddCommand(Guid sessionGroupId, PipelineSyncCommand newCmd);

        /// <summary>
        /// Gets the next active command to be processed.
        /// </summary>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        /// <returns>The next active command; NULL if there is a command being processed or no active command.</returns>
        PipelineSyncCommand? GetNextActiveCommand(Guid sessionGroupId);

        /// <summary>
        /// Marks a command to be processed.
        /// </summary>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        /// <param name="command">The command to be marked as processed.</param>
        void MarkCommandProcessed(Guid sessionGroupId, PipelineSyncCommand command);

        /// <summary>
        /// Mark all commands as processed for a particular session group.
        /// </summary>
        /// <remarks>
        /// This method is called immediately before a session group is started. If the previous session run crashed,
        /// and so leaving some "processing" or "active" commands in the queue, this method cleans them up.
        /// </remarks>
        /// <param name="sessionGroupId">The GUID that's used as the unique Id of the subject session group.</param>
        void ClearUpUnprocessedCommand(Guid sessionGroupId);
    }
}
