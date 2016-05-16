// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;

using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a lookless view of information about the application.
    /// </summary>
    public class About<TController, TModel> : Window
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly ViewModel<TController, TModel> viewModel;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="About&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public About (ViewModel<TController, TModel> viewModel)
        {
            this.viewModel = viewModel;

            // Set the data context to the shell. This sets the stage for simple
            // data binding in the skins (xaml) that can access the wpf specific
            // shell features, the view model, and the data model. 
            this.DataContext = this;

            // Set a dynamic resource reference for the window's content and the window's style.
            // This allows editor applications to switch out the "skin" at compile time or runtime.
            this.SetResourceReference (Window.ContentProperty, "AboutContent");
            this.SetResourceReference (Window.StyleProperty, "AboutStyle");
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the view model.
        /// </summary>
        /// <value>The view model.</value>
        public ViewModel<TController, TModel> ViewModel
        {
            get
            {
                return this.viewModel;
            }
        }
        #endregion
    }
}
