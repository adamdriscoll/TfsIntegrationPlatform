// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Provides a <see cref="CommandBinding"/> that delegates the <see cref="ApplicationCommands.Open"/> routed command to the <see cref="ExportCommand&lt;TController, TModel&gt;"/>.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public class ViewImportExportPageCommandBinding : CommandBinding
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportCommandBinding&lt;TController, TModel&gt;"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="owner">The owner window.</param>
        public ViewImportExportPageCommandBinding(ShellViewModel viewModel)
        {
            this.Command = ShellCommands.ViewImportExportPage;

            this.CanExecute += delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = true;
            };

            this.Executed += delegate (object sender, ExecutedRoutedEventArgs e)
            {
                DisplayImportExport(viewModel);
            };
        }
        #endregion

        public void DisplayImportExport (ShellViewModel viewModel)
        {
            if (!(viewModel.SelectedViewModel is ImportExportViewModel))
            {
                ImportExportViewModel importExportViewModel = new ImportExportViewModel(viewModel);
                viewModel.SetModalViewModel(importExportViewModel);
            }
        }
    }
}