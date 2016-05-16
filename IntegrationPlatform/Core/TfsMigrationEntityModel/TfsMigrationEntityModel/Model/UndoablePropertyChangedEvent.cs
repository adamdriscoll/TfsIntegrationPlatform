// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// This is a delegate for handling UndoablePropertyChanged Events.
    /// </summary>
    public delegate void UndoablePropertyChangedEventHandler (ModelObject sender, UndoablePropertyChangedEventArgs eventArgs);

    /// <summary>
    /// The class that contains the arguments for UndoablePropertyChanged
    /// events.  UndoablePropertyChanged events extend the normal .Net
    /// PropertyChanged events to also include the old and new values for the
    /// property, so that the change can later be undone.
    /// </summary>
    public class UndoablePropertyChangedEventArgs : EventArgs
    {
        #region Fields
        private readonly Property propertyName;
        private readonly object oldValue;
        private readonly object newValue;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of an UndoablePropertyChangedEventArgs object.
        /// </summary>
        /// <param name="owner">
        /// The owner of the property that has changed.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that has changed.
        /// </param>
        /// <param name="oldValue">
        /// The value of the property before it was changed.
        /// </param>
        /// <param name="newValue">
        /// The value of the property after it has been changed.
        /// </param>
        public UndoablePropertyChangedEventArgs (ModelObject owner, string propertyName, object oldValue, object newValue)
        {
            this.propertyName = new Property (owner, propertyName);
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the name of the property that changed.
        /// </summary>
        public Property PropertyName
        {
            get
            {
                return this.propertyName;
            }
        }

        /// <summary>
        /// Gets the value of the property before it was changed.
        /// </summary>
        public object OldValue
        {
            get
            {
                return this.oldValue;
            }
        }

        /// <summary>
        /// Gets the value of the property after it was changed.
        /// </summary>
        public object NewValue
        {
            get
            {
                return this.newValue;
            }
        }
        #endregion
    }
}
