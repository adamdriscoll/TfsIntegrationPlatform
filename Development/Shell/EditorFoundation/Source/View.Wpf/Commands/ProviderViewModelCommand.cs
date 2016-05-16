// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.ComponentModel;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    public abstract class ProviderViewModelCommand<TModelObject> : Command
        where TModelObject : ModelObject, new()
    {
        #region Fields
        /// <summary>
        /// The view model associated with the command.
        /// </summary>
        protected readonly ShellViewModel m_viewModel;
        protected string m_providerReferenceName;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelCommand&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        protected ProviderViewModelCommand(ShellViewModel viewModel, string providerReferenceName)
        {
            m_providerReferenceName = providerReferenceName;

            m_viewModel = viewModel;
            if (!string.IsNullOrEmpty(this.ViewModelPropertyName))
            {
                m_viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
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

        public string ProviderReferenceName
        {
            get
            {
                return m_providerReferenceName;
            }
        }
        #endregion

        #region Private Methods
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.ViewModelPropertyName)
            {
                this.RaiseCanExecuteChangedEvent();
            }
        }
        #endregion
    }
}
