// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Microsoft.TeamFoundation.Migration.Shell.Controller;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a collection of recently opened files.
    /// </summary>
    public class RecentFilesCollection : IEnumerable<FileInfo>, INotifyCollectionChanged, IList<FileInfo>
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RecentFilesCollection"/> class.
        /// </summary>
        /// <param name="controller">The controller to watch in order to keep the list of recently opened files up-to-date.</param>
        public RecentFilesCollection (IController controller)
        {
            if (Properties.Settings.Default.MRU == null)
            {
                Properties.Settings.Default.MRU = new StringCollection ();
            }

            controller.Opened += this.OnOpened;
            controller.Saved += this.OnSaved;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the maximum number of files that can be stored in the list.
        /// </summary>
        public int Capacity
        {
            get
            {
                return Properties.Settings.Default.MaxMRUEntryCount;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="RecentFilesCollection"/>.</returns>
        public int Count
        {
            get
            {
                return Math.Min (Properties.Settings.Default.MRU.Count, this.Capacity);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="RecentFilesCollection"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="RecentFilesCollection"/> is read-only; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get
            {
                return Properties.Settings.Default.MRU.IsReadOnly;
            }
        }
        #endregion
        
        #region Indexers
        /// <summary>
        /// Gets or sets the <see cref="System.IO.FileInfo"/> at the specified index.
        /// </summary>
        public FileInfo this[int index]
        {
            get
            {
                return new FileInfo (Properties.Settings.Default.MRU[index]);
            }
            set
            {
                Properties.Settings.Default.MRU[index] = value.FullName;
                this.RaiseCollectionChangedEvent ();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<FileInfo> GetEnumerator ()
        {
            int index = 0;
            foreach (string filePath in Properties.Settings.Default.MRU)
            {
                if (index < this.Capacity)
                {
                    yield return new FileInfo (filePath);
                    index++;
                }
                else
                {
                    break;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return this.GetEnumerator ();
        }

        /// <summary>
        /// Adds a file to the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <param name="file">The file to add.</param>
        public void Add (FileInfo file)
        {
            this.Add (file.FullName);
        }

        /// <summary>
        /// Adds a file to the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <param name="filePath">The path to the file to add.</param>
        public void Add (string filePath)
        {
            this.Insert (Properties.Settings.Default.MRU.Count, filePath);
        }

        /// <summary>
        /// Inserts a file in the <see cref="RecentFilesCollection"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="file">The file to insert.</param>
        public void Insert (int index, FileInfo file)
        {
            this.Insert (index, file.FullName);
        }

        /// <summary>
        /// Inserts a file in the <see cref="RecentFilesCollection"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="filePath">The path to the file to insert.</param>
        public void Insert (int index, string filePath)
        {
            Properties.Settings.Default.MRU.Insert (index, filePath);
            this.RaiseCollectionChangedEvent ();
        }

        /// <summary>
        /// Removes the first occurrence of a specific file from the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <param name="file">The file to remove.</param>
        /// <returns>true if the item was removed; otherwise, false.</returns>
        public bool Remove (FileInfo file)
        {
            return this.Remove (file.FullName);
        }

        /// <summary>
        /// Removes the first occurrence of a specific file from the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <param name="filePath">The path to the file to remove.</param>
        /// <returns>true if the item was removed; otherwise, false.</returns>
        public bool Remove (string filePath)
        {
            Properties.Settings.Default.MRU.Remove (filePath);
            this.RaiseCollectionChangedEvent ();
            return true;
        }

        /// <summary>
        /// Removes the <see cref="RecentFilesCollection"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt (int index)
        {
            Properties.Settings.Default.MRU.RemoveAt (index);
        }

        /// <summary>
        /// Removes all items from the <see cref="RecentFilesCollection"/>.
        /// </summary>
        public void Clear ()
        {
            Properties.Settings.Default.MRU.Clear ();
            this.RaiseCollectionChangedEvent ();
        }

        /// <summary>
        /// Determines whether <see cref="RecentFilesCollection"/> contains the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        /// 	<c>true</c> if the specified file is found in the <see cref="RecentFilesCollection"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains (FileInfo file)
        {
            return this.Contains (file.FullName);
        }

        /// <summary>
        /// Determines whether <see cref="RecentFilesCollection"/> contains the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>
        /// 	<c>true</c> if the specified file is found in the <see cref="RecentFilesCollection"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains (string filePath)
        {
            return Properties.Settings.Default.MRU.Contains (filePath);
        }

        /// <summary>
        /// Determines the index of a specific file in the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>The index of the specified file if found in the list; otherwise, -1.</returns>
        public int IndexOf (FileInfo file)
        {
            return this.IndexOf (file.FullName);
        }

        /// <summary>
        /// Determines the index of a specific file in the <see cref="RecentFilesCollection"/>.
        /// </summary>
        /// <param name="filePath">The file.</param>
        /// <returns>The index of the specified file path if found in the list; otherwise, -1.</returns>
        public int IndexOf (string filePath)
        {
            return Properties.Settings.Default.MRU.IndexOf (filePath);
        }

        /// <summary>
        /// Copies the elements of the <see cref="RecentFilesCollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="RecentFilesCollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.-or-<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.-or-The number of elements in the source <see cref="RecentFilesCollection"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo (FileInfo[] array, int arrayIndex)
        {
            foreach (FileInfo fileInfo in this)
            {
                array[arrayIndex] = fileInfo;
                arrayIndex++;
            }
        }
        #endregion

        #region Private Methods
        private void RaiseCollectionChangedEvent ()
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
            }
        }

        private void OnOpened (object sender, OpenedEventArgs eventArgs)
        {
            if (eventArgs.Error == null)
            {
                this.Update (eventArgs.FilePath);
            }
        }

        private void OnSaved (object sender, SavedEventArgs eventArgs)
        {
            if (eventArgs.Error == null)
            {
                this.Update (eventArgs.FilePath);
            }
        }

        private void Update (string filePath)
        {
            this.Remove (filePath);
            this.Insert (0, filePath);
        }
        #endregion
    }
}
