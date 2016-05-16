// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a standard set of Editor Foundation commands.
    /// </summary>
    public static class EditorCommands
    {
        #region Fields
        private static readonly RoutedUICommand validate;
        private static readonly RoutedUICommand exit;
        private static readonly RoutedUICommand about;
        #endregion

        #region Constructors
        static EditorCommands ()
        {
            // TODO: Localize?
            EditorCommands.validate = new RoutedUICommand ("Validate", "Validate", typeof (EditorCommands));

            EditorCommands.exit = new RoutedUICommand ("Exit", "Exit", typeof (EditorCommands));
            EditorCommands.exit.InputGestures.Add (new KeyGesture (Key.F4, ModifierKeys.Alt));

            EditorCommands.about = new RoutedUICommand ("About", "About", typeof (EditorCommands));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the value that represents the Validate command.
        /// </summary>
        public static RoutedUICommand Validate
        {
            get
            {
                return EditorCommands.validate;
            }
        }

        /// <summary>
        /// Gets the value that represents the Exit command.
        /// </summary>
        public static RoutedUICommand Exit
        {
            get
            {
                return EditorCommands.exit;
            }
        }

        /// <summary>
        /// Gets the value that represents the About command.
        /// </summary>
        public static RoutedUICommand About
        {
            get
            {
                return EditorCommands.about;
            }
        }
        #endregion
    }
}
