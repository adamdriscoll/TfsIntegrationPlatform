// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Globalization;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a lookless view that gives access to the data model,
    /// the controller, the view model, and the services for the editor.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the view model.</typeparam>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the data model.</typeparam>
    public partial class Shell<TViewModel, TController, TModel> : Window
        where TViewModel : ViewModel<TController, TModel>, new ()
        where TController : ControllerBase<TModel>, new ()
        where TModel : ModelRoot, new ()
    {
        #region Fields
        private readonly TViewModel viewModel;
        #endregion

        #region Dependency Properties
        /// <summary>
        /// Identifies the IsRuntimeSkinningEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRuntimeSkinningEnabledProperty =
            DependencyProperty.Register ("IsRuntimeSkinningEnabled", typeof (bool), typeof (Shell<TViewModel, TController, TModel>), new PropertyMetadata (true));

        /// <summary>
        /// Identifies the IsRuntimeLocalizationEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRuntimeLocalizationEnabledProperty =
            DependencyProperty.Register ("IsRuntimeLocalizationEnabled", typeof (bool), typeof (Shell<TViewModel, TController, TModel>), new PropertyMetadata (true));
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Shell&lt;TViewModel, TController, TModel&gt;"/> class.
        /// </summary>
        public Shell ()
        {
            // Bind visual properties of the window to the persistent settings
            this.BindSettings ();

            this.MinHeight = 600;
            this.MinWidth = 1000;
            this.Width = 1000;

            // Initialize the view model
            try
            {
                this.viewModel = new TViewModel();
            }
            catch (Exception e)
            {
                Utilities.HandleException(e);
            }
            
            // Subscribe to view model events
            this.ViewModel.CurrentFileContentChanged += this.OnCurrentFileContentChanged;

            // Subscribe to controller events
            this.ViewModel.Controller.Opening += this.OnOpening;
            this.ViewModel.Controller.Opened += this.OnOpened;
            this.ViewModel.Controller.Saving += this.OnSaving;
            this.ViewModel.Controller.Saved += this.OnSaved;
            this.ViewModel.Controller.Closing += this.OnClosing;

            // Set the data context to the shell. This sets the stage for simple
            // data binding in the skins (xaml) that can access the wpf specific
            // shell features, the view model, and the data model. 
            this.DataContext = this;

            // Attach any available services to make them easily accessible to child UI elements
            if (this.ViewModel.IsUndoRedoAvailable)
            {
                EditorServices.SetUndoManager (this, this.ViewModel.UndoManager);
            }

            if (this.ViewModel.IsSearchAvailable)
            {
                EditorServices.SetSearchEngine (this, this.ViewModel.SearchEngine);
            }

            if (this.ViewModel.IsValidationAvailable)
            {
                EditorServices.SetValidationManager (this, this.ViewModel.ValidationManager);
            }

            // Set a dynamic resource reference for the window's content and the window's style.
            // This allows editor applications to switch out the "skin" at compile time or runtime.
            this.SetResourceReference (Window.ContentProperty, "ShellContent");
            this.SetResourceReference (Window.StyleProperty, "ShellStyle");

            // Initialize the command bindings
            this.InitializeCommandBindings ();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the view model.
        /// </summary>
        /// <value>The view model.</value>
        public TViewModel ViewModel
        {
            get
            {
                return this.viewModel;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether runtime skinning is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if runtime skinning is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsRuntimeSkinningEnabled
        {
            get
            {
                return (bool)this.GetValue (IsRuntimeSkinningEnabledProperty);
            }
            set
            {
                this.SetValue (IsRuntimeSkinningEnabledProperty, value);
            }
        }

        /// <summary>
        /// Gets the collection of skins that are available.
        /// </summary>
        public CollectionView Skins
        {
            get
            {
                return SkinCollectionView.Default;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether runtime localization is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if runtime localization is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsRuntimeLocalizationEnabled
        {
            get
            {
                return (bool)GetValue (IsRuntimeLocalizationEnabledProperty);
            }
            set
            {
                SetValue (IsRuntimeLocalizationEnabledProperty, value);
            }
        }

        /// <summary>
        /// Gets the collection of localizations that are available.
        /// </summary>
        public CollectionView Localizations
        {
            get
            {
                return LocalizationCollectionView.Default;
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closed"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnClosed (EventArgs e)
        {
            base.OnClosed (e);

            Properties.Settings.Default.Save ();

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closing"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that contains the event data.</param>
        protected override void OnClosing (CancelEventArgs e)
        {
            base.OnClosing (e);

            if (CloseCommand<TController, TModel>.CanClose (this.ViewModel))
            {
                e.Cancel = !CloseCommand<TController, TModel>.Close (this.ViewModel);
            }
        }

        /// <summary>
        /// Called when a data model is opening.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EditorFoundation.Controller.OpeningEventArgs"/> instance containing the event data.</param>
        protected virtual void OnOpening (object sender, OpeningEventArgs eventArgs)
        {
            this.IsEnabled = false;
        }

        /// <summary>
        /// Called when the content of the currently opened files changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnCurrentFileContentChanged (object sender, EventArgs eventArgs)
        {
            // TODO
            // Also, the message should indicate whether unsaved changes will be lost
            if (MessageBox.Show ("Reload file?", "Reload File?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // Closing with unsaved changes will cause this class to prompt for save...
            }
        }

        /// <summary>
        /// Called when a data model is opened.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EditorFoundation.Controller.OpenedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnOpened (object sender, OpenedEventArgs eventArgs)
        {
            this.IsEnabled = true;

            // State has changed that may affect routed commands
            CommandManager.InvalidateRequerySuggested ();

            // Check for open errors
            if (eventArgs.Error != null)
            {
                Utilities.HandleException (eventArgs.Error, false, Properties.WpfViewResources.OpenErrorCaptionString, Properties.WpfViewResources.OpenErrorMessageString + ": ", eventArgs.FilePath);

                // Prompt to remove 
                if (this.ViewModel.RecentFiles.Contains (eventArgs.FilePath) &&
                    MessageBox.Show (string.Format (LocalizationManager.ActiveCulture, Properties.WpfViewResources.QueryRemoveFromRecentMessageString, eventArgs.FilePath),
                    Properties.WpfViewResources.QueryRemoveFromRecentCaptionString, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    this.ViewModel.RecentFiles.Remove (eventArgs.FilePath);
                }
            }
        }

        /// <summary>
        /// Called when a data model is saving.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EditorFoundation.Controller.SavingEventArgs"/> instance containing the event data.</param>
        protected virtual void OnSaving (object sender, SavingEventArgs eventArgs)
        {
            //this.FlushBindings (); ** Uncomment when we have C# 3.0 support **
            Extensions.FlushBindings (this);
        }

        /// <summary>
        /// Called when a loaded data model is saved.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EditorFoundation.Controller.SavedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnSaved (object sender, SavedEventArgs eventArgs)
        {
            if (eventArgs.Error != null)
            {
                Utilities.HandleException (eventArgs.Error, false, Properties.WpfViewResources.SaveErrorCaptionString, Properties.WpfViewResources.SaveErrorMessageString + ": ", eventArgs.FilePath);
            }
        }

        /// <summary>
        /// Called when a loaded data model is closing.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="ClosingEventArgs"/> instance containing the event data.</param>
        protected virtual void OnClosing (object sender, ClosingEventArgs eventArgs)
        {
            //this.FlushBindings (); ** Uncomment when we have C# 3.0 support **
            Extensions.FlushBindings (this);

            if (!eventArgs.Cancel && this.ViewModel.HasUnsavedChanges)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show (this, Properties.WpfViewResources.QuerySaveMessageString, Properties.WpfViewResources.QuerySaveCaptionString, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (messageBoxResult == MessageBoxResult.Cancel)
                {
                    eventArgs.Cancel = true;
                }
                else if (messageBoxResult == MessageBoxResult.Yes)
                {
                    eventArgs.Cancel = !SaveCommand<TController, TModel>.Save (this.ViewModel, this);
                }
            }
        }
        #endregion

        #region Private Methods
        private void BindSettings ()
        {
            Binding widthBinding = new Binding ("WindowWidth");
            widthBinding.Source = Properties.Settings.Default;
            widthBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding (this, Window.WidthProperty, widthBinding);

            Binding heightBinding = new Binding ("WindowHeight");
            heightBinding.Source = Properties.Settings.Default;
            heightBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding (this, Window.HeightProperty, heightBinding);

            Binding leftBinding = new Binding ("WindowLeft");
            leftBinding.Source = Properties.Settings.Default;
            leftBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding (this, Window.LeftProperty, leftBinding);

            Binding topBinding = new Binding ("WindowTop");
            topBinding.Source = Properties.Settings.Default;
            topBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding (this, Window.TopProperty, topBinding);
        }
        #endregion
    }
}
