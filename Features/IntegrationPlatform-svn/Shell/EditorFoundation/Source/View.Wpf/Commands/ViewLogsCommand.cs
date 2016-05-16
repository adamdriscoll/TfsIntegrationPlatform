// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class ViewLogsCommand : ViewModelCommand<ShellController, ConfigurationModel>
    {
        #region Constructors
        public ViewLogsCommand()
            : base(null)
        {
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets the name of the view model property that determines whether this command can be executed.
        /// </summary>
        /// <value></value>
        protected override string ViewModelPropertyName
        {
            get
            {
                return string.Empty;
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
        public override bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute(object parameter)
        {
            OpenLogs();
        }

        #endregion

        #region Internal Methods

        private void OpenLogs()
        {
            Process.Start(TraceDirectory);
        }

        private string m_traceDirectory;
        private string TraceDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(m_traceDirectory))
                {
                    m_traceDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        @"Microsoft\Team Foundation\TFS Integration Platform");
                }

                if (!Directory.Exists(m_traceDirectory))
                {
                    Directory.CreateDirectory(m_traceDirectory);
                }

                return m_traceDirectory;
            }
        }
        #endregion
    }

    public class ViewLogsCommandBinding : CommandBinding
    {
        #region Constructors
        public ViewLogsCommandBinding()
        {
            this.Command = ShellCommands.ViewLogs;

            ViewLogsCommand command = new ViewLogsCommand();

            this.CanExecute += delegate(object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = true;
            };

            this.Executed += delegate(object sender, ExecutedRoutedEventArgs e)
            {
                command.Execute(null);
            };
        }
        #endregion
    }
}
