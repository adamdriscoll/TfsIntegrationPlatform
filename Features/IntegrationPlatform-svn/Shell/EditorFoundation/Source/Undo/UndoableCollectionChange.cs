// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// Provides a base class for IUndoables related to changes in a collection.
    /// </summary>
    public abstract class UndoableCollectionChange : IUndoable
    {
        #region Fields
        /// <summary>
        /// The collection being changed.
        /// </summary>
        protected INotifyingCollection collection;

        /// <summary>
        /// The value of the changed item.
        /// </summary>
        protected object value;

        /// <summary>
        /// The index of the changed item.
        /// </summary>
        protected int index;
        #endregion

        #region Constructors & Destructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UndoableCollectionChange"/> class.
        /// </summary>
        /// <param name="collection">The collection being changed.</param>
        /// <param name="value">The value of the changed item.</param>
        /// <param name="index">The index of the changed item.</param>
        public UndoableCollectionChange (INotifyingCollection collection, object value, int index)
        {
            this.collection = collection;
            this.value = value;
            this.index = index;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Undo the operation that this IUndoable represents.
        /// </summary>
        public abstract void Undo ();

        /// <summary>
        /// Redo the operation that this IUndoable represents.
        /// </summary>
        public abstract void Redo ();

        /// <summary>
        /// Determines whether this IUndoable can contain the specified IUndoable.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containment validity.</param>
        /// <returns>
        /// 	<c>true</c> if the specified IUndoable can be contained by the current IUndoable, <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// In many cases, it makes sense to group IUndoables together. For example,
        /// sometimes Models keep track of whether the value of a property has been
        /// explicitly set by a user. When a user first explicitly sets the value of
        /// a property, it would make sense to group together the property change
        /// and the property specified change.
        /// </remarks>
        public bool CanContain (IUndoable undoable)
        {
            return false;
        }

        /// <summary>
        /// Adds the specified IUndoable to this IUndoable.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this IUndoable.</param>
        /// <returns>
        /// 	<c>true</c> if the specified IUndoable was successfully added to this IUndoable, <c>false</c> otherwise.
        /// </returns>
        public bool Add (IUndoable undoable)
        {
            return false;
        }
        #endregion
    }
}
