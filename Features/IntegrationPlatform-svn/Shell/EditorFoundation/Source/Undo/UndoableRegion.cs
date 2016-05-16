// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Allows all changes withing a region to be atomically grouped.
    /// </summary>
    /// <example>
    /// The following code example demonstrates creating an UndoableRegion.
    /// <code>
    /// using (new UndoableRegion (this.UndoManager))
    /// {
    ///     // Make a change to the Model
    ///     // Make another change to the Model
    ///     // Make one more change to the Model
    /// }
    /// </code>
    /// All three changes will be grouped together. A single undo/redo operation on
    /// the UndoManager will undo/redo all three changes.
    /// </example>
    public class UndoableRegion : IDisposable
    {
        #region Fields
        private UndoableExplicitGroup undoableExplicitGroup;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ControllerBase class.
        /// </summary>
        /// <param name="parentGroup">
        /// The UndoableGroup to which this UndoableRegion should be added. 
        /// Usually, this will be the UndoManager.
        /// </param>
        public UndoableRegion (UndoableGroup parentGroup)
        {
            this.undoableExplicitGroup = new UndoableExplicitGroup ();
            parentGroup.Add (this.undoableExplicitGroup);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Closes the UndoGroup.
        /// </summary>
        public void Dispose ()
        {
            this.undoableExplicitGroup.Close ();
        }
        #endregion
    }
}
