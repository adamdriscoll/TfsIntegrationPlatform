// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for MigrationView.xaml
    /// </summary>
    public partial class MigrationView : UserControl
    {
        public MigrationView()
        {
            InitializeComponent();

            HookCommands();
        }

        private void HookCommands()
        {
            m_homeLink.Command = ShellCommands.ViewHome;
            m_newConfigurationLink.Command = ShellCommands.Import;
            m_openConfigurationLink.Command = ShellCommands.OpenFromDB;
            m_viewCurrentConfigurationLink.Command = ShellCommands.ViewCurrentConfiguration;
            m_editCurrentConfigurationLink.Command = ShellCommands.EditCurrentConfiguration;

            m_optionsLink.Command = ShellCommands.ViewSettings;
            m_exportConfigurationLink.Command = ShellCommands.ViewImportExportPage;
            m_conflictsMigrationLink.Command = ShellCommands.ViewConflicts;
            m_progressMigrationLink.Command = ShellCommands.ViewProgress;
            m_viewEventLogsLink.Command = ShellCommands.ViewEventLogs;
            m_viewLogsLink.Command = ShellCommands.ViewLogs;

            m_helpLink.Command = ShellCommands.Help;

            m_startMigrationLink.Command = MediaCommands.Play;
            m_stopMigrationLink.Command = MediaCommands.Stop;
            m_pauseMigrationLink.Command = MediaCommands.Pause;
        }
    }

    public class SystemStateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SystemState state = (SystemState)value;
            bool result = false;
            result = (state == SystemState.ConfigurationSaved || state == SystemState.MigrationProgress
                || state == SystemState.MigrationStopped || state == SystemState.MigrationCompleted);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class BooleanInvertConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
