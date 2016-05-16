// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// A somewhat heavy-weight, memory intensive data structure
    /// that provides an ordered list, but also very fast Contains and IndexOf operations
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    public class HashList<T> : KeyedCollection<T, T>
    {
        #region Public Methods
        /// <summary>
        /// Adds a range of items.
        /// </summary>
        /// <param name="collection">The items.</param>
        public void AddRange (IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                this.Add (item);
            }
        }

        /// <summary>
        /// Copies the elements of the HashList to a new array.
        /// </summary>
        /// <returns>The array of items.</returns>
        public T[] ToArray ()
        {
            T[] array = new T[this.Count];
            this.CopyTo (array, 0);
            return array;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// When implemented in a derived class, extracts the key from the specified element.
        /// </summary>
        /// <param name="item">The element from which to extract the key.</param>
        /// <returns>The key for the specified element.</returns>
        protected override T GetKeyForItem (T item)
        {
            return item;
        }
        #endregion
    }
}
