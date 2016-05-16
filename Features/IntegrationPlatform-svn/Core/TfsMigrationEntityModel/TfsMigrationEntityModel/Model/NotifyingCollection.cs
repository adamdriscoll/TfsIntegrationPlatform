// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// Represents a collection of objects. Raises events
    /// when items are added, removed, or replaced.
    /// </summary>
    /// <typeparam name="T">The type to be contained in the collection.</typeparam>
    [Serializable]
    public class NotifyingCollection<T> 
        : Collection<T>, IDualNotifyingCollection<T>, INotifyCollectionChanged
    {        
        #region Fields
        [NonSerialized]
        private Collection<CollectionRestriction<T>> restrictions;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the NotifyingCollection class.
        /// </summary>
        /// <param name="restrictions">The initial set of collection membership restrictions.</param>
        public NotifyingCollection (params CollectionRestriction<T>[] restrictions)
            : base ()
        {
            this.restrictions = new Collection<CollectionRestriction<T>> (restrictions);
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when an item is added to the collection.
        /// </summary>
        [field: NonSerialized]
        public event ItemAddedEventHandler<T> ItemAdded;

        /// <summary>
        /// Occurs when an item is removed from the collection.
        /// </summary>
        [field: NonSerialized]
        public event ItemRemovedEventHandler<T> ItemRemoved;

        /// <summary>
        /// Occurs when an item is replaced in the collection.
        /// </summary>
        [field: NonSerialized]
        public event ItemReplacedEventHandler<T> ItemReplaced;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        [field: NonSerialized]
        private event NotifyCollectionChangedEventHandler CollectionChanged;

        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add
            {
                this.CollectionChanged += value;
            }
            remove
            {
                this.CollectionChanged -= value;
            }
        }

        event ItemAddedRemovedEventHandler INotifyingCollection.ItemAdded
        {
            add
            {
                this.ItemAdded += new ItemAddedEventHandler<T> (value);
            }
            remove
            {
                this.ItemAdded -= new ItemAddedEventHandler<T> (value);
            }
        }

        event ItemAddedRemovedEventHandler INotifyingCollection.ItemRemoved
        {
            add
            {
                this.ItemRemoved += new ItemRemovedEventHandler<T> (value);
            }
            remove
            {
                this.ItemRemoved -= new ItemRemovedEventHandler<T> (value);
            }
        }

        event ItemReplacedEventHandler INotifyingCollection.ItemReplaced
        {
            add
            {
                this.ItemReplaced += new ItemReplacedEventHandler<T> (value);
            }
            remove
            {
                this.ItemReplaced -= new ItemReplacedEventHandler<T> (value);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts the collection of items to an equivalent string representation. 
        /// </summary>
        /// <returns>A string that represents the collection of items.</returns>
        public override string ToString ()
        {
            string[] items = new string[this.Count];
            for (int i = 0; i < this.Count; i++)
            {
                items[i] = this[i].ToString ();
            }

            return "{" + string.Join (", ", items) + "}";
        }

        /// <summary>
        /// Adds the specified items to the end of the collection.
        /// </summary>
        /// <param name="items">The set of items to be added to the collection.</param>
        public virtual void AddRange (params T[] items)
        {
            foreach (T item in items)
            {
                this.Add (item);
            }
        }

        /// <summary>
        /// Adds a collection membership restriction to the collection.
        /// </summary>
        /// <param name="restriction">The collection membership restriction to be added to the collection.</param>
        public void AddRestriction (CollectionRestriction<T> restriction)
        {
            this.restrictions.Add (restriction);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// InsertsInserts an item into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the item should be inserted.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem (int index, T item)
        {
            this.VerifyAllRestrictions (item);

            base.InsertItem (index, item);
            this.RaiseItemAddedEvent (item, index);
        }

        /// <summary>
        /// Replaces the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to replace.</param>
        /// <param name="item">The new value for the item at the specified index.</param>
        protected override void SetItem (int index, T item)
        {
            this.VerifyAllRestrictions (item);

            T oldItem = this[index];

            base.SetItem (index, item);
            this.RaiseItemReplacedEvent (oldItem, item, index);
        }

        /// <summary>
        /// Removes the item at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        protected override void RemoveItem (int index)
        {
            T item = this[index];

            base.RemoveItem (index);
            this.RaiseItemRemovedEvent (item, index);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        protected override void ClearItems ()
        {
            while (this.Count > 0)
            {
                this.RemoveItem (0);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>       
        /// Raises the ItemAdded event.       
        /// </summary>       
        private void RaiseItemAddedEvent (T item, int index)
        {
            if (this.ItemAdded != null)
            {
                this.ItemAdded (this, new ItemAddedEventArgs<T> (item, index));
            }

            if (this.CollectionChanged != null)
            {
                this.CollectionChanged (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, item, index));
            }
        }

        /// <summary>       
        /// Raises the ItemRemoved event.       
        /// </summary>       
        private void RaiseItemRemovedEvent (T item, int index)
        {
            if (this.ItemRemoved != null)
            {
                this.ItemRemoved (this, new ItemRemovedEventArgs<T> (item, index));
            }

            if (this.CollectionChanged != null)
            {
                this.CollectionChanged (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, item, index));
            }
        }

        /// <summary>       
        /// Raises the ItemReplaced event.       
        /// </summary>       
        private void RaiseItemReplacedEvent (T oldItem, T newItem, int index)
        {
            if (this.ItemReplaced != null)
            {
                this.ItemReplaced (this, new ItemReplacedEventArgs<T> (oldItem, newItem, index));
            }

            if (this.CollectionChanged != null)
            {
                this.CollectionChanged (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
            }
        }

        /// <summary>
        /// Verifies all membership restrictions for the specified item.
        /// </summary>
        /// <param name="item">The item whose membership is to be verified.</param>
        private void VerifyAllRestrictions (T item)
        {
            foreach (CollectionRestriction<T> restriction in this.restrictions)
            {
                restriction.Verify (item);
            }
        }
        #endregion
    }
}
