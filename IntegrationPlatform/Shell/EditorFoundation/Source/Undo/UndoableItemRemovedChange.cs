// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// An undoable corresponding to the removal of an item from a collection.
    /// </summary>
    public class UndoableItemRemovedChange : UndoableCollectionChange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UndoableItemRemovedChange"/> class.
        /// </summary>
        /// <param name="collection">The collection being changed.</param>
        /// <param name="value">The value of the changed item.</param>
        /// <param name="index">The index of the changed item.</param>
        public UndoableItemRemovedChange (INotifyingCollection collection, object value, int index)
            : base (collection, value, index)
        {
        }

        /// <summary>
        /// Undo the operation that this IUndoable represents.
        /// </summary>
        public override void Undo ()
        {
            this.collection.Insert (index, value);
        }

        /// <summary>
        /// Redo the operation that this IUndoable represents.
        /// </summary>
        public override void Redo ()
        {
            this.collection.RemoveAt (this.index);
        }
    }
}
