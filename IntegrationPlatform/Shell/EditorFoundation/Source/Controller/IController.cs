// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Shell.Search;
using Microsoft.TeamFoundation.Migration.Shell.Undo;
using Microsoft.TeamFoundation.Migration.Shell.Validation;

namespace Microsoft.TeamFoundation.Migration.Shell.Controller
{
    /// <summary>
    /// IController defines the public interface for a Controller. It handles opening, saving,
    /// and creating of the Model and raises corresponding events. Asynchronous operations
    /// are supported, and it is always safe for the View to directly react to the events.
    /// </summary>
    public interface IController
    {
        #region Events
        /// <summary>
        /// Occurs when the Controller creates a new Model.
        /// </summary>
        event EventHandler Created;

        /// <summary>
        /// Occurs when the Controller begins to open an existing Model.
        /// </summary>
        event EventHandler<OpeningEventArgs> Opening;

        /// <summary>
        /// Occurs when the Controller finishes opening an existing Model.
        /// </summary>
        event EventHandler<OpenedEventArgs> Opened;

        /// <summary>
        /// Occurs when the Controller begins to close the current Model.
        /// </summary>
        event EventHandler<ClosingEventArgs> Closing;

        /// <summary>
        /// Occurs when the Controller finishes closing the current Model.
        /// </summary>
        event EventHandler<ClosedEventArgs> Closed;

        /// <summary>
        /// Occurs when the Controller begins to save the current Model.
        /// </summary>
        event EventHandler<SavingEventArgs> Saving;

        /// <summary>
        /// Occurs when the Controller finishes saving the current Model.
        /// </summary>
        event EventHandler<SavedEventArgs> Saved;
        #endregion

        #region Properties
        /// <summary>
        /// The path to which the current Model is saved.
        /// </summary>
        string SavePath { get; }

        /// <summary>
        /// Gets the currently loaded Model.
        /// </summary>
        ModelRoot Model { get; }

        /// <summary>
        /// Gets the Undo Manager to which the current Model is bound.
        /// </summary>
        IEditorUndoManager UndoManager { get; }

        /// <summary>
        /// Gets the Search Engine to which the current Model is bound.
        /// </summary>
        IEditorSearchEngine SearchEngine { get; }

        /// <summary>
        /// Gets the Validation Manager to which the current Model is bound.
        /// </summary>
        IEditorValidationManager ValidationManager { get; }

        /// <summary>
        /// Gets the Plugin Manager used by the Controller.
        /// </summary>
        IPluginManager PluginManager { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a new Model synchronously.
        /// </summary>
        /// <returns><c>true</c> if creation completed, otherwise <c>false</c>.</returns>
        bool Create ();

        /// <summary>
        /// Opens an existing Model synchronously.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        /// <returns><c>true</c> if the open completed, <c>false</c> otherwise.</returns>
        bool Open (string path);

        /// <summary>
        /// Opens an existing Model asynchronously.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        void OpenAsync (string path);

        /// <summary>
        ///  Closes the current Model.
        /// </summary>
        /// <returns><c>true</c> of the close completes, <c>false</c> otherwise.</returns>
        bool Close ();

        /// <summary>
        /// Saves the current Model synchronously.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        /// <returns><c>true</c> if the save completes, <c>false</c> otherwise.</returns>
        bool Save (string path);

        /// <summary>
        /// Saves the current Model asynchronously.
        /// </summary>
        /// <param name="path">The path to the Model.</param>
        void SaveAsync (string path);
        #endregion
    }
}
