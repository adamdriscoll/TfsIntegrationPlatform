// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// Represents the method that will handle the ModelWatcher's PropertyChanged event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void PropertyChangedWatcher (ModelObject sender, UndoablePropertyChangedEventArgs eventArgs);

    /// <summary>
    /// Represents the method that will handle the ModelWatcher's ItemAdded event.
    /// </summary>
    /// <param name="parentObject">The object that owns the collection.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void ItemAddedEventWatcher (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs);

    /// <summary>
    /// Represents the method that will handle the ModelWatcher's ItemRemoved event.
    /// </summary>
    /// <param name="parentObject">The object that owns the collection.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void ItemRemovedEventWatcher (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs);

    /// <summary>
    /// Represents the method that will handle the ModelWatcher's ItemReplaced event.
    /// </summary>
    /// <param name="parentObject">The object that owns the collection.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The data associated with the event.</param>
    public delegate void ItemReplacedEventWatcher (ModelObject parentObject, INotifyingCollection sender, IItemReplacedEventArgs eventArgs);

    /// <summary>
    /// This class allows a Model consumer to be notified
    /// when the properties or structure of the Model change
    /// without explicitly subscribing to events on every
    /// object that composes the Model.
    /// </summary>
    public class ModelWatcher
    {
        #region Fields
        private ModelObject modelObject;
        private Dictionary<ModelObject, bool> registeredObjects;
        private Dictionary<INotifyingCollection, ModelObject> collectionOwners;
        private Dictionary<Type, Dictionary<ModelObject, bool>> instances;

        private static Dictionary<ModelObject, ModelWatcher> sharedModelWatchers;
        #endregion

        #region Constructors
        static ModelWatcher ()
        {
            ModelWatcher.sharedModelWatchers = new Dictionary<ModelObject, ModelWatcher> ();
        }

        /// <summary>
        /// Initializes a new instance of the ModelWatcher class.
        /// </summary>
        public ModelWatcher ()
            : this (null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ModelWatcher class.
        /// </summary>
        /// <param name="modelObject">The ModelObject to which to attach.</param>
        public ModelWatcher (ModelObject modelObject)
        {
            this.registeredObjects = new Dictionary<ModelObject, bool> ();
            this.collectionOwners = new Dictionary<INotifyingCollection, ModelObject> ();
            this.instances = new Dictionary<Type, Dictionary<ModelObject, bool>> ();

            this.ModelObject = modelObject;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a property of an object in the ModelObject being watched changes.
        /// </summary>
        public event PropertyChangedWatcher PropertyChanged;

        /// <summary>
        /// Occurs when an item is added to an object in the ModelObject being watched.
        /// </summary>
        public event ItemAddedEventWatcher ItemAdded;

        /// <summary>
        /// Occurs when an item is removed from an object in the ModelObject being watched.
        /// </summary>
        public event ItemRemovedEventWatcher ItemRemoved;

        /// <summary>
        /// Occurs when an item is replaced in an object in the ModelObject being watched.
        /// </summary>
        public event ItemReplacedEventWatcher ItemReplaced;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the ModelObject to which this ModelWatcher is attached.
        /// </summary>
        public ModelObject ModelObject
        {
            get
            {
                return this.modelObject;
            }
            set
            {
                if (value != this.modelObject)
                {
                    this.UnregisterModelObject (this.modelObject);
                    this.modelObject = value;
                    this.RegisterModelObject (this.modelObject);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Enumerates all instances of a particular type contained in the ModelObject
        /// to which this ModelWatcher is attached.
        /// </summary>
        /// <typeparam name="T">The type of object to enumerate.</typeparam>
        /// <param name="includeSubTypes"><c>true</c> to include subtypes of the specified type, <c>false</c> otherwise.</param>
        /// <returns>An enumerator.</returns>
        public IEnumerable<T> EnumerateInstances<T> (bool includeSubTypes)
        {
            Type requestedType = typeof (T);

            if (!includeSubTypes)
            {
                if (this.instances.ContainsKey (requestedType))
                {
                    foreach (object instance in this.instances[requestedType].Keys)
                    {
                        yield return (T)instance;
                    }
                }
            }
            else
            {
                foreach (Type instanceType in this.instances.Keys)
                {
                    if (requestedType.IsAssignableFrom (instanceType))
                    {
                        foreach (object instance in this.instances[instanceType].Keys)
                        {
                            yield return (T)instance;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all objects contained in the ModelObject
        /// to which this ModelWatcher is attached.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public IEnumerable<ModelObject> EnumerateAllInstances ()
        {
            return this.EnumerateInstances<ModelObject> (true);
        }

        /// <summary>
        /// Gets a "shared" ModelWatcher for the specified ModelObject.
        /// </summary>
        /// <remarks>
        /// Creating a new ModelWatcher can be computationally expensive for large Models
        /// since the ModelWatcher needs to "crawl" the entire model hierarchy. When this
        /// method is used, the ModelWatcher for the specified ModelObject is cached. Future
        /// requests for a ModelWatcher for the specified ModelObject reuse the same ModelWatcher.
        /// </remarks>
        /// <param name="modelObject">The ModelObject to which to bind the ModelWatcher.</param>
        /// <returns>A "shared" ModelWatcher instance.</returns>
        public static ModelWatcher GetSharedModelWatcher (ModelObject modelObject)
        {
            if (modelObject == null)
            {
                throw new ArgumentException ("ModelObject cannot be null", "modelObject");
            }

            lock (typeof (ModelWatcher))
            {
                if (!ModelWatcher.sharedModelWatchers.ContainsKey (modelObject))
                {
                    // Set the dictionary entry to null until the ModelWatcher is fully constructed.
                    // This way, if during construction of the ModelWatcher the same ModelWatcher is requested
                    // (on the same thread), null is returned, indicating that the ModelWatcher is not
                    // yet fully constructed.
                    ModelWatcher.sharedModelWatchers.Add (modelObject, null);
                    ModelWatcher modelWatcher = new ModelWatcher (modelObject);
                    ModelWatcher.sharedModelWatchers[modelObject] = modelWatcher;
                }
            }
            return ModelWatcher.sharedModelWatchers[modelObject];
        }
        #endregion

        #region Object Model Discovery
        /// <summary>
        /// Registers a ModelObject with this ModelWatcher. Property changed events
        /// and collection add/remove/replace events will be watched.
        /// </summary>
        /// <param name="modelObject">The ModelObject to watch.</param>
        private void RegisterModelObject (ModelObject modelObject)
        {
            // Make sure the ModelObject hasn't already been registered and isn't a blacklisted type
            if (modelObject != null)
            {
                if (this.registeredObjects.ContainsKey (modelObject))
                {
                    // This assert fails when processing properties that raise PropertyChanged events
                    // when a value is first fetched.  The property changed event gets hooked and
                    // the read causes the model watcher to try to register the model object again.
                    // Debug.Fail ("Model contains a cycle.");
                }
                else
                {
                    // Add the type and register for property changed events
                    this.registeredObjects.Add (modelObject, true);
                    this.AddInstance (modelObject);
                    modelObject.PropertyChanged += this.OnPropertyChanged;

                    // Examine the properties of the ModelObject to look for more ModelObjects to monitor
                    foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (modelObject))
                    {
                        // Check if the property is owned by the current ModelObject
                        if (propertyDescriptor.Attributes.Contains (RelationAttribute.Owner))
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
                                // Register for collection item add/remove/replace events
                                INotifyingCollection notifyingCollection = propertyDescriptor.GetValue (modelObject) as INotifyingCollection;
                                if (notifyingCollection != null && !this.collectionOwners.ContainsKey (notifyingCollection))
                                {
                                    notifyingCollection.ItemAdded += this.OnItemAdded;
                                    notifyingCollection.ItemRemoved += this.OnItemRemoved;
                                    notifyingCollection.ItemReplaced += this.OnItemReplaced;

                                    // Add a mapping between the collection and the object that owns it
                                    this.collectionOwners.Add (notifyingCollection, modelObject);

                                    // Recurse on ModelObject children
                                    foreach (object obj in notifyingCollection)
                                    {
                                        ModelObject childObject = obj as ModelObject;
                                        this.RegisterModelObject (childObject);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters a ModelObject with this ModelWatcher. Events from the ModelObject
        /// will no longer be monitored.
        /// </summary>
        /// <param name="modelObject">The ModelObject to unregister.</param>
        private void UnregisterModelObject (ModelObject modelObject)
        {
            // Make sure the ModelObject is currently registered
            if (modelObject != null && this.registeredObjects.ContainsKey (modelObject))
            {
                // Remove the type and unregister for property changed events
                this.registeredObjects.Remove (modelObject);
                this.RemoveInstance (modelObject);
                modelObject.PropertyChanged -= this.OnPropertyChanged;

                // Examine the properties of the ModelObject to look for more ModelObjects that are being monitored
                foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (modelObject))
                {
                    // Check if the property is owned by the current ModelObject
                    if (propertyDescriptor.Attributes.Contains (RelationAttribute.Owner))
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
                            // Unregister for collection item add/remove/replace events
                            INotifyingCollection notifyingCollection = propertyDescriptor.GetValue (modelObject) as INotifyingCollection;
                            if (notifyingCollection != null && this.collectionOwners.ContainsKey (notifyingCollection))
                            {
                                notifyingCollection.ItemAdded -= this.OnItemAdded;
                                notifyingCollection.ItemRemoved -= this.OnItemRemoved;
                                notifyingCollection.ItemReplaced -= this.OnItemReplaced;

                                // Remove the mapping between the collection and the object that owns it
                                this.collectionOwners.Remove (notifyingCollection);

                                // Recurse on ModelObject children
                                foreach (object obj in notifyingCollection)
                                {
                                    ModelObject childObject = obj as ModelObject;
                                    this.UnregisterModelObject (childObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnPropertyChanged (ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            this.UnregisterModelObject(eventArgs.OldValue as ModelObject);
            this.RaisePropertyChangedEvent(sender, eventArgs);
            this.RegisterModelObject(eventArgs.NewValue as ModelObject);
        }

        private void OnItemAdded (INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs)
        {
            this.RegisterModelObject(eventArgs.Item as ModelObject);
            this.RaiseItemAddedEvent (sender, eventArgs);
        }

        private void OnItemRemoved (INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs)
        {
            this.UnregisterModelObject(eventArgs.Item as ModelObject);
            this.RaiseItemRemovedEvent (sender, eventArgs);
        }

        private void OnItemReplaced (INotifyingCollection sender, IItemReplacedEventArgs eventArgs)
        {
            if (eventArgs.OldItem != null)
            {
                this.UnregisterModelObject(eventArgs.OldItem as ModelObject);
            }

            if (eventArgs.NewItem != null)
            {
                this.RegisterModelObject(eventArgs.NewItem as ModelObject);
            }

            this.RaiseItemReplacedEvent (sender, eventArgs);
        }
        #endregion

        #region Helpers
        private void AddInstance (ModelObject modelObject)
        {
            Type type = modelObject.GetType ();

            if (!this.instances.ContainsKey (type))
            {
                this.instances[type] = new Dictionary<ModelObject, bool> ();
            }
            this.instances[type].Add (modelObject, true);
        }

        private void RemoveInstance (ModelObject modelObject)
        {
            Type type = modelObject.GetType ();

            this.instances[type].Remove (modelObject);
            if (this.instances[type].Count == 0)
            {
                this.instances.Remove (type);
            }
        }

        private void RaisePropertyChangedEvent (ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (sender, eventArgs);
            }
        }

        private void RaiseItemAddedEvent (INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs)
        {
            if (this.ItemAdded != null)
            {
                this.ItemAdded (this.collectionOwners[sender], sender, eventArgs);
            }
        }

        private void RaiseItemRemovedEvent (INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs)
        {
            if (this.ItemRemoved != null)
            {
                this.ItemRemoved (this.collectionOwners[sender], sender, eventArgs);
            }
        }

        private void RaiseItemReplacedEvent (INotifyingCollection sender, IItemReplacedEventArgs eventArgs)
        {
            if (this.ItemReplaced != null)
            {
                this.ItemReplaced (this.collectionOwners[sender], sender, eventArgs);
            }
        }
        #endregion
    }
}
