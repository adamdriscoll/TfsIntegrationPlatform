// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a command that focuses a <see cref="UIElement"/>.
    /// </summary>
    public class FocusCommand : Command
    {
        #region Fields
        private static readonly FocusCommand _default = new FocusCommand ();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="FocusCommand"/> instance.
        /// </summary>
        public static FocusCommand Default
        {
            get
            {
                return FocusCommand._default;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public override bool CanExecute (object parameter)
        {
            return parameter is UIElement;
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute (object parameter)
        {
            UIElement element = parameter as UIElement;
            if (element != null)
            {
                element.Focus ();
            }
        }
        #endregion
    }
}
