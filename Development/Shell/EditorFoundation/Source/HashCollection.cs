// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// An unordered collection that provides a fast Contains operation.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    public class HashCollection<T> : ICollection<T>
    {
        #region Fields
        private readonly Dictionary<T, bool> dictionary;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the HashCollection class.
        /// </summary>
        public HashCollection ()
        {
            this.dictionary = new Dictionary<T, bool> ();
        }

        /// <summary>
        /// Initializes a new instance of the HashCollection class.
        /// </summary>
        /// <param name="equalityComparer">The IEqualityComparer implementation to use when comparing items, or a null reference (Nothing in Visual Basic) to use the default EqualityComparer for the type of the item.</param>
        public HashCollection (IEqualityComparer<T> equalityComparer)
            : this ()
        {
            this.dictionary = new Dictionary<T, bool> (equalityComparer);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the number of items actually contained in the HashCollection. 
        /// </summary>
        public int Count
        {
            get
            {
                return this.dictionary.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the HashCollection is read-only.
        /// </summary>
        /// <value><c>false</c> always. The HashCollection is a read-write collection.</value>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a range of items to the HashCollection.
        /// </summary>
        /// <param name="collection">The items to add.</param>
        public void AddRange (IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                this.Add (item);
            }
        }

        /// <summary>
        /// Adds an item to the HashCollection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add (T item)
        {
            this.dictionary.Add (item, true);
        }

        /// <summary>
        /// Removes all items from the HashCollection.
        /// </summary>
        public void Clear ()
        {
            this.dictionary.Clear ();
        }

        /// <summary>
        /// Determines whether an item is contained in the HashCollection.
        /// </summary>
        /// <param name="item">The item to locate in the HashCollection.</param>
        /// <returns><c>true</c> if the item is found in the HashCollection, <c>false</c> otherwise.</returns>
        public bool Contains (T item)
        {
            return this.dictionary.ContainsKey (item);
        }

        /// <summary>
        /// Copies the HashCollection or a portion of it to an array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the items copied from HashCollection. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo (T[] array, int arrayIndex)
        {
            this.dictionary.Keys.CopyTo (array, arrayIndex);
        }

        /// <summary>
        /// Removes the specified item from the HashCollection.
        /// </summary>
        /// <param name="item">The item to remove from the HashCollection.</param>
        /// <returns><c>true</c> if the item was successfully removed, <c>false</c> otherwise.</returns>
        public bool Remove (T item)
        {
            return this.dictionary.Remove (item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the HashCollection.
        /// </summary>
        /// <returns>A generic enmerator for the HashCollection.</returns>
        public IEnumerator<T> GetEnumerator ()
        {
            return this.dictionary.Keys.GetEnumerator ();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the HashCollection.
        /// </summary>
        /// <returns>A nongeneric enmerator for the HashCollection.</returns>
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return this.GetEnumerator ();
        }

        /// <summary>
        /// Copies the elements of the HashCollection to a new array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray ()
        {
            T[] array = new T[this.Count];
            this.dictionary.Keys.CopyTo (array, 0);
            return array;
        }
        #endregion
    }
}
