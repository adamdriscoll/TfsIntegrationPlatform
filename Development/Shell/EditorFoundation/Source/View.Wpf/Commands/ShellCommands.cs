// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class ShellCommands
    {
        #region Constructors

        static ShellCommands ()
        {
            ShellCommands.ViewHome = new RoutedUICommand("ViewHome", "ViewHome", typeof(ShellCommands));
            ShellCommands.OpenFromDB = new RoutedUICommand("OpenFromDB", "OpenFromDB", typeof(ShellCommands));
            ShellCommands.OpenFromDB.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            ShellCommands.SaveToDB = new RoutedUICommand("SaveToDB", "SaveToDB", typeof(ShellCommands));
            ShellCommands.SaveToDB.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            ShellCommands.SaveAsToDB = new RoutedUICommand("SaveAsToDB", "SaveAsToDB", typeof(ShellCommands));
            ShellCommands.Import = new RoutedUICommand("Import", "Import", typeof(ShellCommands));
            ShellCommands.Export = new RoutedUICommand("Export", "Export", typeof(ShellCommands));
            ShellCommands.ViewConfiguration = new RoutedUICommand("ViewConfiguration", "ViewConfiguration", typeof(ShellCommands));
            ShellCommands.ViewSettings = new RoutedUICommand("ViewSettings", "ViewSettings", typeof(ShellCommands));
            ShellCommands.OpenRecent = new RoutedUICommand("OpenRecent", "OpenRecent", typeof(ShellCommands));
            ShellCommands.Refresh = new RoutedUICommand("Refresh", "Refresh", typeof(ShellCommands));
            ShellCommands.Refresh.InputGestures.Add(new KeyGesture(Key.F5));
            ShellCommands.Help = new RoutedUICommand("Help", "Help", typeof(ShellCommands));
            ShellCommands.ViewConflicts = new RoutedUICommand("ViewConflicts", "ViewConflicts", typeof(ShellCommands));
            ShellCommands.ViewProgress = new RoutedUICommand("ViewProgress", "ViewProgress", typeof(ShellCommands));
            ShellCommands.ViewCurrentConfiguration = new RoutedUICommand("ViewCurrentConfiguration", "ViewCurrentConfiguration", typeof(ShellCommands));
            ShellCommands.EditCurrentConfiguration = new RoutedUICommand("EditCurrentConfiguration", "EditCurrentConfiguration", typeof(ShellCommands));
            ShellCommands.ViewEventLogs = new RoutedUICommand("ViewEventLogs", "ViewEventLogs", typeof(ShellCommands));
            ShellCommands.ViewLogs = new RoutedUICommand("ViewLogs", "ViewLogs", typeof(ShellCommands));
            ShellCommands.ViewImportExportPage = new RoutedUICommand("ViewImportExportPage", "ViewImportExportPage", typeof(ShellCommands));
        }


        #endregion

        #region Properties

        public static RoutedUICommand ViewHome { get; private set; }

        public static RoutedUICommand Refresh { get; private set; }

        public static RoutedUICommand ViewSettings { get; private set; }

        public static RoutedUICommand ViewConfiguration { get; private set; }

        public static RoutedUICommand OpenFromDB { get; private set; }

        public static RoutedUICommand OpenRecent { get; private set; }

        public static RoutedUICommand SaveToDB { get; private set; }

        public static RoutedUICommand SaveAsToDB { get; private set; }

        public static RoutedUICommand Import { get; private set; }

        public static RoutedUICommand Export { get; private set; }

        public static RoutedUICommand Help { get; private set; }

        public static RoutedUICommand ViewConflicts { get; private set; }

        public static RoutedUICommand ViewProgress { get; private set; }

        public static RoutedUICommand ViewCurrentConfiguration { get; private set; }

        public static RoutedUICommand EditCurrentConfiguration { get; private set; }

        public static RoutedUICommand ViewEventLogs { get; private set; }

        public static RoutedUICommand ViewLogs { get; private set; }

        public static RoutedUICommand ViewImportExportPage { get; private set; }
        #endregion
    }
}
