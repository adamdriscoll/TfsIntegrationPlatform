// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// Represents the method that will handle a Notifying Collection's nongeneric ItemAdded or ItemRemoved event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The data associated with the event.</param>
    public delegate void ItemAddedRemovedEventHandler (INotifyingCollection sender, IItemAddedRemovedEventArgs e);

    /// <summary>
    /// Represents the method that will handle a Notifying Collection's nongeneric ItemReplaced event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The data associated with the event.</param>
    public delegate void ItemReplacedEventHandler (INotifyingCollection sender, IItemReplacedEventArgs e);

    /// <summary>
    /// Provides data for the nongeneric ItemAdded and nongeneric ItemRemoved events.
    /// </summary>
    public interface IItemAddedRemovedEventArgs
    {
        #region Properties
        /// <summary>
        /// Gets the item that was added or removed.
        /// </summary>
        object Item
        {
            get;
        }

        /// <summary>
        /// Gets the index of the item that was added or removed.
        /// </summary>
        int Index
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the nongeneric ItemReplaced event.
    /// </summary>
    public interface IItemReplacedEventArgs
    {
        #region Properties
        /// <summary>
        /// The old item that was replaced.
        /// </summary>
        object OldItem
        {
            get;
        }

        /// <summary>
        /// The new item that replaced the old item.
        /// </summary>
        object NewItem
        {
            get;
        }

        /// <summary>
        /// The index of the item that was replaced.
        /// </summary>
        int Index
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Represents the method that will handle a Notifying Collection's generic ItemAdded event.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void ItemAddedEventHandler<T> (IDualNotifyingCollection<T> sender, ItemAddedEventArgs<T> eventArgs);

    /// <summary>
    /// Represents the method that will handle a Notifying Collection's generic ItemRemoved event.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void ItemRemovedEventHandler<T> (IDualNotifyingCollection<T> sender, ItemRemovedEventArgs<T> eventArgs);

    /// <summary>
    /// Represents the method that will handle a Notifying Collection's generic ItemReplaced event.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void ItemReplacedEventHandler<T> (IDualNotifyingCollection<T> sender, ItemReplacedEventArgs<T> eventArgs);

    /// <summary>
    /// Provides data for the generic ItemRemoved and ItemAdded events.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public abstract class ItemAddedRemovedEventArgs<T> 
        : EventArgs, IItemAddedRemovedEventArgs
    {
        #region Fields
        private readonly T item;
        private readonly int index;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ItemAddedRemovedEventArgs class.
        /// </summary>
        /// <param name="item">The item that was added or removed.</param>
        /// <param name="index">The index of the item that was added or removed.</param>
        public ItemAddedRemovedEventArgs (T item, int index)
        {
            this.item = item;
            this.index = index;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the item that was added or removed.
        /// </summary>
        public T Item
        {
            get
            {
                return this.item;
            }
        }

        /// <summary>
        /// Gets the item that was added or removed.
        /// </summary>
        object IItemAddedRemovedEventArgs.Item
        {
            get
            {
                return this.Item;
            }
        }

        /// <summary>
        /// Gets the index of the item that was added or removed.
        /// </summary>
        public int Index
        {
            get
            {
                return this.index;
            }
        }

        /// <summary>
        /// Gets the index of the item that was added or removed.
        /// </summary>
        int IItemAddedRemovedEventArgs.Index
        {
            get
            {
                return this.Index;
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the generic ItemAdded event.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public class ItemAddedEventArgs<T> 
        : ItemAddedRemovedEventArgs<T>
    {        
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ItemAddedEventArgs class.
        /// </summary>
        /// <param name="item">The item that was added or removed.</param>
        /// <param name="index">The index of the item that was added or removed.</param>
        public ItemAddedEventArgs (T item, int index) 
            : base (item, index)
        {
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the generic ItemRemoved event.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public class ItemRemovedEventArgs<T> 
        : ItemAddedRemovedEventArgs<T>
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ItemRemovedEventArgs class.
        /// </summary>
        /// <param name="item">The item that was added or removed.</param>
        /// <param name="index">The index of the item that was added or removed.</param>
        public ItemRemovedEventArgs (T item, int index) 
            : base (item, index)
        {
        }
        #endregion
    }

    /// <summary>
    /// Provides data for the generic ItemReplaced event.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public class ItemReplacedEventArgs<T> 
        : EventArgs, IItemReplacedEventArgs
    {
        #region Fields
        private readonly T oldItem;
        private readonly T newItem;
        private readonly int index;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ItemReplacedEventArgs class.
        /// </summary>
        /// <param name="oldItem">The old item that was replaced.</param>
        /// <param name="newItem">The new item that replaced the old item.</param>
        /// <param name="index">The index of the item that was replaced.</param>
        public ItemReplacedEventArgs (T oldItem, T newItem, int index)
        {
            this.oldItem = oldItem;
            this.newItem = newItem;
            this.index = index;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The old item that was replaced.
        /// </summary>
        public T OldItem
        {
            get
            {
                return this.oldItem;
            }
        }

        /// <summary>
        /// The old item that was replaced.
        /// </summary>
        object IItemReplacedEventArgs.OldItem
        {
            get
            {
                return this.OldItem;
            }
        }

        /// <summary>
        /// The new item that replaced the old item.
        /// </summary>
        public T NewItem
        {
            get
            {
                return this.newItem;
            }
        }

        /// <summary>
        /// The new item that replaced the old item.
        /// </summary>
        object IItemReplacedEventArgs.NewItem
        {
            get
            {
                return this.NewItem;
            }
        }

        /// <summary>
        /// The index of the item that was replaced.
        /// </summary>
        public int Index
        {
            get
            {
                return this.index;
            }
        }

        /// <summary>
        /// The index of the item that was replaced.
        /// </summary>
        int IItemReplacedEventArgs.Index
        {
            get
            {
                return this.Index;
            }
        }
        #endregion
    }
}
