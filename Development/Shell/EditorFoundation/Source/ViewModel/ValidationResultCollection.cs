// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.TeamFoundation.Migration.Shell.Validation;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a collection of <see cref="ValidationResult"/> objects.
    /// </summary>
    public class ValidationResultCollection : IEnumerable<ValidationResult>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Fields
        private readonly IEditorValidationManager validationManager;
        private readonly HashList<ValidationResult> validationResults;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultCollection"/> class.
        /// </summary>
        /// <param name="validationManager">The validation manager.</param>
        public ValidationResultCollection (IEditorValidationManager validationManager)
        {
            this.validationManager = validationManager;

            this.validationResults = new HashList<ValidationResult> ();
            this.validationResults.AddRange (this.validationManager.EnumerateValidationResults ());

            this.validationManager.ValidationResultAdded += this.OnValidationResultAdded;
            this.validationManager.ValidationResultRemoved += this.OnValidationResultRemoved;
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
                return this.validationResults.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)this.validationResults).IsSynchronized;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.</returns>
        object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)this.validationResults).SyncRoot;
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
        public IEnumerator<ValidationResult> GetEnumerator ()
        {
            return this.validationResults.GetEnumerator ();
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
        void ICollection.CopyTo (Array array, int index)
        {
            ((ICollection)this.validationResults).CopyTo (array, index);
        }
        #endregion

        #region Private Methods
        private void OnValidationResultAdded (object sender, ValidationResultsChangedEvent e)
        {
            this.validationResults.Add (e.ValidationResult);
            this.RaiseCollectionChangedEvent (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, e.ValidationResult));
            this.RaisePropertyChangedEvent ("Count");
        }

        private void OnValidationResultRemoved (object sender, ValidationResultsChangedEvent e)
        {
            int index = this.validationResults.IndexOf (e.ValidationResult);
            System.Diagnostics.Debug.Assert (index >= 0);
            if (index >= 0)
            {
                this.validationResults.RemoveAt (index);
                this.RaiseCollectionChangedEvent (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, e.ValidationResult, index));
                this.RaisePropertyChangedEvent ("Count");
            }
        }

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
    }
}
