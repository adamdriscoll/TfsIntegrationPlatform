// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Defines the public interface of an Undo Manager.
    /// </summary>
    public interface IUndoManager : IUndoableGroup
    {
        /// <summary>
        /// Occurs when the CanUndo property changes.
        /// </summary>
        event EventHandler CanUndoChanged;

        /// <summary>
        /// Occurs when the CanRedo property changes.
        /// </summary>
        event EventHandler CanRedoChanged;

        /// <summary>
        /// Occurs before an undo operation.
        /// </summary>
        event EventHandler BeforeUndo;

        /// <summary>
        /// Occurs after an undo operation.
        /// </summary>
        event EventHandler AfterUndo;

        /// <summary>
        /// Occurs before a redo operation.
        /// </summary>
        event EventHandler BeforeRedo;

        /// <summary>
        /// Occurs after a redo operation.
        /// </summary>
        event EventHandler AfterRedo;
        
        /// <summary>
        /// Determines whether the UndoManager can perform an Undo operation.
        /// </summary>
        /// <remarks>
        /// The index of the current IUndoable on the undo stack determines
        /// the value of the CanUndo. If the current IUndoable is at the bottom
        /// of the stack, then there is nothing to undo.
        /// </remarks>
        bool CanUndo { get; }

        /// <summary>
        ///  Determines whether the UndoManager can perform a Redo operation.
        /// </summary>
        /// <remarks>
        /// The index of the current IUndoable on the undo stack determines
        /// the value of the CanRedo. If the current IUndoable is at the top
        /// of the stack, then there is nothing to redo.
        /// </remarks>
        bool CanRedo { get; }

        /// <summary>
        /// Gets a unique identifier for the current undoable.
        /// </summary>
        int CurrentUndoableKey { get; }
    }
}
