// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// An UndoableGroup that must be explicitly closed.
    /// </summary>
    public class UndoableExplicitGroup : UndoableGroup
    {
        #region Fields
        private bool isClosed;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the UndoableExplicitGroup class.
        /// </summary>
        public UndoableExplicitGroup ()
            : base ()
        {
            this.isClosed = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Closes the group (i.e. no more IUndoables can be added to the UndoableGroup)
        /// </summary>
        public void Close ()
        {
            this.isClosed = true;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Determines whether this UndoableExplicitGroup can contain the specified IUndoable directly.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containability.</param>
        /// <returns><c>true</c> if the specified IUndoable can be contained by this UndoableGroup, <c>false</c> otherwise.</returns>
        protected override bool CanContainUndoable (IUndoable undoable)
        {
            return !this.isClosed;
        }
        #endregion
    }
}
