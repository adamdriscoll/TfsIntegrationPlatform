// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Represents a single item that can be searched for.
    /// This consists of a Model Object plus an associated Property.
    /// </summary>
    public class EditorSearchItem : IEquatable<EditorSearchItem>, IDisposable
    {
        #region Fields
        private readonly ModelObject modelObject;
        private readonly PropertyDescriptor property;
        string propertyValue;
        #endregion

        #region Constructors
        internal EditorSearchItem (ModelObject modelObject, PropertyDescriptor property)
        {
            this.modelObject = modelObject;
            this.property = property;
            this.propertyValue = null;

            this.modelObject.PropertyChanged += this.OnModelObjectPropertyChanged;
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when the value of the associated property changes.
        /// </summary>
        public event EventHandler PropertyValueChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Model Object associated with this Search Item.
        /// </summary>
        public ModelObject ModelObject
        {
            get
            {
                return this.modelObject;
            }
        }

        /// <summary>
        /// Gets the Property Descriptor associated with this Search Item.
        /// </summary>
        public PropertyDescriptor PropertyDescriptor
        {
            get
            {
                return this.property;
            }
        }

        /// <summary>
        /// Gets the value of the Property associated with this Search Item.
        /// </summary>
        public string PropertyValue
        {
            get
            {
              if (this.propertyValue == null)
              {
                    object propertyValue = this.PropertyDescriptor.GetValue (this.ModelObject);
                    if (propertyValue != null)
                    {
                        this.propertyValue = propertyValue.ToString ();
                    }
              }
              return this.propertyValue;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose ()
        {
            this.modelObject.PropertyChanged -= this.OnModelObjectPropertyChanged;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <remarks>
        /// Two instances of an Editor Search Item that contain the same. 
        /// Model Object and same Property should be considered the same.
        /// </remarks>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode ()
        {
            return this.modelObject.GetHashCode () ^ this.property.Name.GetHashCode ();
        }

        /// <summary>
        /// Determines whether two EditorSearchItem instances are equal.
        /// </summary>
        /// <remarks>
        /// Two instances of an Editor Search Item that contain the same. 
        /// Model Object and same Property should be considered the same.
        /// </remarks>
        /// <param name="obj">The EditorSearchItem to compare with the current EditorSearchItem.</param>
        /// <returns><c>true</c> if the specified Object is equal to the current EditorSearchItem, <c>false</c> otherwise.</returns>
        public override bool Equals (object obj)
        {
            if (obj is EditorSearchItem)
            {
                return this.Equals ((EditorSearchItem)obj);
            }
            return false;
        }

        /// <summary>
        /// Determines whether two EditorSearchItem instances are equal.
        /// </summary>
        /// <remarks>
        /// Two instances of an Editor Search Item that contain the same. 
        /// Model Object and same Property should be considered the same.
        /// </remarks>
        /// <param name="other">The EditorSearchItem to compare with the current EditorSearchItem.</param>
        /// <returns><c>true</c> if the specified EditorSearchItem is equal to the current EditorSearchItem, <c>false</c> otherwise.</returns>
        public bool Equals (EditorSearchItem other)
        {
            return this.ModelObject.Equals (other.ModelObject) && this.PropertyDescriptor.Equals (other.PropertyDescriptor);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString ()
        {
            return string.Format ("Source: {0}, Property Name: {1}, Property Value: {2}", this.ModelObject, this.PropertyDescriptor.Name, this.PropertyValue);
        }
        #endregion

        #region Private Methods
        private void OnModelObjectPropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == this.PropertyDescriptor.Name)
            {
                string oldValue = this.PropertyValue;
                this.propertyValue = null;
                if (this.PropertyValueChanged != null && oldValue != this.PropertyValue)
                {
                    this.PropertyValueChanged(this, EventArgs.Empty);
                }
            }
        }

        #endregion
    }
}
