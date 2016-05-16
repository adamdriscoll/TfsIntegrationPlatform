// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.ComponentModel;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a base command implementation for common view model based commands.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public abstract class ViewModelCommand<TController, TModel> : Command
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        /// <summary>
        /// The view model associated with the command.
        /// </summary>
        protected readonly ViewModel<TController, TModel> viewModel;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        protected ViewModelCommand (ViewModel<TController, TModel> viewModel)
        {
            this.viewModel = viewModel;
            if (!string.IsNullOrEmpty (this.ViewModelPropertyName))
            {
                this.viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
                this.CanExecuteChanged += delegate { CommandManager.InvalidateRequerySuggested(); };
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the name of the view model property that determines whether this command can be executed.
        /// </summary>
        protected abstract string ViewModelPropertyName
        {
            get;
        }
        #endregion

        #region Private Methods
        private void OnViewModelPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.ViewModelPropertyName)
            {
                this.RaiseCanExecuteChangedEvent ();
            }
        }
        #endregion
    }
}
