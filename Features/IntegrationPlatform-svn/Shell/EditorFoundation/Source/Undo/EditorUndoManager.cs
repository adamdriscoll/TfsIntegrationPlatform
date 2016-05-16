// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// An Undo Manager specifically for Models.
    /// </summary>
    public class EditorUndoManager<T>
        : UndoManager, IEditorUndoManager
        where T : ModelRoot
    {
        #region Fields
        private ModelWatcher modelWatcher;
        private TimeSpan groupingTimeout;
        #endregion

        #region Constructors & Destructors
        /// <summary>
        /// Initializes a new instance of the EditorUndoManager class.
        /// </summary>
        /// <param name="model">The Model to which this EditorUndoManager will be bound.</param>
        public EditorUndoManager (T model)
            : base ()
        {
            this.groupingTimeout = TimeSpan.FromMilliseconds (100);
            this.Model = model;
        }

        /// <summary>
        /// Initializes a new instance of the EditorUndoManager class.
        /// </summary>
        public EditorUndoManager ()
            : this (null)
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the Model to which this UndoManager is attached.
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
                }

                this.Reset ();

                if (value != null)
                {
                    this.modelWatcher = ModelWatcher.GetSharedModelWatcher (value);
                    this.modelWatcher.PropertyChanged += this.OnPropertyChanged;
                    this.modelWatcher.ItemAdded += this.OnItemAdded;
                    this.modelWatcher.ItemRemoved += this.OnItemRemoved;
                    this.modelWatcher.ItemReplaced += this.OnItemReplaced;
                }
            }
        }

        /// <summary>
        /// Gets or sets the time interval between consecutive IUndoable
        /// additions that will determine default grouping behavior.
        /// </summary>
        public TimeSpan GroupingTimeout
        {
            get
            {
                return this.groupingTimeout;
            }
            set
            {
                this.groupingTimeout = value;
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Adds the specified IUndoable to this UndoManager.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this UndoManager.</param>
        protected override void AddUndoable (IUndoable undoable)
        {
            // If there is no current undoable or the timeout on the current undoable temporal group has expired,
            // create a new undoable temporal group before adding the specified IUndoable
            if (this.CurrentUndoable == null || !(this.CurrentUndoable is UndoableTemporalGroup) || !((UndoableTemporalGroup)this.CurrentUndoable).CanContain (undoable))
            {
                base.AddUndoable (new UndoableTemporalGroup (this.GroupingTimeout));
            }
            base.AddUndoable (undoable);
        }
        #endregion

        #region Private Methods

        #region Object Model Discovery
        private void OnPropertyChanged (ModelObject sender, UndoablePropertyChangedEventArgs args)
        {
            this.RecordPropertyChanged (sender, args.PropertyName, args.OldValue, args.NewValue);
        }

        private void OnItemAdded (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs e)
        {
            this.RecordObjectAdded (sender, e.Item, e.Index);
        }

        private void OnItemRemoved (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs e)
        {
            this.RecordObjectRemoved (sender, e.Item, e.Index);
        }

        private void OnItemReplaced (ModelObject parentObject, INotifyingCollection sender, IItemReplacedEventArgs e)
        {
            if (e.OldItem != null)
            {
                this.RecordObjectRemoved (sender, e.OldItem, e.Index);
            }
            if (e.NewItem != null)
            {
                this.RecordObjectAdded (sender, e.NewItem, e.Index);
            }
        }
        #endregion

        #region Object Model Change Handling
        private void RecordPropertyChanged (ModelObject modelObject, string propertyName, object oldValue, object newValue)
        {
            this.Add (new UndoablePropertyChange (modelObject, propertyName, oldValue, newValue));
        }

        private void RecordObjectAdded (INotifyingCollection parent, object modelObject, int index)
        {
            if (modelObject != null)
            {
                this.Add (new UndoableItemAddedChange (parent, modelObject, index));
            }
        }

        private void RecordObjectRemoved (INotifyingCollection parent, object modelObject, int index)
        {
            if (modelObject != null)
            {
                this.Add (new UndoableItemRemovedChange (parent, modelObject, index));
            }
        }
        #endregion

        #endregion
    }
}
