// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.Controller;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class ViewEventLogsCommand : ViewModelCommand<ShellController, ConfigurationModel>
    {
        #region Constructors
        public ViewEventLogsCommand()
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
            OpenEventViewer();
        }

        #endregion

        #region Internal Methods

        private void OpenEventViewer()
        {
            string parameters;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                // Create a custom view file
                string customView = System.IO.Path.Combine(System.IO.Path.GetTempPath(), string.Format(CultureInfo.CurrentCulture, "{0}.xml", "TFSIntegrationPlatform"));
                using (StreamWriter writer = new StreamWriter(customView, false, Encoding.UTF8))
                {
                    string viewerConfig = string.Format("<ViewerConfig><QueryConfig><QueryParams><Simple><BySource>True</BySource><Channel>Application</Channel><Source>{0},{1}</Source></Simple></QueryParams><QueryNode><Name>TFS Integration Platform Events</Name><Description>TFS Integration Platform Events</Description><QueryList><Query Id='0' Path='Application'><Select Path='Application'>*[System[Provider[@Name='{0}' or @Name='{1}']]]</Select></Query></QueryList></QueryNode></QueryConfig></ViewerConfig>", Constants.TfsIntegrationServiceName, Constants.TfsIntegrationJobServiceName);
                    // Show all events
                    writer.Write(viewerConfig);
                }
                parameters = string.Format(CultureInfo.CurrentCulture, "/v:{0}", customView);
            }
            else
            {
                parameters = Environment.MachineName;
            }

            StopEventViewer();

            try
            {
                EventViewer = new Process();
                EventViewer.StartInfo = new ProcessStartInfo("eventvwr.msc", parameters);
                EventViewer.EnableRaisingEvents = false;
                EventViewer.Start();
            }
            catch (Exception ex)
            {
                Utilities.HandleException(ex, true, "Error", "Cannot open event viewer");
            }
        }

        private Process EventViewer { get; set; }

        private void StopEventViewer()
        {
            // Close the previous copy
            if (EventViewer != null)
            {
                try
                {
                    EventViewer.Close();
                }
                catch
                {
                    try
                    {
                        EventViewer.Kill();
                    }
                    catch
                    {
                        // If we cannot kill it we simply ignore it
                    }
                }
                finally
                {
                    EventViewer.Dispose();
                    EventViewer = null;
                }
            }
        }
        #endregion
    }

    public class ViewEventLogsCommandBinding : CommandBinding
    {
        #region Constructors
        public ViewEventLogsCommandBinding()
        {
            this.Command = ShellCommands.ViewEventLogs;

            ViewEventLogsCommand command = new ViewEventLogsCommand();

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
