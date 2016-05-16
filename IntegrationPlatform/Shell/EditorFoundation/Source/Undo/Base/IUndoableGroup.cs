// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Provides the interface for groups of undoables.
    /// </summary>
    public interface IUndoableGroup : IUndoable
    {
        /// <summary>
        /// Gets the number of IUndoables contained in this UndoableGroup.
        /// </summary>
        int UndoableCount { get; }

        /// <summary>
        /// Clears all Undoables contained by this UndoableGroup.
        /// </summary>
        void Reset ();
    }
}
