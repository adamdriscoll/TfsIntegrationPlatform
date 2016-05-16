// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Provides the interface for all undoable actions.
    /// </summary>
    public interface IUndoable
    {
        /// <summary>
        /// Undo the operation that this IUndoable represents.
        /// </summary>
        void Undo ();

        /// <summary>
        /// Redo the operation that this IUndoable represents.
        /// </summary>
        void Redo ();

        /// <summary>
        /// Determines whether this IUndoable can contain the specified IUndoable.
        /// </summary>
        /// <remarks>
        /// In many cases, it makes sense to group IUndoables together. For example,
        /// sometimes Models keep track of whether the value of a property has been
        /// explicitly set by a user. When a user first explicitly sets the value of
        /// a property, it would make sense to group together the property change 
        /// and the property specified change.
        /// </remarks>
        /// <param name="undoable">The IUndoable to check for containment validity.</param>
        /// <returns><c>true</c> if the specified IUndoable can be contained by the current IUndoable, <c>false</c> otherwise.</returns>
        bool CanContain (IUndoable undoable);

        /// <summary>
        /// Adds the specified IUndoable to this IUndoable.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this IUndoable.</param>
        /// <returns><c>true</c> if the specified IUndoable was successfully added to this IUndoable, <c>false</c> otherwise.</returns>
        bool Add (IUndoable undoable);
    }
}
