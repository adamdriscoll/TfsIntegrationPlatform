// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// An UndoableGroup that groups temporally adjacent changes to the same property of the same object and then collapses those changes into a single change.
    /// </summary>
    public class UndoablePropertyChangeGroup : UndoableExplicitGroup
    {
        #region Fields
        private readonly Dictionary<int, UndoablePropertyChange> rootUndoablePropertyChanges;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UndoablePropertyChangeGroup"/> class.
        /// </summary>
        public UndoablePropertyChangeGroup ()
        {
            this.rootUndoablePropertyChanges = new Dictionary<int, UndoablePropertyChange> ();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Determines whether this UndoableExplicitGroup can contain the specified IUndoable directly.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containability.</param>
        /// <returns>
        /// 	<c>true</c> if the specified IUndoable can be contained by this UndoableGroup, <c>false</c> otherwise.
        /// </returns>
        protected override bool CanContainUndoable (IUndoable undoable)
        {
            if (!base.CanContainUndoable (undoable) || !(undoable is UndoablePropertyChange))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds the specified IUndoable to this UndoableGroup.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this UndoableGroup.</param>
        protected override void AddUndoable (IUndoable undoable)
        {
            UndoablePropertyChange currentUndoablePropertyChange = (UndoablePropertyChange)undoable;
            int hashCode = this.GetHashCode (currentUndoablePropertyChange);
            this.Reset ();

            UndoablePropertyChange rootUndoablePropertyChange;
            if (!this.rootUndoablePropertyChanges.TryGetValue (hashCode, out rootUndoablePropertyChange))
            {
                rootUndoablePropertyChange = currentUndoablePropertyChange;
                this.rootUndoablePropertyChanges.Add (hashCode, currentUndoablePropertyChange);
            }

            this.rootUndoablePropertyChanges[hashCode] = new UndoablePropertyChange (
                rootUndoablePropertyChange.ModelObject,
                rootUndoablePropertyChange.PropertyDescriptor.Name,
                rootUndoablePropertyChange.OldValue,
                currentUndoablePropertyChange.NewValue);

            foreach (UndoablePropertyChange undoablePropertyChange in this.rootUndoablePropertyChanges.Values)
            {
                base.AddUndoable (undoablePropertyChange);
            }
        }
        #endregion

        #region Private Methods
        private int GetHashCode (UndoablePropertyChange undoablePropertyChange)
        {
            unchecked
            {
                return undoablePropertyChange.ModelObject.GetHashCode () + undoablePropertyChange.PropertyDescriptor.Name.GetHashCode ();
            }
        }
        #endregion
    }
}
