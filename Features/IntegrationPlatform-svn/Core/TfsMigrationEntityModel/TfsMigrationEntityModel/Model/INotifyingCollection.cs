// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// Represents a nongeneric collection that raises events when the contents of the collection change.
    /// </summary>
    public interface INotifyingCollection : IList
    {
        #region Events
        /// <summary>
        /// Occurs when an item is added to the collection.
        /// </summary>
        event ItemAddedRemovedEventHandler ItemAdded;

        /// <summary>
        /// Occurs when an item is removed from the collection.
        /// </summary>
        event ItemAddedRemovedEventHandler ItemRemoved;

        /// <summary>
        /// Occurs when an item is replaced in the collection.
        /// </summary>
        event ItemReplacedEventHandler ItemReplaced;
        #endregion
    }

    /// <summary>
    /// Represents a generic collection that raises events when the contents of the collection change.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    public interface INotifyingCollection<T> : IList<T>
    {
        #region Events
        /// <summary>
        /// Occurs when an item is added to the collection.
        /// </summary>
        event ItemAddedEventHandler<T> ItemAdded;

        /// <summary>
        /// Occurs when an item is removed from the collection.
        /// </summary>
        event ItemRemovedEventHandler<T> ItemRemoved;

        /// <summary>
        /// Occurs when an item is replaced in the collection.
        /// </summary>
        event ItemReplacedEventHandler<T> ItemReplaced;
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new membership restriction to the collection.
        /// </summary>
        /// <remarks>
        /// When a restriction verification fails, a CollectionRestrictionException is thrown.
        /// </remarks>
        /// <param name="restriction">The restriction to add to the collection.</param>
        void AddRestriction (CollectionRestriction<T> restriction);
        #endregion
    }

    /// <summary>
    /// Represents a generic collection that raises events when the contents of the collection change.
    /// </summary>
    /// <remarks>
    /// The IDualNotifyingCollection interface implements both the generic and nongeneric INotifyingCollection.
    /// This is useful when a collection needs to be treated as either generic or nongeneric.
    /// </remarks>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    public interface IDualNotifyingCollection<T> : INotifyingCollection<T>, INotifyingCollection
    {
    }
}
