// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Provides a collection that can only be modified by the owner.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class PrivateCollection<T> : ICollection, IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Fields
        private readonly PrivateCollectionKey key;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateCollection&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public PrivateCollection (out IList<T> key)
        {
            key = this.key = new PrivateCollectionKey (this);
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Indexers
        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                return this.key[index];
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.ICollection"/>.</returns>
        public int Count
        {
            get
            {
                return this.key.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized
        {
            get
            {
                return ((ICollection)this.key).IsSynchronized;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.</returns>
        public object SyncRoot
        {
            get
            {
                return ((ICollection)this.key).SyncRoot;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.-or- <paramref name="index"/> is equal to or greater than the length of <paramref name="array"/>.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>. </exception>
        /// <exception cref="T:System.ArgumentException">The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>. </exception>
        public void CopyTo (Array array, int index)
        {
            ((ICollection)this.key).CopyTo (array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return this.GetEnumerator ();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator ()
        {
            return this.key.GetEnumerator ();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Called when an item is inserted.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        protected virtual void OnInsertItem (int index, T item)
        {
        }

        /// <summary>
        /// Called when an item is set.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="oldItem">The old item.</param>
        /// <param name="newItem">The new item.</param>
        protected virtual void OnSetItem (int index, T oldItem, T newItem)
        {
        }

        /// <summary>
        /// Called when an item is removed.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        protected virtual void OnRemoveItem (int index, T item)
        {
        }

        /// <summary>
        /// Called when all items are cleared.
        /// </summary>
        /// <param name="items">The items.</param>
        protected virtual void OnClearItems (IEnumerable<T> items)
        {
        }
        #endregion

        #region Private Methods
        private void RaiseCollectionChangedEvent (NotifyCollectionChangedEventArgs eventArgs)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged (this, eventArgs);
            }
        }

        private void RaisePropertyChangedEvent (string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
            }
        }
        #endregion

        #region Classes
        private class PrivateCollectionKey : Collection<T>
        {
            private readonly PrivateCollection<T> privateCollection;

            public PrivateCollectionKey (PrivateCollection<T> privateCollection)
            {
                this.privateCollection = privateCollection;
            }

            protected override void InsertItem (int index, T item)
            {
                base.InsertItem (index, item);

                this.privateCollection.OnInsertItem (index, item);

                this.privateCollection.RaiseCollectionChangedEvent (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, item, index));
                this.privateCollection.RaisePropertyChangedEvent ("Count");
                this.privateCollection.RaisePropertyChangedEvent ("Item[]");
            }

            protected override void SetItem (int index, T item)
            {
                T oldItem = this[index];

                base.SetItem (index, item);

                this.privateCollection.OnSetItem (index, oldItem, item);

                this.privateCollection.RaiseCollectionChangedEvent (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Replace, item, oldItem, index));
            }

            protected override void RemoveItem (int index)
            {
                T oldItem = this[index];

                base.RemoveItem (index);

                this.privateCollection.OnRemoveItem (index, oldItem);

                this.privateCollection.RaiseCollectionChangedEvent (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, oldItem, index));
                this.privateCollection.RaisePropertyChangedEvent ("Count");
                this.privateCollection.RaisePropertyChangedEvent ("Item[]");
            }

            protected override void ClearItems ()
            {
                //T[] oldItems = this.ToArray (); ** Uncomment when we have C# 3.0 support **
                T[] oldItems = Enumerable.ToArray (this);

                base.ClearItems ();

                this.privateCollection.OnClearItems (oldItems);

                this.privateCollection.RaiseCollectionChangedEvent (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, oldItems));
                this.privateCollection.RaisePropertyChangedEvent ("Count");
                this.privateCollection.RaisePropertyChangedEvent ("Item[]");
            }
        }
        #endregion
    }
}
