// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Shell.Search;
using Microsoft.TeamFoundation.Migration.Shell.Undo;
using Microsoft.TeamFoundation.Migration.Shell.Validation;

namespace Microsoft.TeamFoundation.Migration.Shell.Controller
{
    /// <summary>
    /// ControllerBase provides the base functionality for an IController.
    /// </summary>
    /// <typeparam name="T">The Model type.</typeparam>
    public abstract partial class ControllerBase<T> : IController
        where T : ModelRoot, new ()
    {
        #region Fields
        private string savePath;
        private T model;
        private readonly AsyncOperation asyncOperation;
        private readonly EditorUndoManager<T> undoManager;
        private readonly EditorSearchEngine<T> searchEngine;
        private readonly EditorValidationManager<T> validationManager;
        private readonly PluginManager pluginManager;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ControllerBase class.
        /// </summary>
        public ControllerBase ()
        {
            this.savePath = null;
            this.model = null;

            this.asyncOperation = AsyncOperationManager.CreateOperation (null);

            this.undoManager = this.InitializeUndoManager ();
            this.searchEngine = this.InitializeSearchEngine ();
            this.validationManager = this.InitializeValidationManager ();
            this.pluginManager = this.InitializePluginManager ();
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the Controller creates a new Model.
        /// </summary>
        public event EventHandler Created;

        /// <summary>
        /// Occurs when the Controller begins to open an existing Model.
        /// </summary>
        public event EventHandler<OpeningEventArgs> Opening;

        /// <summary>
        /// Occurs when the Controller finishes opening an existing Model.
        /// </summary>
        public event EventHandler<OpenedEventArgs> Opened;

        /// <summary>
        /// Occurs when the Controller begins to close the current Model.
        /// </summary>
        public event EventHandler<ClosingEventArgs> Closing;

        /// <summary>
        /// Occurs when the Controller finishes closing the current Model.
        /// </summary>
        public event EventHandler<ClosedEventArgs> Closed;

        /// <summary>
        /// Occurs when the Controller begins to save the current Model.
        /// </summary>
        public event EventHandler<SavingEventArgs> Saving;

        /// <summary>
        /// Occurs when the Controller finishes saving the current Model.
        /// </summary>
        public event EventHandler<SavedEventArgs> Saved;
        #endregion

        #region Properties
        /// <summary>
        /// The path to which the current Model is saved.
        /// </summary>
        public string SavePath
        {
            get
            {
                return this.savePath;
            }
        }

        /// <summary>
        /// Gets the currently loaded Model.
        /// </summary>
        public T Model
        {
            get
            {
                return this.model;
            }
            protected set
            {
                if (this.Close ())
                {
                    this.model = value;

                    this.OnCreatedInternal (EventArgs.Empty);
                    this.RaiseCreatedEvent (EventArgs.Empty);
                }
            }
        }

        ModelRoot IController.Model
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the Undo Manager to which the current Model is bound.
        /// </summary>
        public EditorUndoManager<T> UndoManager
        {
            get
            {
                return this.undoManager;
            }
        }

        IEditorUndoManager IController.UndoManager
        {
            get
            {
                return this.UndoManager;
            }
        }

        /// <summary>
        /// Gets the Search Engine to which the current Model is bound.
        /// </summary>
        public EditorSearchEngine<T> SearchEngine
        {
            get
            {
                return this.searchEngine;
            }
        }

        IEditorSearchEngine IController.SearchEngine
        {
            get
            {
                return this.SearchEngine;
            }
        }

        /// <summary>
        /// Gets the Validation Manager to which the current Model is bound.
        /// </summary>
        public EditorValidationManager<T> ValidationManager
        {
            get
            {
                return this.validationManager;
            }
        }

        IEditorValidationManager IController.ValidationManager
        {
            get
            {
                return this.ValidationManager;
            }
        }

        /// <summary>
        /// Gets the Plugin Manager used by the Controller.
        /// </summary>
        public PluginManager PluginManager
        {
            get
            {
                return this.pluginManager;
            }
        }

        IPluginManager IController.PluginManager
        {
            get
            {
                return this.PluginManager;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a new Model synchronously.
        /// </summary>
        /// <returns><c>true</c> if creation completed, otherwise <c>false</c>.</returns>
        public bool Create ()
        {
            if (this.Close ())
            {
                this.model = ModelRoot.Create<T> ();

                this.OnCreatedInternal (EventArgs.Empty);
                this.RaiseCreatedEvent (EventArgs.Empty);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens an existing Model synchronously from a file.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        /// <returns><c>true</c> if the open completed, <c>false</c> otherwise.</returns>
        public bool Open (string path)
        {
            return this.Open (null, path);
        }

        /// <summary>
        /// Opens an existing Model synchronously from a stream.
        /// </summary>
        /// <param name="stream">The stream to the Model.</param>
        /// <returns><c>true</c> if the open completed, <c>false</c> otherwise.</returns>
        public bool Open (Stream stream)
        {
            return this.Open (stream, string.Empty);
        }

        /// <summary>
        /// Opens an existing Model asynchronously from a file.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        public void OpenAsync (string path)
        {
            this.OpenAsync (null, path);
        }

        /// <summary>
        /// Opens an existing Model asynchronously from a stream.
        /// </summary>
        /// <param name="stream">The stream to the Model.</param>
        public void OpenAsync (Stream stream)
        {
            this.OpenAsync (stream, string.Empty);
        }

        /// <summary>
        ///  Closes the current Model.
        /// </summary>
        /// <returns><c>true</c> of the close completes, <c>false</c> otherwise.</returns>
        public bool Close ()
        {
            if (this.Model == null)
            {
                return true;
            }

            string path = this.SavePath;

            ClosingEventArgs closingEventArgs = new ClosingEventArgs (path);
            this.OnClosingInternal (closingEventArgs);
            if (closingEventArgs.Cancel)
            {
                return false;
            }

            if (this.RaiseClosingEvent (closingEventArgs))
            {
                return false;
            }

            this.model = null;
            this.savePath = null;

            ClosedEventArgs closedEventArgs = new ClosedEventArgs (path);
            this.OnClosedInternal (closedEventArgs);
            this.RaiseClosedEvent (closedEventArgs);

            return true;
        }

        /// <summary>
        /// Saves the current Model synchronously to a file.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        /// <returns><c>true</c> if the save completes, <c>false</c> otherwise.</returns>
        public bool Save (string path)
        {
            return this.Save (null, path);
        }

        /// <summary>
        /// Saves the current Model synchronously to a stream.
        /// </summary>
        /// <param name="stream">The stream to the Model.</param>
        /// <returns><c>true</c> if the save completes, <c>false</c> otherwise.</returns>
        public bool Save (Stream stream)
        {
            return this.Save (stream, string.Empty);
        }

        /// <summary>
        /// Saves the current Model asynchronously to a file.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        public void SaveAsync (string path)
        {
            this.SaveAsync (null, path);
        }

        /// <summary>
        /// Saves the current Model asynchronously to a stream.
        /// </summary>
        /// <param name="stream">The path to the Model.</param>
        public void SaveAsync (Stream stream)
        {
            this.SaveAsync (stream, string.Empty);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Initializes the Undo Manager.
        /// </summary>
        /// <returns>The initialized Undo Manager.</returns>
        /// <remarks>
        /// To enable Undo, a Controller must initialize an Undo Manager. If the default Editor Undo Manager is satisfactory, then this method can simply instantiate and return an EditorUndoManager.
        /// <para>Notes to Inheritors: When overriding InitializeUndoManager in a derived class, calling the base class's InitializeUndoManager method is not necessary because there is no initial implementation.</para>
        /// </remarks>
        protected virtual EditorUndoManager<T> InitializeUndoManager ()
        {
            return null;
        }

        /// <summary>
        /// Initializes the Search Engine.
        /// </summary>
        /// <returns>The initialized Search Engine</returns>
        /// <remarks>
        /// To enable Undo, a Controller must initialize a Search Engine. If the default Editor Search Engine is satisfactory, then this method can simply instantiate and return an EditorSearchEngine.
        /// <para>Notes to Inheritors: When overriding InitializeSearchEngine in a derived class, calling the base class's InitializeSearchEngine method is not necessary because there is no initial implementation.</para>
        /// </remarks>
        protected virtual EditorSearchEngine<T> InitializeSearchEngine ()
        {
            return null;
        }

        /// <summary>
        /// Initializes the Validation Manager.
        /// </summary>
        /// <returns>The initialized Validation Manager</returns>
        /// <remarks>
        /// To enable Validation, a Controller must initialize a Validation Manager. If the default Editor Validation Manager is satisfactory, then this method can simply instantiate and return an EditorValidationManager.
        /// <para>Notes to Inheritors: When overriding InitializeValidationManager in a derived class, calling the base class's InitializeValidationManager method is not necessary because there is no initial implementation.</para>
        /// </remarks>
        protected virtual EditorValidationManager<T> InitializeValidationManager ()
        {
            return null;
        }

        /// <summary>
        /// Initializes the Plugin Manager.
        /// </summary>
        /// <returns>The initialized Plugin Manager.</returns>
        /// <remarks>
        /// To enable Undo, a Controller must initialize an Plugin Manager. If the default Plugin Manager is satisfactory, then this method can simply instantiate and return an PluginManager.
        /// <para>Notes to Inheritors: When overriding InitializePluginManager in a derived class, calling the base class's InitializePluginManager method is not necessary because there is no initial implementation.</para>
        /// </remarks>
        protected virtual PluginManager InitializePluginManager ()
        {
            return null;
        }

        /// <summary>
        /// Called before the Created event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnCreated in a derived class, calling the base class's OnCreated method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnCreated (EventArgs eventArgs)
        {
        }

        /// <summary>
        /// Called before the Opening event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnOpening in a derived class, calling the base class's OnOpening method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnOpening (OpeningEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Called before the Opened event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnOpened in a derived class, calling the base class's OnOpened method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnOpened (OpenedEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Called before the Closing event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnClosing in a derived class, calling the base class's OnClosing method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnClosing (ClosingEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Called before the Closed event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnClosed in a derived class, calling the base class's OnClosed method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnClosed (ClosedEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Called before the Saving event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnSaving in a derived class, calling the base class's OnSaving method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnSaving (SavingEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Called before the Saved event is raised.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnSaved in a derived class, calling the base class's OnSaved method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="eventArgs">An EventArgs that contains the event data.</param>
        protected virtual void OnSaved (SavedEventArgs eventArgs)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnCreatedInternal(EventArgs eventArgs)
        {
            if (this.UndoManager != null)
            {
                this.UndoManager.Model = this.Model;
            }

            if (this.SearchEngine != null)
            {
                this.SearchEngine.Model = this.Model;
            }

            if (this.ValidationManager != null)
            {
                this.ValidationManager.Model = this.Model;
            }

            this.OnCreated(eventArgs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void RaiseCreatedEvent(EventArgs eventArgs)
        {
            if (this.Created != null)
            {
                this.Created(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnOpeningInternal(OpeningEventArgs eventArgs)
        {
            this.OnOpening(eventArgs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void RaiseOpeningEvent(OpeningEventArgs eventArgs)
        {
            if (this.Opening != null)
            {
                this.Opening(this, eventArgs);
            }
        }
        
        /// <summary>
        /// OnOpenedInternal should be called by a controller extending Open once the model is loaded.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnOpenedInternal(OpenedEventArgs eventArgs)
        {
            if (this.UndoManager != null)
            {
                this.UndoManager.Model = this.Model;
            }

            if (this.SearchEngine != null)
            {
                this.SearchEngine.Model = this.Model;
            }

            if (this.ValidationManager != null)
            {
                this.ValidationManager.Model = this.Model;
            }

            this.OnOpened(eventArgs);
        }

        /// <summary>
        /// RaiseOpenedEvent should be called by a controller extending Open once the model is loaded.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void RaiseOpenedEvent(OpenedEventArgs eventArgs)
        {
            if (this.Opened != null)
            {
                this.Opened(this, eventArgs);
            }
        }

        /// <summary>
        /// OnClosingInternal should be called by a controller extending Close before the model is closed.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnClosingInternal(ClosingEventArgs eventArgs)
        {
            this.OnClosing(eventArgs);
        }

        /// <summary>
        /// RaiseClosingEvent should be called by a controller extending Close before the model is closed.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        protected bool RaiseClosingEvent(ClosingEventArgs eventArgs)
        {
            if (this.Closing != null)
            {
                this.Closing(this, eventArgs);
            }

            return eventArgs.Cancel;
        }

        /// <summary>
        /// OnClosedInternal should be called by a controller extending Close after the model is closed.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnClosedInternal(ClosedEventArgs eventArgs)
        {
            if (this.UndoManager != null)
            {
                this.UndoManager.Model = null;
            }

            if (this.SearchEngine != null)
            {
                this.SearchEngine.Model = null;
            }

            if (this.ValidationManager != null)
            {
                this.ValidationManager.Model = null;
            }

            this.OnClosed(eventArgs);
        }

        /// <summary>
        /// RaiseClosedEvent should be called by a controller extending Close after the model is closed.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void RaiseClosedEvent(ClosedEventArgs eventArgs)
        {
            if (this.Closed != null)
            {
                this.Closed(this, eventArgs);
            }
        }

        /// <summary>
        /// OnSavingInternal should be called by a controller extending Save before the model is saved.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnSavingInternal(SavingEventArgs eventArgs)
        {
            this.OnSaving(eventArgs);
        }

        /// <summary>
        /// RaiseSavingEvent should be called by a controller extending Save before the model is saved.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void RaiseSavingEvent(SavingEventArgs eventArgs)
        {
            if (this.Saving != null)
            {
                this.Saving(this, eventArgs);
            }
        }

        /// <summary>
        /// OnSavedInternal should be called by a controller extending Save after the model is saved.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnSavedInternal(SavedEventArgs eventArgs)
        {
            this.OnSaved(eventArgs);
        }

        /// <summary>
        /// RaiseSavedEvent should be called by a controller extending Save after the model is saved.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void RaiseSavedEvent(SavedEventArgs eventArgs)
        {
            if (this.Saved != null)
            {
                this.Saved(this, eventArgs);
            }
        }
        #endregion

        #region Private Methods
        private bool Open (Stream stream, string path)
        {
            if (this.Close ())
            {
                OpeningEventArgs openingEventArgs = new OpeningEventArgs (path);
                this.OnOpeningInternal (openingEventArgs);
                this.RaiseOpeningEvent (openingEventArgs);

                OpenedEventArgs openedEventArgs = null;

                try
                {
                    this.OpenInternal (stream, path);
                    openedEventArgs = new OpenedEventArgs (path, null);
                }
                catch (Exception exception)
                {
                    openedEventArgs = new OpenedEventArgs (path, exception);
                }

                try
                {
                    this.OnOpenedInternal (openedEventArgs);
                }
                catch (Exception exception)
                {
                    openedEventArgs = new OpenedEventArgs (path, exception);
                }

                this.RaiseOpenedEvent (openedEventArgs);

                return true;
            }

            return false;
        }

        private void OpenAsync (Stream stream, string path)
        {
            if (this.Close ())
            {
                OpeningEventArgs openingEventArgs = new OpeningEventArgs (path);
                this.OnOpeningInternal (openingEventArgs);
                this.RaiseOpeningEvent (openingEventArgs);

                SendOrPostCallback onOpened = delegate (object eventArgs)
                {
                    OpenedEventArgs openedEventArgs = eventArgs as OpenedEventArgs;
                    Debug.Assert (openedEventArgs != null);
                    this.OnOpenedInternal (openedEventArgs);
                };

                SendOrPostCallback raiseEvent = delegate (object eventArgs)
                {
                    OpenedEventArgs openedEventArgs = eventArgs as OpenedEventArgs;
                    Debug.Assert (openedEventArgs != null);
                    this.RaiseOpenedEvent (openedEventArgs);
                };

                AnonymousMethod open = delegate
                {
                    OpenedEventArgs openedEventArgs = null;

                    try
                    {
                        this.OpenInternal(stream, path);
                        openedEventArgs = new OpenedEventArgs(path, null);
                    }
                    catch (ConfigurationSchemaViolationException e)
                    {
                        openedEventArgs = new OpenedEventArgs(path, e);
                    }
                    catch (ConfigurationBusinessRuleViolationException e)
                    {
                        openedEventArgs = new OpenedEventArgs(path, e);
                    }
                    catch (Exception exception)
                    {
                        openedEventArgs = new OpenedEventArgs(path, exception);
                    }

                    try
                    {
                        // For the OnOpened virtual method, do a synchronous send so that we
                        // can trap any exceptions and report them to the event handlers
                        this.asyncOperation.SynchronizationContext.Send (onOpened, openedEventArgs);
                    }
                    catch (Exception exception)
                    {
                        openedEventArgs = new OpenedEventArgs (path, exception);
                    }

                    // For the Opened event, do an asynchronous post because we don't care about
                    // exceptions that are raised by event handlers
                    this.asyncOperation.Post (raiseEvent, openedEventArgs);
                };

                open.BeginInvoke (null, null);
            }
        }

        private void OpenInternal (Stream stream, string path)
        {
            if (!string.IsNullOrEmpty (path))
            {
                this.model = ModelRoot.Load<T> (path);
                this.savePath = path;
            }
            else
            {
                this.model = ModelRoot.Load<T> (stream);
                this.savePath = string.Empty;
            }
        }
        
        private bool Save (Stream stream, string path)
        {
            if (this.Model == null)
            {
                return false;
            }

            SavingEventArgs savingEventArgs = new SavingEventArgs (path);
            this.OnSavingInternal (savingEventArgs);
            this.RaiseSavingEvent (savingEventArgs);

            SavedEventArgs savedEventArgs = null;

            try
            {
                this.SaveInternal (stream, path);
                savedEventArgs = new SavedEventArgs (path, null);
            }
            catch (Exception exception)
            {
                savedEventArgs = new SavedEventArgs (path, exception);
            }

            try
            {
                this.OnSavedInternal (savedEventArgs);
            }
            catch (Exception exception)
            {
                savedEventArgs = new SavedEventArgs (path, exception);
            }

            this.RaiseSavedEvent (savedEventArgs);

            return true;
        }

        private void SaveAsync (Stream stream, string path)
        {
            if (this.Model == null)
            {
                return;
            }

            SavingEventArgs savingEventArgs = new SavingEventArgs (path);
            this.OnSavingInternal (savingEventArgs);
            this.RaiseSavingEvent (savingEventArgs);

            SendOrPostCallback onSaved = delegate (object eventArgs)
            {
                SavedEventArgs savedEventArgs = eventArgs as SavedEventArgs;
                Debug.Assert (savedEventArgs != null);
                this.OnSavedInternal (savedEventArgs);
            };

            SendOrPostCallback raiseEvent = delegate (object eventArgs)
            {
                SavedEventArgs savedEventArgs = eventArgs as SavedEventArgs;
                Debug.Assert (savedEventArgs != null);
                this.RaiseSavedEvent (savedEventArgs);
            };

            AnonymousMethod save = delegate
            {
                SavedEventArgs savedEventArgs = null;

                try
                {
                    this.SaveInternal (stream, path);
                    savedEventArgs = new SavedEventArgs (path, null);
                }
                catch (Exception exception)
                {
                    savedEventArgs = new SavedEventArgs (path, exception);
                }

                try
                {
                    // For the OnSaved virtual method, do a synchronous send so that we
                    // can trap any exceptions and report them to the event handlers
                    this.asyncOperation.SynchronizationContext.Send (onSaved, savedEventArgs);
                }
                catch (Exception exception)
                {
                    savedEventArgs = new SavedEventArgs (path, exception);
                }

                // For the Saved event, do an asynchronous post because we don't care about
                // exceptions that are raised by event handlers
                this.asyncOperation.Post (raiseEvent, savedEventArgs);
            };

            save.BeginInvoke (null, null);
        }

        private void SaveInternal (Stream stream, string path)
        {
            if (!string.IsNullOrEmpty (path))
            {
                this.Model.Save (path);
                this.savePath = path;
            }
            else
            {
                this.Model.Save (stream);
                this.savePath = string.Empty;
            }
        }
        #endregion
    }
}
