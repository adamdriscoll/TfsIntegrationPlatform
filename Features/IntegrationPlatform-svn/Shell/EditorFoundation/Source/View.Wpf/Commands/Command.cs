// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a base implementation of <see cref="ICommand"/>.
    /// </summary>
    public abstract class Command : ICommand
    {
        #region Events
        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;
        #endregion

        #region Public Methods
        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public abstract bool CanExecute (object parameter);

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public abstract void Execute (object parameter);
        #endregion

        #region Protected Methods
        /// <summary>
        /// Raises the can execute changed event.
        /// </summary>
        protected void RaiseCanExecuteChangedEvent ()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged (this, EventArgs.Empty);
            }
        }
        #endregion
    }
}
