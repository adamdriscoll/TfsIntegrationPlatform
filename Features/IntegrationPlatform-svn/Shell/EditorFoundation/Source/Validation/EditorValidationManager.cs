// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections;
using System.ComponentModel;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Validation
{
    /// <summary>
    /// Provides a validation manager that works with editors
    /// based on the Editor Foundation's Model design.
    /// </summary>
    /// <typeparam name="T">The type of object that will be validated.</typeparam>
    public class EditorValidationManager<T>
        : ValidationManager, IEditorValidationManager
        where T : ModelRoot
    {
        #region Fields
        private ModelWatcher modelWatcher;
        private readonly HashCollection<ModelObject> registeredObjects;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorValidationManager&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="model">The model to which the validation manager will be bound.</param>
        public EditorValidationManager (T model)
        {
            this.registeredObjects = new HashCollection<ModelObject> ();
            this.Model = model;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorValidationManager&lt;T&gt;"/> class.
        /// </summary>
        public EditorValidationManager ()
            : this (null)
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the Model to which this Validation Manager is bound.
        /// </summary>
        public T Model
        {
            get
            {
                if (this.modelWatcher == null)
                {
                    return null;
                }
                return (T)this.modelWatcher.ModelObject;
            }
            set
            {
                if (this.modelWatcher != null)
                {
                    this.modelWatcher.PropertyChanged -= this.OnPropertyChanged;
                    this.modelWatcher.ItemAdded -= this.OnItemAdded;
                    this.modelWatcher.ItemRemoved -= this.OnItemRemoved;
                    this.modelWatcher.ItemReplaced -= this.OnItemReplaced;
                    this.modelWatcher = null;

                    this.ClearObjects ();
                }

                if (value != null)
                {
                    this.modelWatcher = ModelWatcher.GetSharedModelWatcher (value);
                    this.modelWatcher.PropertyChanged += this.OnPropertyChanged;
                    this.modelWatcher.ItemAdded += this.OnItemAdded;
                    this.modelWatcher.ItemRemoved += this.OnItemRemoved;
                    this.modelWatcher.ItemReplaced += this.OnItemReplaced;

                    this.RegisterModelObject (value);
                }
            }
        }
        #endregion

        #region Private Methods
        private void RegisterModelObject (ModelObject modelObject)
        {
            lock (this.registeredObjects)
            {
                // Make sure the ModelObject hasn't already been processed
                if (modelObject != null && !this.registeredObjects.Contains (modelObject))
                {
                    // Add the object to the list of already processed objects
                    this.registeredObjects.Add (modelObject);

                    // Add the object to the validation manager
                    ISupportValidation obj = modelObject as ISupportValidation;
                    if (obj != null)
                    {
                        this.AddObject (obj);
                        this.BeginValidation (obj);
                    }

                    // Examine the properties of the ModelObject to look for more ModelObjects to process
                    foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (modelObject))
                    {
                        // Check if the property itself is a ModelObject
                        if (propertyDescriptor.PropertyType.IsSubclassOf (typeof (ModelObject)))
                        {
                            // Recurse on the ModelObject property
                            this.RegisterModelObject ((ModelObject)propertyDescriptor.GetValue (modelObject));
                        }
                        // Check if the property is a collection of child ModelObjects
                        else if (typeof (IEnumerable).IsAssignableFrom (propertyDescriptor.PropertyType))
                        {
                            INotifyingCollection notifyingCollection = propertyDescriptor.GetValue (modelObject) as INotifyingCollection;
                            if (notifyingCollection != null)
                            {
                                // Recurse on ModelObject children
                                foreach (object childObject in notifyingCollection)
                                {
                                    this.RegisterModelObject (childObject as ModelObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UnregisterModelObject (ModelObject modelObject)
        {
            lock (this.registeredObjects)
            {
                // Make sure the object was previously processed
                if (modelObject != null && this.registeredObjects.Contains (modelObject))
                {
                    // Remove the object from the list of already processed objects
                    this.registeredObjects.Remove (modelObject);

                    // Remove the object to the validation manager
                    ISupportValidation obj = modelObject as ISupportValidation;
                    if (obj != null)
                    {
                        this.RemoveObject (obj);
                    }

                    // Examine the properties of the ModelObject to look for more ModelObjects to monitor
                    foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (modelObject))
                    {
                        // Check if the property itself is a ModelObject
                        if (propertyDescriptor.PropertyType.IsSubclassOf (typeof (ModelObject)))
                        {
                            // Recurse on the ModelObject property
                            this.UnregisterModelObject ((ModelObject)propertyDescriptor.GetValue (modelObject));
                        }
                        // Check if the property is a collection of child ModelObjects
                        else if (typeof (IEnumerable).IsAssignableFrom (propertyDescriptor.PropertyType))
                        {
                            INotifyingCollection notifyingCollection = propertyDescriptor.GetValue (modelObject) as INotifyingCollection;
                            if (notifyingCollection != null)
                            {
                                // Recurse on ModelObject children
                                foreach (object childObject in notifyingCollection)
                                {
                                    this.UnregisterModelObject (childObject as ModelObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnPropertyChanged (ModelObject sender, UndoablePropertyChangedEventArgs args)
        {
            ISupportValidation obj = sender as ISupportValidation;
            if (obj != null)
            {
                PropertyDescriptor propertyDescriptor = Utilities.GetPropertyDescriptor (sender, args.PropertyName);
                if (propertyDescriptor.Attributes.Contains (AffectsValidityAttribute.Yes))
                {
                    this.BeginValidation (obj);
                }
            }
        }

        private void OnItemAdded (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs e)
        {
            this.RegisterModelObject (e.Item as ModelObject);
        }

        private void OnItemRemoved (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs e)
        {
            this.UnregisterModelObject (e.Item as ModelObject);
        }

        private void OnItemReplaced (ModelObject parentObject, INotifyingCollection sender, IItemReplacedEventArgs e)
        {
            this.UnregisterModelObject (e.OldItem as ModelObject);
            this.RegisterModelObject (e.NewItem as ModelObject);
        }
        #endregion
    }
}
