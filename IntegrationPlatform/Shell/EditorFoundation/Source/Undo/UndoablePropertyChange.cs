// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// An undoable corresponding to the value of a property being changed.
    /// </summary>
    public class UndoablePropertyChange : IUndoable
    {
        #region Fields
        private readonly ModelObject modelObject;
        private readonly PropertyDescriptor propertyDescriptor;
        private readonly object oldValue;
        private readonly object newValue;
        #endregion

        #region Constructors & Destructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UndoablePropertyChange"/> class.
        /// </summary>
        /// <param name="modelObject">The model object.</param>
        /// <param name="property">The property.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public UndoablePropertyChange (ModelObject modelObject, string property, object oldValue, object newValue)
            : base ()
        {
            if (modelObject == null)
            {
                throw new Exception ("The ModelObject must not be null");
            }
            this.modelObject = modelObject;

            this.propertyDescriptor = Utilities.GetPropertyDescriptor (modelObject, property);

            this.oldValue = oldValue;
            this.newValue = newValue;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the ModelObject associated with this change.
        /// </summary>
        /// <value>The ModelObject.</value>
        public ModelObject ModelObject
        {
            get
            {
                return this.modelObject;
            }
        }

        /// <summary>
        /// Gets the PropertyDescriptor for the property that was changed.
        /// </summary>
        /// <value>The PropertyDescriptor.</value>
        public PropertyDescriptor PropertyDescriptor
        {
            get
            {
                return this.propertyDescriptor;
            }
        }

        /// <summary>
        /// Gets the old value (pre-change value).
        /// </summary>
        /// <value>The old value.</value>
        public object OldValue
        {
            get
            {
                return this.oldValue;
            }
        }

        /// <summary>
        /// Gets the new value (post-change value).
        /// </summary>
        /// <value>The new value.</value>
        public object NewValue
        {
            get
            {
                return this.newValue;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Undo the operation that this IUndoable represents.
        /// </summary>
        public void Undo ()
        {
            this.propertyDescriptor.SetValue (this.modelObject, this.oldValue);
        }

        /// <summary>
        /// Redo the operation that this IUndoable represents.
        /// </summary>
        public void Redo ()
        {
            this.propertyDescriptor.SetValue (this.modelObject, this.newValue);
        }

        /// <summary>
        /// Determines whether this IUndoable can contain the specified IUndoable.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containment validity.</param>
        /// <returns>
        /// 	<c>true</c> if the specified IUndoable can be contained by the current IUndoable, <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// In many cases, it makes sense to group IUndoables together. For example,
        /// sometimes Models keep track of whether the value of a property has been
        /// explicitly set by a user. When a user first explicitly sets the value of
        /// a property, it would make sense to group together the property change
        /// and the property specified change.
        /// </remarks>
        public bool CanContain (IUndoable undoable)
        {
            return false;
        }

        /// <summary>
        /// Adds the specified IUndoable to this IUndoable.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this IUndoable.</param>
        /// <returns>
        /// 	<c>true</c> if the specified IUndoable was successfully added to this IUndoable, <c>false</c> otherwise.
        /// </returns>
        public bool Add (IUndoable undoable)
        {
            return false;
        }
        #endregion
    }
}
