// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Represents a group of IUndoables.
    /// </summary>
    /// <remarks>
    /// This class is not thread safe. It assumes that changes
    /// to the underlying object are only being made on one thread.
    /// </remarks>
    public abstract class UndoableGroup : IUndoableGroup
    {
        #region Fields
        /// <summary>
        /// The list of IUndoables currently contained by this UndoableGroup.
        /// </summary>
        protected readonly List<IUndoable> undoables;

        /// <summary>
        /// The index of the current IUndoable in the list of IUndoables contained by this UndoableGroup.
        /// </summary>
        protected int undoableIndex;
        #endregion

        #region Constructors & Destructors
        /// <summary>
        /// Initializes a new instance of the UndoableGroup class.
        /// </summary>
        public UndoableGroup ()
        {
            this.undoables = new List<IUndoable> ();
            this.undoableIndex = -1;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the number of IUndoables contained in this UndoableGroup.
        /// </summary>
        public int UndoableCount
        {
            get
            {
                return this.undoables.Count;
            }
        }

        /// <summary>
        /// Gets the current IUndoable in the undo stack.
        /// </summary>
        protected virtual IUndoable CurrentUndoable
        {
            get
            {
                if (this.undoableIndex >= 0 && this.undoableIndex < this.undoables.Count)
                {
                    return this.undoables[this.undoableIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the current undoable recursively.
        /// </summary>
        /// <remarks>
        /// If the current undoable is an undoable group, then the child undoable group's
        /// current undoable is retrieved. This behavior is recursive.
        /// </remarks>
        protected IUndoable CurrentUndoableRecursive
        {
            get
            {
                return UndoableGroup.GetCurrentUndoableRecursive (this);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Undo the entire UndoableGroup.
        /// </summary>
        public virtual void Undo ()
        {
            for (int i = this.undoables.Count - 1; i >= 0; i--)
            {
                this.undoables[i].Undo ();
            }
        }

        /// <summary>
        /// Redo the entire UndoableGroup.
        /// </summary>
        public virtual void Redo ()
        {
            foreach (IUndoable undoable in this.undoables)
            {
                undoable.Redo ();
            }
        }

        /// <summary>
        /// Determines whether this UndoableGroup or the CurrentUndoable can contain the specified IUndoable.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containability.</param>
        /// <returns><c>true</c> if the specified IUndoable can be contained by this UndoableGroup, <c>false</c> otherwise.</returns>
        public bool CanContain (IUndoable undoable)
        {
            if (this.CurrentUndoable != null && this.CurrentUndoable.CanContain (undoable))
            {
                return true;
            }
            return this.CanContainUndoable (undoable);
        }

        /// <summary>
        /// Adds the specified IUndoable to this UndoableGroup if it can be contained.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this UndoableGroup.</param>
        /// <returns><c>true</c> if the specified IUndoable was added to this UndoableGroup, <c>false</c> otherwise.</returns>
        public virtual bool Add (IUndoable undoable)
        {
            if (this.CanContain (undoable))
            {
                this.AddUndoable (undoable);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Clears all Undoables contained by this UndoableGroup.
        /// </summary>
        public virtual void Reset ()
        {
            this.undoables.Clear ();
            this.undoableIndex = -1;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Adds the specified IUndoable to this UndoableGroup.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this UndoableGroup.</param>
        protected virtual void AddUndoable (IUndoable undoable)
        {
            // First check if the current undoable can contain the specified undoable
            if (this.CurrentUndoable != null && this.CurrentUndoable.CanContain (undoable))
            {
                this.CurrentUndoable.Add (undoable);
            }
            // If not, add the undoable in the scope of this UndoableGroup
            else
            {
                // If the current undoable is an empty group, replace it
                if (this.CurrentUndoable is UndoableGroup && ((UndoableGroup)this.CurrentUndoable).UndoableCount == 0)
                {
                    this.undoables[this.undoableIndex] = undoable;
                }
                // Otherwise add it as the next undoable
                else
                {
                    this.undoables.Add (undoable);
                    this.undoableIndex++;
                }
            }
        }

        /// <summary>
        /// Determines whether this UndoableGroup can contain the specified IUndoable directly.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containability.</param>
        /// <returns><c>true</c> if the specified IUndoable can be contained by this UndoableGroup, <c>false</c> otherwise.</returns>
        protected abstract bool CanContainUndoable (IUndoable undoable);
        #endregion

        #region Private Methods
        private static IUndoable GetCurrentUndoableRecursive (UndoableGroup undoableGroup)
        {
            IUndoable undoable = undoableGroup.CurrentUndoable;
            UndoableGroup childGroup = undoable as UndoableGroup;
            if (childGroup != null)
            {
                undoable = UndoableGroup.GetCurrentUndoableRecursive (childGroup);
            }

            return undoable;
        }
        #endregion
    }
}
