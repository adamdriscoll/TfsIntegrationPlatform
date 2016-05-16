// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for ValidationResultsListView.xaml
    /// </summary>
    public partial class ValidationResultsListView : ListView
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultsListView"/> class.
        /// </summary>
        public ValidationResultsListView()
        {
            InitializeComponent();
        }
        #endregion

        #region Private Methods
        private void OnCanExecuteCopy (object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedItems.Count > 0;
        }

        private void OnExecutedCopy (object sender, ExecutedRoutedEventArgs e)
        {
            string format = DataFormats.UnicodeText;

            //string text = string.Join (Environment.NewLine, (from searchItem in this.SelectedItems.Cast<EditorFoundation.Validation.ValidationResult> () select searchItem.ToString ()).ToArray ()); ** Uncomment when we have C# 3.0 support **
            string text = string.Join(Environment.NewLine, Enumerable.ToArray(Enumerable.Select<Microsoft.TeamFoundation.Migration.Shell.Validation.ValidationResult, string>(Enumerable.Cast<Microsoft.TeamFoundation.Migration.Shell.Validation.ValidationResult>(this.SelectedItems), delegate(Microsoft.TeamFoundation.Migration.Shell.Validation.ValidationResult validationResult) { return validationResult.ToString(); })));
            DataObject dataObject = new DataObject ();
            dataObject.SetData (format, text);

            DataObjectSettingDataEventArgs settingDataEventArgs = new DataObjectSettingDataEventArgs (dataObject, format);
            this.RaiseEvent (settingDataEventArgs);

            if (settingDataEventArgs.CommandCancelled)
            {
                return;
            }

            DataObjectCopyingEventArgs copyingEventArgs = new DataObjectCopyingEventArgs (dataObject, false);
            this.RaiseEvent (copyingEventArgs);

            if (copyingEventArgs.CommandCancelled)
            {
                return;
            }

            Clipboard.SetDataObject (dataObject, true);
        }
        #endregion
    }
}
