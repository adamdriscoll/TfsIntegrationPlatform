// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration
{
    /// <summary>
    /// This class serves as a generic base class for all Model objects.
    /// </summary>
    [Serializable]
    public abstract class ModelObject : INotifyPropertyChanged
    {
        /// <summary>
        /// This event is raised whenever a property of the class changes.
        /// </summary>
        [field: NonSerialized]
        public event UndoablePropertyChangedEventHandler PropertyChanged;

        [field: NonSerialized]
        private PropertyChangedEventHandler notifyPropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this.notifyPropertyChanged += value;
            }
            remove
            {
                this.notifyPropertyChanged -= value;
            }
        }

        /// <summary>
        /// This method is a helper method for raising the PropertyChanged event.
        /// </summary>
        /// <param name="name">The name of the property that was just changed.</param>
        /// <param name="oldValue">The value of the property before it was changed.</param>
        /// <param name="newValue">The value of the property after it was changed.</param>
        protected void RaisePropertyChangedEvent (string name, object oldValue, object newValue)
        {
            // Validate the specified property name
            this.ValidateProperty (name);

            // Create the event args
            UndoablePropertyChangedEventArgs eventArgs = new UndoablePropertyChangedEventArgs (this, name, oldValue, newValue);

            // Invoke the OnPropertyChanged method for derived classes
            this.OnPropertyChanged (eventArgs);

            // Raise the event for the INotifyPropertyChanged interface
            if (this.notifyPropertyChanged != null)
            {
                this.notifyPropertyChanged (this, new System.ComponentModel.PropertyChangedEventArgs (name));
            }

            // Raise the EditorFoundation based event which includes old/new values
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, eventArgs);
            }
        }

        /// <summary>
        /// Called before the <see cref="E:PropertyChanged"/> event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnPropertyChanged in a derived class, calling the base class's OnPropertyChanged method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">The <see cref="UndoablePropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged (UndoablePropertyChangedEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Verifies that the specified property is defined on the current object (DEBUG only).
        /// </summary>
        /// <param name="name">Name of the property to validate.</param>
        internal void ValidateProperty (string name)
        {
            Debug.Assert (Utility.TryGetPropertyDescriptor (this, name) != null, string.Format ("{0} is not a property of {1}", name, this.GetType ().Name));
        }
    }
}