// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Indicates the status of the asynchronous indexing.
    /// </summary>
    public enum IndexingStatus : byte
    {
        /// <summary>
        /// Indicates that indexing is in progress.
        /// </summary>
        Indexing,

        /// <summary>
        /// Indicates that indexing is idle.
        /// </summary>
        Ready
    }

    /// <summary>
    /// Provides a search engine that works with editors
    /// based on the Editor Foundation's Model design.
    /// </summary>
    /// <typeparam name="T">The type of object that will be indexed and can then be searched.</typeparam>
    public class EditorSearchEngine<T>
        : SearchEngine<EditorSearchItem>, IEditorSearchEngine
        where T : ModelRoot
    {
        #region Enumerations
        private enum IndexingOperation
        {
            Register,
            Unregister
        }
        #endregion

        #region Fields
        private IndexingStatus status;
        private ModelWatcher modelWatcher;
        private readonly Dictionary<ModelObject, IndexingOperation> indexingQueue;
        private readonly ManualResetEvent queueResetEvent;
        private readonly AsyncOperation asyncOperation;
        private readonly Dictionary<ModelObject, bool> registeredObjects;
        private readonly Dictionary<INotifyingCollection, ModelObject> collectionOwners;
        private readonly Dictionary<ModelObject, Dictionary<PropertyDescriptor, EditorSearchItem>> searchItems;

        private static readonly object requestPropertyQuery = new object ();
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new instance of the Editor Search Engine and 
        /// binds it to the specified model.
        /// </summary>
        /// <param name="model">The model to which the search engine will be bound.</param>
        public EditorSearchEngine (T model)
        {
            this.status = IndexingStatus.Ready;
            this.indexingQueue = new Dictionary<ModelObject, IndexingOperation> ();
            this.queueResetEvent = new ManualResetEvent (false);
            this.asyncOperation = AsyncOperationManager.CreateOperation (null);
            this.registeredObjects = new Dictionary<ModelObject, bool> ();
            this.collectionOwners = new Dictionary<INotifyingCollection, ModelObject> ();
            this.searchItems = new Dictionary<ModelObject, Dictionary<PropertyDescriptor, EditorSearchItem>> ();

            this.BeginIndexing ();

            this.Model = model;
        }

        /// <summary>
        /// Constructs a new instance of the Editor Search Engine that
        /// is not initially bound to any model.
        /// </summary>
        public EditorSearchEngine ()
            : this (null)
        {
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when the status of the Search Engine changes.
        /// </summary>
        public event EventHandler StatusChanged;

        /// <summary>
        /// Raised when an asynchronous search is complete.
        /// </summary>
        public event EventHandler<SearchCompleteEventArgs> SearchComplete;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the Model to which this Search Engine is bound.
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

                    this.ClearSearchItems ();
                }

                if (value != null)
                {
                    this.modelWatcher = ModelWatcher.GetSharedModelWatcher (value);
                    this.modelWatcher.PropertyChanged += this.OnPropertyChanged;
                    this.modelWatcher.ItemAdded += this.OnItemAdded;
                    this.modelWatcher.ItemRemoved += this.OnItemRemoved;
                    this.modelWatcher.ItemReplaced += this.OnItemReplaced;

                    this.EnqueueIndexingOperation (value, IndexingOperation.Register);
                }
            }
        }

        /// <summary>
        /// Gets the status of the Search Engine.
        /// </summary>
        public IndexingStatus Status
        {
            get
            {
                return this.status;
            }
            private set
            {
                if (value != this.status)
                {
                    this.status = value;

                    SendOrPostCallback raiseEvent = delegate
                    {
                        this.RaiseStatusChangedEvent ();
                    };

                    this.asyncOperation.Post (raiseEvent, null);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts an asynchronous search operation.
        /// </summary>
        /// <param name="searchCriteria">The string(s) being searched for.</param>
        /// <param name="additionalConstraints">Additional constraints to filter the search results.</param>
        public void SearchAsync (string searchCriteria, params Predicate<EditorSearchItem>[] additionalConstraints)
        {
            // Anonymous method to raise the search complete event
            SendOrPostCallback raiseEvent = delegate (object searchItems)
            {
                Debug.Assert (searchItems is EditorSearchItem[]);
                this.RaiseSearchCompleteEvent ((EditorSearchItem[])searchItems);
            };

            // Anonymous method to perform the search and post the search complete
            // event on the appropriate thread
            ThreadStart search = delegate
            {
                EditorSearchItem[] searchItems = this.Search (searchCriteria, additionalConstraints);
                this.asyncOperation.Post (raiseEvent, searchItems);
            };

            // Create a new thread and start the search
            Thread thread = new Thread (search);
            thread.Name = "Search Thread";
            thread.IsBackground = true;
            thread.Start ();
        }
        #endregion

        #region Private Methods
        // Starts a new thread that continuously indexes new search items
        // as they become available
        private void BeginIndexing ()
        {
            // An anonymous method that loops and does the
            // indexing when appropriate
            ThreadStart index = delegate
            {
                while (true)
                {
                    // Wait if there is no work to do
                    this.queueResetEvent.WaitOne ();

                    // Get the next indexing operation, if one is available
                    ModelObject modelObject = null;
                    IndexingOperation indexingOperation = IndexingOperation.Register;
                    lock (this.indexingQueue)
                    {
                        // If there are no indexing operations available, set
                        // the status to ready and wait for more work
                        if (this.indexingQueue.Count == 0)
                        {
                            this.Status = IndexingStatus.Ready;
                            this.queueResetEvent.Reset ();
                            continue;
                        }
                        // Otherwise, dequeue the next work item
                        else
                        {
                            foreach (KeyValuePair<ModelObject, IndexingOperation> entry in this.indexingQueue)
                            {
                                modelObject = entry.Key;
                                indexingOperation = entry.Value;
                                break;
                            }

                            if (modelObject != null)
                            {
                                this.indexingQueue.Remove (modelObject);
                            }
                        }
                    }

                    // Process the indexing operation
                    if (modelObject != null)
                    {
                        if (indexingOperation == IndexingOperation.Register)
                        {
                            this.RegisterModelObject (modelObject);
                        }
                        else if (indexingOperation == IndexingOperation.Unregister)
                        {
                            this.UnregisterModelObject (modelObject);
                        }
                    }
                }
            };

            // Create a new thread and start the indexing loop
            Thread thread = new Thread (index);
            thread.Name = "Search Engine Indexer";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start ();
        }

        private void EnqueueIndexingOperation (ModelObject modelObject, IndexingOperation indexingOperation)
        {
            lock (this.indexingQueue)
            {
                IndexingOperation pendingOperation;
                if (this.indexingQueue.TryGetValue (modelObject, out pendingOperation))
                {
                    if (indexingOperation != pendingOperation)
                    {
                        this.indexingQueue.Remove (modelObject);
                    }
                }
                else
                {
                    this.indexingQueue.Add (modelObject, indexingOperation);
                }
            }

            this.Status = IndexingStatus.Indexing;
            this.queueResetEvent.Set ();
        }

        // When a single property changes, just index it synchronously since it is very fast
        private void OnPropertyChanged (ModelObject sender, UndoablePropertyChangedEventArgs args)
        {
            PropertyDescriptor propertyDescriptor = Utilities.GetPropertyDescriptor (sender, args.PropertyName);

            if (args.OldValue != null)
            {
                this.UnregisterProperty (sender, propertyDescriptor, args.OldValue);
            }

            if (args.NewValue != null)
            {
                this.RegisterProperty (sender, propertyDescriptor, args.NewValue);
            }
        }

        // When an item is added, there could be any number of new items to index,
        // so enqueue a new indexing operation that will be processed asynchronously
        private void OnItemAdded (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs e)
        {
            if (e.Item is ModelObject)
            {
                this.EnqueueIndexingOperation ((ModelObject)e.Item, IndexingOperation.Register);
            }
        }

        // When an item is removed, there could be any number of items to remove from the index,
        // so enqueue a new indexing operation that will be processed asynchronously
        private void OnItemRemoved (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs e)
        {
            if (e.Item is ModelObject)
            {
                this.EnqueueIndexingOperation ((ModelObject)e.Item, IndexingOperation.Unregister);
            }
        }

        // When an item is added, there could be any number of new items to index
        // and old items to remove from the index, 
        // so enqueue a new indexing operation that will be processed asynchronously
        private void OnItemReplaced (ModelObject parentObject, INotifyingCollection sender, IItemReplacedEventArgs e)
        {
            if (e.OldItem is ModelObject)
            {
                this.EnqueueIndexingOperation ((ModelObject)e.OldItem, IndexingOperation.Unregister);
            }
            if (e.NewItem is ModelObject)
            {
                this.EnqueueIndexingOperation ((ModelObject)e.NewItem, IndexingOperation.Register);
            }
        }

        private void RegisterModelObject (ModelObject modelObject)
        {
            // Make sure the ModelObject hasn't already been processed
            if (modelObject != null && !this.registeredObjects.ContainsKey (modelObject))
            {
                // Make sure the ModelObject is searchable
                if (this.GetSearchableAttribute (modelObject.GetType ()).KeywordProvider != null)
                {
                    // Add the object to the list of already processed objects
                    this.registeredObjects.Add (modelObject, true);

                    // Examine the properties of the ModelObject to look for more ModelObjects to process
                    foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (modelObject))
                    {
                        // Process the property
                        if (this.RegisterProperty (modelObject, propertyDescriptor))
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
                                if (notifyingCollection != null && !this.collectionOwners.ContainsKey (notifyingCollection))
                                {
                                    this.collectionOwners.Add (notifyingCollection, modelObject);

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
        }

        private void UnregisterModelObject (ModelObject modelObject)
        {
            // Make sure the object was previously processed
            if (modelObject != null && this.registeredObjects.ContainsKey (modelObject))
            {
                // Remove the object from the list of already processed objects
                this.registeredObjects.Remove (modelObject);

                // Examine the properties of the ModelObject to look for more ModelObjects to monitor
                foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (modelObject))
                {
                    // Remove the property
                    if (this.UnregisterProperty (modelObject, propertyDescriptor))
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
                            if (notifyingCollection != null && this.collectionOwners.ContainsKey (notifyingCollection))
                            {
                                this.collectionOwners.Remove (notifyingCollection);

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

        private bool RegisterProperty (ModelObject modelObject, PropertyDescriptor propertyDescriptor)
        {
            return this.RegisterProperty (modelObject, propertyDescriptor, EditorSearchEngine<T>.requestPropertyQuery);
        }

        private bool RegisterProperty (ModelObject modelObject, PropertyDescriptor propertyDescriptor, object value)
        {
            SearchableAttribute searchableAttribute = null;

            if (value == EditorSearchEngine<T>.requestPropertyQuery)
            {
                // Querying for the property value can be slow, so don't do it unless the property is searchable
                searchableAttribute = this.GetSearchableAttribute (propertyDescriptor);
                if (searchableAttribute.KeywordProvider != null)
                {
                    value = propertyDescriptor.GetValue (modelObject);
                }
                else
                {
                    value = null;
                }
            }

            // Querying for the search attribute can be slow, so don't do it unless the property value is non-null
            if (value != null)
            {
                if (searchableAttribute == null)
                {
                    searchableAttribute = this.GetSearchableAttribute (propertyDescriptor);
                }

                if (searchableAttribute.KeywordProvider != null)
                {
                    this.AddTerms (this.GetSearchItem (modelObject, propertyDescriptor), searchableAttribute.KeywordProvider.GetKeywords (value));
                    return true;
                }
            }

            return false;
        }

        private bool UnregisterProperty (ModelObject modelObject, PropertyDescriptor propertyDescriptor)
        {
            return this.UnregisterProperty (modelObject, propertyDescriptor, EditorSearchEngine<T>.requestPropertyQuery);
        }

        private bool UnregisterProperty (ModelObject modelObject, PropertyDescriptor propertyDescriptor, object value)
        {
            SearchableAttribute searchableAttribute = null;

            if (value == EditorSearchEngine<T>.requestPropertyQuery)
            {
                // Querying for the property value can be slow, so don't do it unless the property is searchable
                searchableAttribute = this.GetSearchableAttribute (propertyDescriptor);
                if (searchableAttribute.KeywordProvider != null)
                {
                    value = propertyDescriptor.GetValue (modelObject);
                }
                else
                {
                    value = null;
                }
            }

            // Querying for the search attribute can be slow, so don't do it unless the property value is non-null
            if (value != null)
            {
                if (searchableAttribute == null)
                {
                    searchableAttribute = this.GetSearchableAttribute (propertyDescriptor);
                }

                if (searchableAttribute.KeywordProvider != null)
                {
                    this.RemoveTerms (this.GetSearchItem (modelObject, propertyDescriptor), searchableAttribute.KeywordProvider.GetKeywords (value));
                    this.DisposeSearchItem (modelObject, propertyDescriptor);
                    return true;
                }
            }

            return false;
        }

        private void ClearSearchItems ()
        {
            lock (this.searchItems)
            {
                foreach (Dictionary<PropertyDescriptor, EditorSearchItem> searchItemsForObject in this.searchItems.Values)
                {
                    foreach (EditorSearchItem searchItem in searchItemsForObject.Values)
                    {
                        searchItem.Dispose ();
                    }
                }

                this.searchItems.Clear ();
            }
            this.ClearIndex ();
        }

        private EditorSearchItem GetSearchItem (ModelObject modelObject, PropertyDescriptor propertyDescriptor)
        {
            Dictionary<PropertyDescriptor, EditorSearchItem> searchItemsForObject;
            lock (this.searchItems)
            {
                if (!this.searchItems.TryGetValue (modelObject, out searchItemsForObject))
                {
                    searchItemsForObject = new Dictionary<PropertyDescriptor, EditorSearchItem> ();
                    this.searchItems.Add (modelObject, searchItemsForObject);
                }
            }

            EditorSearchItem searchItem;
            lock (searchItemsForObject)
            {
                if (!searchItemsForObject.TryGetValue (propertyDescriptor, out searchItem))
                {
                    searchItem = new EditorSearchItem (modelObject, propertyDescriptor);
                    searchItemsForObject.Add (propertyDescriptor, searchItem);
                }
            }

            return searchItem;
        }

        private void DisposeSearchItem (ModelObject modelObject, PropertyDescriptor propertyDescriptor)
        {
            Dictionary<PropertyDescriptor, EditorSearchItem> searchItemsForObject;
            lock (this.searchItems)
            {
                if (this.searchItems.TryGetValue (modelObject, out searchItemsForObject))
                {
                    lock (searchItemsForObject)
                    {
                        EditorSearchItem searchItem;
                        if (searchItemsForObject.TryGetValue (propertyDescriptor, out searchItem))
                        {
                            searchItem.Dispose ();
                            searchItemsForObject.Remove (propertyDescriptor);

                            if (searchItemsForObject.Count == 0)
                            {
                                this.searchItems.Remove (modelObject);
                            }
                        }
                    }
                }
            }
        }

        // Gets a search attribute for the property. The attribute source can be the
        // Property itself, the Type of the property, or the Default value of the search
        // attribute. The priority of these sources is: Property, Type, Default
        private SearchableAttribute GetSearchableAttribute (PropertyDescriptor propertyDescriptor)
        {
            // Check the Property
            SearchableAttribute searchableAttribute = this.GetSearchableAttribute (propertyDescriptor.Attributes);
            if (searchableAttribute != SearchableAttribute.Default)
            {
                return searchableAttribute;
            }

            // Check the Type
            return this.GetSearchableAttribute (propertyDescriptor.PropertyType);
        }

        private SearchableAttribute GetSearchableAttribute (Type type)
        {
            return this.GetSearchableAttribute (TypeDescriptor.GetAttributes (type));
        }

        private SearchableAttribute GetSearchableAttribute (AttributeCollection attributes)
        {
            return (SearchableAttribute)attributes[typeof (SearchableAttribute)];
        }

        private void RaiseStatusChangedEvent ()
        {
            if (this.StatusChanged != null)
            {
                this.StatusChanged (this, EventArgs.Empty);
            }
        }

        private void RaiseSearchCompleteEvent (EditorSearchItem[] searchItems)
        {
            if (this.SearchComplete != null)
            {
                this.SearchComplete (this, new SearchCompleteEventArgs (searchItems));
            }
        }
        #endregion
    }
}
