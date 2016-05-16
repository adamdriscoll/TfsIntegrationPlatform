// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public partial class Shell<TViewModel, TController, TModel>
    {
        #region Fields
        private CommandBinding newCommandBinding;
        private CommandBinding openCommandBinding;
        private CommandBinding closeCommandBinding;
        private CommandBinding saveCommandBinding;
        private CommandBinding saveAsCommandBinding;

        private CommandBinding undoCommandBinding;
        private CommandBinding redoCommandBinding;

        private CommandBinding searchCommandBinding;

        private CommandBinding validateCommandBinding;

        private CommandBinding aboutCommandBinding;

        private CommandBinding exitCommandBinding;
        #endregion

        #region Initialization
        private void InitializeCommandBindings ()
        {
            this.NewCommandBinding = new NewCommandBinding<TController, TModel> (this.ViewModel, this);
            this.OpenCommandBinding = new OpenCommandBinding<TController, TModel> (this.ViewModel, this);
            this.CloseCommandBinding = new CloseCommandBinding<TController, TModel> (this.ViewModel, this);
            this.SaveCommandBinding = new SaveCommandBinding<TController, TModel> (this.ViewModel, this);
            this.SaveAsCommandBinding = new SaveAsCommandBinding<TController, TModel> (this.ViewModel, this);

            this.UndoCommandBinding = new UndoCommandBinding<TController, TModel> (this.ViewModel);
            this.RedoCommandBinding = new RedoCommandBinding<TController, TModel> (this.ViewModel);

            this.SearchCommandBinding = new SearchCommandBinding<TController, TModel> (this.ViewModel, this);

            this.ValidateCommandBinding = new ValidateCommandBinding<TController, TModel> (this.ViewModel);

            this.AboutCommandBinding = new AboutCommandBinding<TController, TModel> (this.ViewModel, this);

            this.ExitCommandBinding = new ExitCommandBinding<TController, TModel> (this.viewModel, this);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.New" /> command.
        /// </summary>
        protected CommandBinding NewCommandBinding
        {
            get
            {
                return this.newCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.newCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.Open" /> command.
        /// </summary>
        protected CommandBinding OpenCommandBinding
        {
            get
            {
                return this.openCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.openCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.Close" /> command.
        /// </summary>
        protected CommandBinding CloseCommandBinding
        {
            get
            {
                return this.closeCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.closeCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.Save" /> command.
        /// </summary>
        protected CommandBinding SaveCommandBinding
        {
            get
            {
                return this.saveCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.saveCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.SaveAs" /> command.
        /// </summary>
        protected CommandBinding SaveAsCommandBinding
        {
            get
            {
                return this.saveAsCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.saveAsCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.Undo" /> command.
        /// </summary>
        protected CommandBinding UndoCommandBinding
        {
            get
            {
                return this.undoCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.undoCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.Redo" /> command.
        /// </summary>
        protected CommandBinding RedoCommandBinding
        {
            get
            {
                return this.redoCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.redoCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="ApplicationCommands.Find" /> command.
        /// </summary>
        protected CommandBinding SearchCommandBinding
        {
            get
            {
                return this.searchCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.searchCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="EditorCommands.Validate" /> command.
        /// </summary>
        protected CommandBinding ValidateCommandBinding
        {
            get
            {
                return this.validateCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.validateCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="EditorCommands.About" /> command.
        /// </summary>
        protected CommandBinding AboutCommandBinding
        {
            get
            {
                return this.aboutCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.aboutCommandBinding, value);
            }
        }

        /// <summary>
        /// Gets or sets the binding for the <see cref="EditorCommands.Exit" /> command.
        /// </summary>
        protected CommandBinding ExitCommandBinding
        {
            get
            {
                return this.exitCommandBinding;
            }
            set
            {
                this.UpdateCommandBinding (ref this.exitCommandBinding, value);
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Updates the specified command binding.
        /// </summary>
        /// <param name="currentCommandBinding">The current command binding.</param>
        /// <param name="newCommandBinding">The new command binding.</param>
        protected void UpdateCommandBinding (ref CommandBinding currentCommandBinding, CommandBinding newCommandBinding)
        {
            if (currentCommandBinding != newCommandBinding)
            {
                if (currentCommandBinding != null)
                {
                    this.CommandBindings.Remove (currentCommandBinding);
                }

                currentCommandBinding = newCommandBinding;

                if (currentCommandBinding != null)
                {
                    this.CommandBindings.Add (currentCommandBinding);
                }
            }
        }
        #endregion
    }
}
