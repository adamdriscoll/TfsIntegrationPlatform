// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.Globalization;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Shell.Search;
using Microsoft.TeamFoundation.Migration.Shell.Undo;
using Microsoft.TeamFoundation.Migration.Shell.Validation;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{   
    /// <summary>
    /// Provides view-independent logic and data in a form that
    /// is easily consumable by particular view implementations.
    /// For example, the ViewModel could be used by either a WinForms
    /// based view implementation or a WPF based view implementation.
    /// </summary>
    public class ViewModel<TController, TModel> : INotifyPropertyChanged
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly TController controller = new TController();

        private ModelWatcher modelWatcher;
        private int lastSavedUndoableKey;
        private bool changedSinceLastSave;

        private FileSystemWatcher currentFileWatcher;

        private string applicationName;
        private string applicationVersion;
        private string applicationCopyright;

        private bool isOpening = false;
        private bool isSaving = false;
        private bool isClosing = false;
        private bool isSearching = false;

        private bool allowCreate = true;
        private bool allowOpen = true;
        private bool allowClose = true;
        private bool allowSave = true;

        private bool allowUndoRedo;
        private bool allowSearch;
        private bool allowValidation;

        private readonly IList<StatusEvent> statusEventsKey;
        private readonly PrivateCollection<StatusEvent> statusEvents;
        private readonly RecentFilesCollection recentFiles;
        private readonly SearchResultCollection searchResults;
        private readonly ValidationResultCollection validationResults;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModel&lt;TController, TModel&gt;"/> class.
        /// </summary>
        public ViewModel ()
        {
            // Initialize service availability
            this.allowUndoRedo = this.IsUndoRedoAvailable;
            this.allowSearch = this.IsSearchAvailable;
            this.allowValidation = this.IsValidationAvailable;

            // Subscribe to controller events
            this.Controller.Created += this.OnCreated;
            this.Controller.Opening += this.OnOpening;
            this.Controller.Opened += this.OnOpened;
            this.Controller.Closing += this.OnClosing;
            this.Controller.Closed += this.OnClosed;
            this.Controller.Saving += this.OnSaving;
            this.Controller.Saved += this.OnSaved;

            // Initialize status events list
            this.statusEvents = new PrivateCollection<StatusEvent> (out this.statusEventsKey);

            // Initialize recent files list
            this.recentFiles = new RecentFilesCollection (this.Controller);

            // Attach to udno manager
            if (this.IsUndoRedoAvailable)
            {
                this.UndoManager.AfterUndo += this.OnAfterUndo;
                this.UndoManager.AfterRedo += this.OnAfterRedo;
            }

            // Attach to search engine
            if (this.IsSearchAvailable)
            {
                this.SearchEngine.StatusChanged += this.OnSearchEngineStatusChanged;
                this.SearchEngine.SearchComplete += this.OnSearchEngineSearchComplete;
                this.searchResults = new SearchResultCollection (this.SearchEngine);
            }

            // Attach to validation manager
            if (this.IsValidationAvailable)
            {
                this.ValidationManager.StatusChanged += this.OnValidationManagerStatusChanged;
                this.validationResults = new ValidationResultCollection (this.ValidationManager);
            }

            // Get the default application name using the assembly level AssemblyProductAttribute,
            // which is usually defined in the AssemblyInfo.cs file.
            Assembly entryAssembly = Assembly.GetEntryAssembly ();
            if (entryAssembly != null)
            {
                object[] assemblyProductAttributes = entryAssembly.GetCustomAttributes (typeof (AssemblyProductAttribute), false);
                if ((assemblyProductAttributes != null) && (assemblyProductAttributes.Length > 0))
                {
                    this.applicationName = ((AssemblyProductAttribute)assemblyProductAttributes[0]).Product;
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Occurs when the content of the currently opened file changes.
        /// </summary>
        public event EventHandler CurrentFileContentChanged;

        ///// <summary>
        ///// Occurs when the loaded model is being closed and has unsaved changes.
        ///// </summary>
        //public event EventHandler<CancelEventArgs> ClosingWithUnsavedChanges;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the controller.
        /// </summary>
        /// <value>The controller.</value>
        public TController Controller
        {
            get
            {
                return this.controller;
            }
        }

        /// <summary>
        /// Gets the data model.
        /// </summary>
        /// <remarks>
        /// The data model can also be retrieved from Controller.Model.
        /// However, the ViewModel automatically raises a PropertyChangedEvent
        /// when the data model changes, which makes it ideal for binding to a UI.
        /// </remarks>
        /// <value>The data model.</value>
        public TModel DataModel
        {
            get
            {
                return this.Controller.Model;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a data model is loaded and has unsaved changes.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if there are unsaved changes; otherwise, <c>false</c>.
        /// </value>
        public bool HasUnsavedChanges
        {
            get
            {
                bool hasUnsavedChanges = false;

                if (this.IsDataModelLoaded)
                {
                    if (this.IsUndoRedoAvailable)
                    {
                        hasUnsavedChanges = this.UndoManager.CurrentUndoableKey != this.lastSavedUndoableKey;
                    }
                    else
                    {
                        hasUnsavedChanges = this.changedSinceLastSave;
                    }
                }

                return hasUnsavedChanges;
            }
        }

        /// <summary>
        /// Gets the collection of all status events that have occurred.
        /// </summary>
        public PrivateCollection<StatusEvent> StatusEvents
        {
            get
            {
                return this.statusEvents;
            }
        }

        /// <summary>
        /// Gets the key for the status events collection, which provides write access to the collection.
        /// </summary>
        protected IList<StatusEvent> StatusEventsKey
        {
            get
            {
                return this.statusEventsKey;
            }
        }

        /// <summary>
        /// Gets the list of recently opened files.
        /// </summary>
        public RecentFilesCollection RecentFiles
        {
            get
            {
                return this.recentFiles;
            }
        }

        /// <summary>
        /// Gets the Undo Manager, which is maintained by the Controller
        /// </summary>
        public EditorUndoManager<TModel> UndoManager
        {
            get
            {
                return this.Controller.UndoManager;
            }
        }

        /// <summary>
        /// Gets the Search Engine, which is maintained by the Controller
        /// </summary>
        public EditorSearchEngine<TModel> SearchEngine
        {
            get
            {
                return this.Controller.SearchEngine;
            }
        }

        /// <summary>
        /// Gets the Validation Manager, which is maintained by the Controller
        /// </summary>
        public EditorValidationManager<TModel> ValidationManager
        {
            get
            {
                return this.Controller.ValidationManager;
            }
        }

        /// <summary>
        /// Gets the Plugin Manager, which is maintained by the Controller
        /// </summary>
        public PluginManager PluginManager
        {
            get
            {
                return this.Controller.PluginManager;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is opening a data model.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if opening; otherwise, <c>false</c>.
        /// </value>
        public bool IsOpening
        {
            get
            {
                return this.isOpening;
            }
            private set 
            {
                this.isOpening = value;
                this.OnIsOpeningChanged ();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is saving a data model.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if saving; otherwise, <c>false</c>.
        /// </value>
        public bool IsSaving
        {
            get
            {
                return this.isSaving;
            }
            private set
            {
                this.isSaving = value;
                this.OnIsSavingChanged ();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is closing a data model.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if closing; otherwise, <c>false</c>.
        /// </value>
        public bool IsClosing
        {
            get
            {
                return this.isClosing;
            }
            private set
            {
                this.isClosing = value;
                this.OnIsClosingChanged ();
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is indexing a data model.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if indexing; otherwise, <c>false</c>.
        /// </value>
        public bool IsIndexing
        {
            get
            {
                return this.IsSearchAvailable && this.SearchEngine.Status == IndexingStatus.Indexing;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is searching a data model.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if searching; otherwise, <c>false</c>.
        /// </value>
        public bool IsSearching
        {
            get
            {
                return this.isSearching;
            }
            private set
            {
                this.isSearching = value;
                this.OnIsSearchingChanged ();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is validating a data model.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if validating; otherwise, <c>false</c>.
        /// </value>
        public bool IsValidating
        {
            get
            {
                return this.IsValidationAvailable && this.ValidationManager.Status == ValidationStatus.Validating;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ViewModel&lt;TController, TModel&gt;"/> is currently performing a synchronous or asynchronous task.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if busy; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsBusy
        {
            get
            {
                return this.IsOpening || this.IsSaving || this.IsClosing || this.IsIndexing || this.IsSearching || this.IsValidating;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a data model is currently loaded.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a data model is loaded; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataModelLoaded
        {
            get
            {
                return this.DataModel != null;
            }
        }

        /// <summary>
        /// Gets the full path to the application.
        /// </summary>
        /// <value>The path to the application.</value>
        public string ApplicationPath
        {
            get
            {
                return Assembly.GetEntryAssembly ().Location;
            }
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        public string ApplicationName
        {
            get
            {
                return this.applicationName;
            }
            set
            {
                if (value != this.applicationName)
                {
                    this.applicationName = value;
                    this.OnApplicationNameChanged ();
                }
            }
        }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        /// <value>The version of the application.</value>
        public string ApplicationVersion
        {
            get
            {
                if (!string.IsNullOrEmpty (this.applicationVersion))
                {
                    return this.applicationVersion;
                }
                else
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly ();
                    if (entryAssembly != null)
                    {
                        object[] assemblyInformationVersionAttributes = entryAssembly.GetCustomAttributes (typeof (AssemblyInformationalVersionAttribute), false);
                        if (assemblyInformationVersionAttributes != null && assemblyInformationVersionAttributes.Length > 0)
                        {
                            this.applicationVersion = ((AssemblyInformationalVersionAttribute)assemblyInformationVersionAttributes[0]).InformationalVersion;
                        }
                    }

                    if (string.IsNullOrEmpty (this.applicationVersion))
                    {
                        this.applicationVersion = "1.0.0.0";
                    }

                    return this.applicationVersion;
                }
            }
        }

        /// <summary>
        /// Gets the copyright information for the application.
        /// </summary>
        /// <value>The copyright information.</value>
        public string ApplicationCopyright
        {
            get
            {
                if (!string.IsNullOrEmpty (this.applicationCopyright))
                {
                    return this.applicationCopyright;
                }
                else
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly ();
                    if (entryAssembly != null)
                    {
                        object[] copyrightAttributes = entryAssembly.GetCustomAttributes (typeof (AssemblyCopyrightAttribute), false);
                        if (copyrightAttributes != null && copyrightAttributes.Length > 0)
                        {
                            this.applicationCopyright = ((AssemblyCopyrightAttribute)copyrightAttributes[0]).Copyright;
                        }
                    }

                    return this.applicationCopyright;
                }
            }
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <remarks>
        /// The title is commonly used as a window title.
        /// </remarks>
        /// <value>The title.</value>
        public virtual string Title
        {
            get
            {
                if (string.IsNullOrEmpty (this.CurrentFileName))
                {
                    return this.ApplicationName;
                }
                else
                {                    
                    return string.Format ("{0}{1} - {2}", this.CurrentFileName, this.HasUnsavedChanges ? "*" : string.Empty, this.ApplicationName);
                }
            }
        }

        /// <summary>
        /// Gets the full path to the file that is currently loaded,
        /// or the empty string if no file is loaded.
        /// </summary>
        /// <value>The current file path.</value>
        public string CurrentFilePath
        {
            get
            {
                return this.Controller.SavePath ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the name of the file that is currently loaded,
        /// or the empty string if no file is loaded.
        /// </summary>
        /// <value>The name of the current file.</value>
        public string CurrentFileName
        {
            get
            {
                return Path.GetFileName (this.CurrentFilePath);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether creating a new file is allowed.
        /// </summary>
        /// <value><c>true</c> if creating a new file is allowed; otherwise, <c>false</c>.</value>
        public bool AllowCreate
        {
            get
            {
                return this.allowCreate;
            }
            set
            {
                if (value != this.allowCreate)
                {
                    this.allowCreate = value;
                    this.OnAllowCreateChanged ();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a new file can be created at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a new file can be created; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanCreate
        {
            get
            {
                return this.AllowCreate;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether opening a new file is allowed.
        /// </summary>
        /// <value><c>true</c> if opening a new file is allowed; otherwise, <c>false</c>.</value>
        public bool AllowOpen
        {
            get
            {
                return this.allowOpen;
            }
            set
            {
                if (value != this.allowOpen)
                {
                    this.allowOpen = value;
                    this.OnAllowOpenChanged ();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a new file can be opened at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a new file can be opened; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanOpen
        {
            get
            {
                return this.AllowOpen;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether closing a new file is allowed.
        /// </summary>
        /// <value><c>true</c> if closing a new file is allowed; otherwise, <c>false</c>.</value>
        public bool AllowClose
        {
            get
            {
                return this.allowClose;
            }
            set
            {
                if (value != this.allowClose)
                {
                    this.allowClose = value;
                    this.OnAllowCloseChanged ();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a new file can be closed at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a new file can be closed; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanClose
        {
            get
            {
                return this.AllowClose && this.IsDataModelLoaded;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether saving an existing file is allowed.
        /// </summary>
        /// <value><c>true</c> if saving an existing file is allowed; otherwise, <c>false</c>.</value>
        public bool AllowSave
        {
            get
            {
                return this.allowSave;
            }
            set
            {
                if (value != this.allowSave)
                {
                    this.allowSave = value;
                    this.OnAllowSaveChanged ();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current file can be saved at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the current file can be saved; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanSave
        {
            get
            {
                return this.AllowSave && this.IsDataModelLoaded;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the undo service is available.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if undo/redo is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsUndoRedoAvailable
        {
            get
            {
                return this.UndoManager != null;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether undo/redo is allowed.
        /// </summary>
        /// <value><c>true</c> if undo/redo is allowed; otherwise, <c>false</c>.</value>
        public bool AllowUndoRedo
        {
            get
            {
                return this.allowUndoRedo;
            }
            set
            {
                if (value != this.allowUndoRedo)
                {
                    if (value && !this.IsUndoRedoAvailable)
                    {
                        throw new InvalidOperationException ("Search is not allowed when no Search Engine is available.");
                    }

                    this.allowUndoRedo = value;
                    this.OnAllowUndoRedoChanged ();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether an undo operation can be initiated at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if an undo operation can be initiated; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanUndo
        {
            get
            {
                return this.AllowUndoRedo && this.IsDataModelLoaded && this.IsUndoRedoAvailable && this.UndoManager.CanUndo;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a redo operation can be initiated at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a redo operation can be initiated; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanRedo
        {
            get
            {
                return this.AllowUndoRedo && this.IsDataModelLoaded && this.IsUndoRedoAvailable && this.UndoManager.CanRedo;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the search service is available.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if search is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsSearchAvailable
        {
            get
            {
                return this.SearchEngine != null;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether searching is allowed.
        /// </summary>
        /// <value><c>true</c> if searching is allowed; otherwise, <c>false</c>.</value>
        public bool AllowSearch
        {
            get
            {
                return this.allowSearch;
            }
            set
            {
                if (value != this.allowSearch)
                {
                    if (value && !this.IsSearchAvailable)
                    {
                        throw new InvalidOperationException ("Search is not allowed when no Search Engine is available.");
                    }

                    this.allowSearch = value;
                    this.OnAllowSearchChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a search can be initiated at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a search can be initiated; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanSearch
        {
            get
            {
                return this.AllowSearch && this.IsDataModelLoaded && this.IsSearchAvailable && this.SearchEngine.Status == IndexingStatus.Ready;
            }
        }

        /// <summary>
        /// Gets the current set of search results.
        /// </summary>
        public SearchResultCollection SearchResults
        {
            get
            {
                return this.searchResults;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the data validation service is available.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if validation is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsValidationAvailable
        {
            get
            {
                return this.ValidationManager != null;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether data validation is allowed.
        /// </summary>
        /// <value><c>true</c> if validation is allowed; otherwise, <c>false</c>.</value>
        public bool AllowValidation
        {
            get
            {
                return this.allowValidation;
            }
            set
            {
                if (value != this.allowValidation)
                {
                    if (value && !this.IsValidationAvailable)
                    {
                        throw new InvalidOperationException ("Validation is not allowed when no Validation Manager is available.");
                    }

                    this.allowValidation = value;
                    this.OnAllowValidationChanged ();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether validation can be initiated at this time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if validation can be initiated; otherwise, <c>false</c>.
        /// </value>
        public virtual bool CanValidate
        {
            get
            {
                return this.AllowValidation && this.IsDataModelLoaded && this.IsValidationAvailable;
            }
        }

        /// <summary>
        /// Gets the current set of validation results.
        /// </summary>
        public ValidationResultCollection ValidationResults
        {
            get
            {
                return this.validationResults;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a new file.
        /// </summary>
        public virtual bool Create ()
        {
            if (this.CanCreate)
            {
                return this.Controller.Create ();
            }

            return false;
        }

        /// <summary>
        /// Opens a file at the specified path.
        /// </summary>
        /// <param name="path">The path to the file from which to open.</param>
        public virtual void Open (string path)
        {
            if (this.CanOpen)
            {
                this.Controller.OpenAsync (path);
            }
        }

        /// <summary>
        /// Opens a file from the specified stream.
        /// </summary>
        /// <param name="stream">The stream from which to open.</param>
        public virtual void Open (Stream stream)
        {
            if (this.CanOpen)
            {
                this.Controller.OpenAsync (stream);
            }
        }

        /// <summary>
        /// Closes the current file.
        /// </summary>
        public virtual bool Close ()
        {
            if (this.CanClose)
            {
                return this.Controller.Close ();
            }

            return false;
        }

        /// <summary>
        /// Saves the current file to the specified path.
        /// </summary>
        /// <param name="path">The path to the file to which to save.</param>
        public virtual bool Save (string path)
        {
            if (this.CanSave)
            {
                return this.Controller.Save (path);
            }

            return false;
        }

        /// <summary>
        /// Saves the current file to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to which to save.</param>
        public virtual bool Save (Stream stream)
        {
            if (this.CanSave)
            {
                return this.Controller.Save (stream);
            }

            return false;
        }

        /// <summary>
        /// Initiates an undo operation.
        /// </summary>
        public virtual void Undo ()
        {
            if (this.CanUndo)
            {
                this.UndoManager.Undo ();
            }
        }

        /// <summary>
        /// Initiates a redo operation.
        /// </summary>
        public virtual void Redo ()
        {
            if (this.CanRedo)
            {
                this.UndoManager.Redo ();
            }
        }

        /// <summary>
        /// Initiates an asynchronous search.
        /// </summary>
        /// <param name="searchString">The string for which to search.</param>
        public virtual void Search (string searchString)
        {
            if (this.CanSearch)
            {
                // TODO: IsSearching should really be set in a SearchEngine.SearchStarted event handler
                this.IsSearching = true;
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "SearchStartedString"));
                this.SearchEngine.SearchAsync (searchString);
            }
        }

        /// <summary>
        /// Initiates an asynchronous validation.
        /// </summary>
        public virtual void Validate ()
        {
            if (this.CanValidate)
            {
                this.ValidationManager.Validate ();
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Adds a status event object.
        /// </summary>
        /// <param name="statusEvent">The status event.</param>
        protected void AddStatusEvent (object statusEvent)
        {
            this.statusEventsKey.Insert (0, new StatusEvent (statusEvent));
        }

        /// <summary>
        /// Adds a status event string.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">A <see cref="System.Object"/> array containing zero or more objects to format.</param>
        protected void AddStatusEvent (string format, params object[] args)
        {
            this.statusEventsKey.Insert (0, new StatusEvent (string.Format (format, args)));
        }

        /// <summary>
        /// Adds a mutable status event.
        /// </summary>
        /// <param name="statusEvent">The status event.</param>
        protected void AddStatusEvent (IMutable statusEvent)
        {
            this.statusEventsKey.Insert (0, new StatusEvent (statusEvent));
        }

        /// <summary>
        /// Called when the IsOpening state changes.
        /// </summary>
        protected virtual void OnIsOpeningChanged ()
        {
            this.RaiseIsOpeningChanged ();
            this.RaiseIsBusyChanged ();
        }

        /// <summary>
        /// Called when the IsSaving state changes.
        /// </summary>
        protected virtual void OnIsSavingChanged ()
        {
            this.RaiseIsSavingChanged ();
            this.RaiseIsBusyChanged ();
        }

        /// <summary>
        /// Called when the IsClosing state changes.
        /// </summary>
        protected virtual void OnIsClosingChanged ()
        {
            this.RaiseIsClosingChanged ();
            this.RaiseIsBusyChanged ();
        }

        /// <summary>
        /// Called when the IsIndexing state changes.
        /// </summary>
        protected virtual void OnIsIndexingChanged ()
        {
            this.RaiseIsIndexingChanged ();
            this.RaiseIsBusyChanged ();

            if (this.SearchEngine.Status == IndexingStatus.Indexing)
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "IndexingStartedString"));
            }
            else
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "IndexingCompleteString"));
            }
        }

        /// <summary>
        /// Called when the IsSearching state changes.
        /// </summary>
        protected virtual void OnIsSearchingChanged ()
        {
            this.RaiseIsSearchingChanged ();
            this.RaiseIsBusyChanged ();
        }

        /// <summary>
        /// Called when the IsValidating state changes.
        /// </summary>
        protected virtual void OnIsValidatingChanged ()
        {
            this.RaiseIsValidatingChanged ();
            this.RaiseIsBusyChanged ();

            if (this.ValidationManager.Status == ValidationStatus.Validating)
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "ValidationStartedString"));
            }
            else
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "ValidationCompleteString"));
            }
        }

        /// <summary>
        /// Called when the data model changes.
        /// </summary>
        protected virtual void OnDataModelChanged ()
        {
            if (this.DataModel != null)
            {
                this.modelWatcher = ModelWatcher.GetSharedModelWatcher (this.DataModel);
                this.modelWatcher.ItemAdded += this.OnDataModelItemAdded;
                this.modelWatcher.ItemRemoved += this.OnDataModelItemRemoved;
                this.modelWatcher.ItemReplaced += this.OnDataModelItemReplaced;
                this.modelWatcher.PropertyChanged += this.OnDataModelPropertyChanged;
            }
            else if (this.modelWatcher != null)
            {
                this.modelWatcher.ItemAdded -= this.OnDataModelItemAdded;
                this.modelWatcher.ItemRemoved -= this.OnDataModelItemRemoved;
                this.modelWatcher.ItemReplaced -= this.OnDataModelItemReplaced;
                this.modelWatcher.PropertyChanged -= this.OnDataModelPropertyChanged;
                this.modelWatcher = null;
            }

            this.RaiseDataModelChangedEvent ();
            this.RaiseIsDataModelLoadedEvent ();
            this.RaiseCanSaveChangedEvent ();
            this.RaiseCanSearchChangedEvent ();
            this.RaiseCanValidateChangedEvent ();
        }

        /// <summary>
        /// Called when some internal aspect of the data model changes.
        /// </summary>
        protected virtual void OnDataModelInternalsChanged ()
        {
            this.changedSinceLastSave = true;
            this.OnHasUnsavedChangesChanged ();
        }

        /// <summary>
        /// Called when the current file changes.
        /// </summary>
        protected virtual void OnCurrentFileChanged ()
        {
            this.RaiseCurrentFilePathChangedEvent ();
            this.RaiseCurrentFileNameChangedEvent ();
            this.RaiseTitleChangedEvent ();
        }

        /// <summary>
        /// Called when the application name changes.
        /// </summary>
        protected virtual void OnApplicationNameChanged ()
        {
            this.RaiseApplicationNameChangedEvent ();
            this.RaiseTitleChangedEvent ();
        }

        /// <summary>
        /// Called when allow create changes.
        /// </summary>
        protected virtual void OnAllowCreateChanged ()
        {
            this.RaiseAllowCreateChangedEvent ();
            this.RaiseCanCreateChangedEvent ();
        }

        /// <summary>
        /// Called when allow open changes.
        /// </summary>
        protected virtual void OnAllowOpenChanged ()
        {
            this.RaiseAllowOpenChangedEvent ();
            this.RaiseCanOpenChangedEvent ();
        }

        /// <summary>
        /// Called when allow close changes.
        /// </summary>
        protected virtual void OnAllowCloseChanged ()
        {
            this.RaiseAllowCloseChangedEvent ();
            this.RaiseCanCloseChangedEvent ();
        }

        /// <summary>
        /// Called when allow save changes.
        /// </summary>
        protected virtual void OnAllowSaveChanged ()
        {
            this.RaiseAllowSaveChangedEvent ();
            this.RaiseCanSaveChangedEvent ();
        }

        /// <summary>
        /// Called when allow undo/redo changes.
        /// </summary>
        protected virtual void OnAllowUndoRedoChanged ()
        {
            this.RaiseAllowUndoRedoChangedEvent ();
            this.RaiseCanUndoChangedEvent ();
            this.RaiseCanRedoChangedEvent ();
        }

        /// <summary>
        /// Called when allow search changes.
        /// </summary>
        protected virtual void OnAllowSearchChanged ()
        {
            this.RaiseAllowSearchChangedEvent ();
            this.RaiseCanSearchChangedEvent ();
        }

        /// <summary>
        /// Called when allow validation changes.
        /// </summary>
        protected virtual void OnAllowValidationChanged ()
        {
            this.RaiseAllowValidationChangedEvent ();
            this.RaiseCanValidateChangedEvent ();
        }

        /// <summary>
        /// Called when has unsaved changes changes.
        /// </summary>
        protected virtual void OnHasUnsavedChangesChanged ()
        {
            this.RaiseHasUnsavedChangesChangedEvent ();
            this.RaiseTitleChangedEvent ();
        }

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Private Methods
        private void RaiseCurrentFileContentChangedEvent ()
        {
            if (this.CurrentFileContentChanged != null)
            {
                this.CurrentFileContentChanged (this, EventArgs.Empty);
            }
        }

        private void RaiseIsOpeningChanged ()
        {
            this.RaisePropertyChangedEvent ("IsOpening");
        }

        private void RaiseIsSavingChanged ()
        {
            this.RaisePropertyChangedEvent ("IsSaving");
        }

        private void RaiseIsClosingChanged ()
        {
            this.RaisePropertyChangedEvent ("IsClosing");
        }

        private void RaiseIsIndexingChanged ()
        {
            this.RaisePropertyChangedEvent ("IsIndexing");
        }

        private void RaiseIsSearchingChanged ()
        {
            this.RaisePropertyChangedEvent ("IsSearching");
        }

        private void RaiseIsValidatingChanged ()
        {
            this.RaisePropertyChangedEvent ("IsValidating");
        }

        private void RaiseIsBusyChanged ()
        {
            this.RaisePropertyChangedEvent ("IsBusy");
        }

        private void RaiseDataModelChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("DataModel");
        }

        private void RaiseIsDataModelLoadedEvent ()
        {
            this.RaisePropertyChangedEvent ("IsDataModelLoaded");
        }

        private void RaiseApplicationNameChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("ApplicationName");
        }

        private void RaiseCurrentFilePathChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CurrentFilePath");
        }

        private void RaiseCurrentFileNameChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CurrentFileName");
        }

        private void RaiseTitleChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("Title");
        }

        private void RaiseAllowCreateChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowCreate");
        }

        private void RaiseCanCreateChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanCreate");
        }

        private void RaiseAllowOpenChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowOpen");
        }

        private void RaiseCanOpenChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanOpen");
        }

        private void RaiseAllowCloseChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowClose");
        }

        private void RaiseCanCloseChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanClose");
        }

        private void RaiseAllowSaveChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowSave");
        }

        private void RaiseCanSaveChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanSave");
        }

        private void RaiseAllowUndoRedoChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowUndoRedo");
        }

        private void RaiseCanUndoChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanUndo");
        }

        private void RaiseCanRedoChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanRedo");
        }

        private void RaiseAllowSearchChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowSearch");
        }

        private void RaiseCanSearchChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanSearch");
        }

        private void RaiseAllowValidationChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("AllowValidation");
        }

        private void RaiseCanValidateChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("CanValidate");
        }

        private void RaiseHasUnsavedChangesChangedEvent ()
        {
            this.RaisePropertyChangedEvent ("HasUnsavedChanges");
        }

        #region Data Model Event Handlers
        private void OnDataModelItemAdded (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs)
        {
            this.OnDataModelInternalsChanged ();
        }

        private void OnDataModelItemRemoved (ModelObject parentObject, INotifyingCollection sender, IItemAddedRemovedEventArgs eventArgs)
        {
            this.OnDataModelInternalsChanged ();
        }

        private void OnDataModelItemReplaced (ModelObject parentObject, INotifyingCollection sender, IItemReplacedEventArgs eventArgs)
        {
            this.OnDataModelInternalsChanged ();
        }

        private void OnDataModelPropertyChanged (ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            this.OnDataModelInternalsChanged ();
        }
        #endregion

        #region Controller Event Handlers
        private void OnCreated (object sender, EventArgs eventArgs)
        {
            this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "CreatedNewDocString"));

            this.OnDataModelChanged ();
        }

        private void OnOpening (object sender, OpeningEventArgs eventArgs)
        {
            this.IsOpening = true;

            this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "OpeningString", eventArgs.FilePath));
        }

        private void OnOpened(object sender, OpenedEventArgs eventArgs)
        {
            if (eventArgs.Error == null)
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "OpenedString", eventArgs.FilePath));

                if (File.Exists(eventArgs.FilePath))
                {
                    // Watch for file changes
                    this.currentFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(eventArgs.FilePath));
                    this.currentFileWatcher.Filter = Path.GetFileName(eventArgs.FilePath);
                    this.currentFileWatcher.EnableRaisingEvents = true;
                    //this.currentFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    //this.currentFileWatcher.Changed += this.OnCurrentFileContentChanged;
                }
            }
            else
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "OpenErrorMessageString", eventArgs.FilePath));
            }

            this.IsOpening = false;

            this.OnDataModelChanged ();
            this.OnCurrentFileChanged ();
        }

        private void OnSaving(object sender, SavingEventArgs eventArgs)
        {
            this.IsSaving = true;

            this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "SavingString", eventArgs.FilePath));
        }

        private void OnSaved(object sender, SavedEventArgs eventArgs)
        {
            if (eventArgs.Error == null)
            {
                // If saving to a file this time, or if always saving to a stream, mark that a save has happened and that the model is not dirty
                if (!string.IsNullOrEmpty (eventArgs.FilePath) || string.IsNullOrEmpty (this.CurrentFilePath))
                {
                    this.changedSinceLastSave = false;

                    if (this.IsUndoRedoAvailable)
                    {
                        this.lastSavedUndoableKey = this.UndoManager.CurrentUndoableKey;
                    }
                }

                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "SavedString", eventArgs.FilePath ?? string.Empty));
            }
            else
            {
                this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "SaveErrorMessageString", eventArgs.FilePath ?? string.Empty));
            }

            this.IsSaving = false;

            this.OnCurrentFileChanged ();
        }

        private void OnClosing(object sender, ClosingEventArgs eventArgs)
        {
            // TODO: If the close is canceled, we never get the Closed event and incorrectly reportpublic void Test ()
        {
//            DerivedClass d = new DerivedClass ();
//            IDisposable b = (IDisposable)d;
        }
            // that we're still in the Closing state :(

            //this.IsClosing = true;

            //this.AddStatusEvent (WpfViewProperties.Resources.ClosingString, eventArgs.FilePath);
        }

        private void OnClosed(object sender, ClosedEventArgs eventArgs)
        {
            this.changedSinceLastSave = false;
            this.lastSavedUndoableKey = 0;

            if (this.currentFileWatcher != null)
            {
                this.currentFileWatcher.Changed -= this.OnCurrentFileContentChanged;
                this.currentFileWatcher.EnableRaisingEvents = false;
                this.currentFileWatcher = null;
            }

            this.IsClosing = false;

            this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "ClosedString", eventArgs.FilePath));

            this.OnDataModelChanged ();
            this.OnCurrentFileChanged ();
        }
        #endregion

        #region Editor Service Event Handlers
        private void OnAfterUndo (object sender, EventArgs e)
        {
            this.OnHasUnsavedChangesChanged ();
        }

        private void OnAfterRedo (object sender, EventArgs e)
        {
            this.OnHasUnsavedChangesChanged ();
        }

        private void OnSearchEngineStatusChanged (object sender, EventArgs e)
        {
            this.OnIsIndexingChanged ();
            this.RaiseCanSearchChangedEvent ();
        }

        private void OnSearchEngineSearchComplete (object sender, SearchCompleteEventArgs e)
        {
            this.IsSearching = false;

            this.AddStatusEvent ((IMutable)new ManagedResourceString (Properties.WpfViewResources.ResourceManager, "SearchCompleteString", e.SearchItems.Length));
        }

        private void OnValidationManagerStatusChanged (object sender, EventArgs e)
        {
            this.OnIsValidatingChanged ();
        }
        #endregion
        
        private void OnCurrentFileContentChanged (object sender, FileSystemEventArgs e)
        {
            this.RaiseCurrentFileContentChangedEvent ();
        }

        #endregion
    }
}
