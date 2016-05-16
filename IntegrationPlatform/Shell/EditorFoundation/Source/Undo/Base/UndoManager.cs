// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Manages undo/redo functionality.
    /// </summary>
    public abstract class UndoManager : UndoableGroup, IUndoManager
    {
        #region Fields
        private bool changing;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the UndoManager class.
        /// </summary>
        public UndoManager ()
        {
            this.changing = false;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the CanUndo property changes.
        /// </summary>
        public event EventHandler CanUndoChanged;

        /// <summary>
        /// Occurs when the CanRedo property changes.
        /// </summary>
        public event EventHandler CanRedoChanged;

        /// <summary>
        /// Occurs before an undo operation.
        /// </summary>
        public event EventHandler BeforeUndo;

        /// <summary>
        /// Occurs after an undo operation.
        /// </summary>
        public event EventHandler AfterUndo;

        /// <summary>
        /// Occurs before a redo operation.
        /// </summary>
        public event EventHandler BeforeRedo;

        /// <summary>
        /// Occurs after a redo operation.
        /// </summary>
        public event EventHandler AfterRedo;
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether the UndoManager can perform an Undo operation.
        /// </summary>
        /// <remarks>
        /// The index of the current IUndoable on the undo stack determines
        /// the value of the CanUndo. If the current IUndoable is at the bottom
        /// of the stack, then there is nothing to undo.
        /// </remarks>
        public bool CanUndo
        {
            get
            {
                if (this.CurrentUndoable != null)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        ///  Determines whether the UndoManager can perform a Redo operation.
        /// </summary>
        /// <remarks>
        /// The index of the current IUndoable on the undo stack determines
        /// the value of the CanRedo. If the current IUndoable is at the top
        /// of the stack, then there is nothing to redo.
        /// </remarks>
        public bool CanRedo
        {
            get
            {
                if (this.NextUndoable != null)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a unique identifier for the current undoable.
        /// </summary>
        public int CurrentUndoableKey
        {
            get
            {
                IUndoable currentUndoableRecursive = this.CurrentUndoableRecursive;
                return currentUndoableRecursive == null ? 0 : currentUndoableRecursive.GetHashCode ();
            }
        }

        /// <summary>
        /// Determines whether the UndoManager is currently changing
        /// the Model for an Undo or Redo operation.
        /// </summary>
        protected bool Changing
        {
            get
            {
                return this.changing;
            }
        }

        /// <summary>
        /// Gets the next IUndoable on the undo stack. This will return a
        /// non-null value only when Undo operations have been performed
        /// and no new user operations have occurred.
        /// </summary>
        protected IUndoable NextUndoable
        {
            get
            {
                int nextIndex = this.undoableIndex + 1;
                if (nextIndex >= 0 && nextIndex < this.undoables.Count)
                {
                    return this.undoables[nextIndex];
                }
                return null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs an Undo operation on the current IUndoable of the undo stack.
        /// </summary>
        public override void Undo ()
        {
            bool couldUndo = this.CanUndo;
            bool couldRedo = this.CanRedo;

            if (this.CanUndo)
            {
                this.RaiseBeforeUndoEvent ();

                this.changing = true;
                this.CurrentUndoable.Undo ();
                this.undoableIndex--;
                this.changing = false;

                this.RaiseAfterUndoEvent ();

                // Check if CanUndo/CanRedo changed
                this.CheckForUndoRedoChanged (couldUndo, couldRedo);
            }
        }

        /// <summary>
        /// Performs an Redo operation on the next IUndoable of the undo stack.
        /// </summary>
        public override void Redo ()
        {
            bool couldUndo = this.CanUndo;
            bool couldRedo = this.CanRedo;

            if (this.CanRedo)
            {
                this.RaiseBeforeRedoEvent ();

                this.changing = true;
                this.NextUndoable.Redo ();
                this.undoableIndex++;
                this.changing = false;

                this.RaiseAfterRedoEvent ();

                // Check if CanUndo/CanRedo changed
                this.CheckForUndoRedoChanged (couldUndo, couldRedo);
            }
        }

        /// <summary>
        /// Clears all Undoables contained by this UndoableManager.
        /// </summary>
        public override void Reset ()
        {
            bool couldUndo = this.CanUndo;
            bool couldRedo = this.CanRedo;

            base.Reset ();

            this.CheckForUndoRedoChanged (couldUndo, couldRedo);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Adds the specified IUndoable to this UndoManager.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this UndoManager.</param>
        protected override void AddUndoable (IUndoable undoable)
        {
            // Disallow adding new undoables while the UndoManager is performing
            // an Undo or Redo operation. Assume changes to the model during this
            // period are caused by the UndoManager, and ignore those changes.
            if (!this.changing)
            {
                bool couldUndo = this.CanUndo;
                bool couldRedo = this.CanRedo;

                // If the current undoable is not at the top of the undo stack,
                // then clear everything above the current undoable
                while (this.undoableIndex + 1 < this.undoables.Count)
                {
                    this.undoables.RemoveAt (this.undoableIndex + 1);
                }

                // Now add the undoable
                base.AddUndoable (undoable);

                // Check if CanUndo/CanRedo changed
                this.CheckForUndoRedoChanged (couldUndo, couldRedo);
            }
        }

        /// <summary>
        /// Determines whether this UndoableManager can contain the specified IUndoable directly.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containability.</param>
        /// <returns><c>true</c> if the specified IUndoable can be contained by this UndoableGroup, <c>false</c> otherwise.</returns>
        protected override bool CanContainUndoable (IUndoable undoable)
        {
            return true;
        }
        #endregion

        #region Private Methods
        private void RaiseCanUndoChangedEvent ()
        {
            if (this.CanUndoChanged != null)
            {
                this.CanUndoChanged (this, EventArgs.Empty);
            }
        }

        private void RaiseCanRedoChangedEvent ()
        {
            if (this.CanRedoChanged != null)
            {
                this.CanRedoChanged (this, EventArgs.Empty);
            }
        }

        private void RaiseBeforeUndoEvent ()
        {
            if (this.BeforeUndo != null)
            {
                this.BeforeUndo (this, EventArgs.Empty);
            }
        }

        private void RaiseAfterUndoEvent ()
        {
            if (this.AfterUndo != null)
            {
                this.AfterUndo (this, EventArgs.Empty);
            }
        }

        private void RaiseBeforeRedoEvent ()
        {
            if (this.BeforeRedo != null)
            {
                this.BeforeRedo (this, EventArgs.Empty);
            }
        }

        private void RaiseAfterRedoEvent ()
        {
            if (this.AfterRedo != null)
            {
                this.AfterRedo (this, EventArgs.Empty);
            }
        }

        private void CheckForUndoRedoChanged (bool couldUndo, bool couldRedo)
        {
            // Check if CanUndo changed
            if (couldUndo != this.CanUndo)
            {
                this.RaiseCanUndoChangedEvent ();
            }

            // Check if CanRedo changed
            if (couldRedo != this.CanRedo)
            {
                this.RaiseCanRedoChangedEvent ();
            }
        }
        #endregion
    }
}
