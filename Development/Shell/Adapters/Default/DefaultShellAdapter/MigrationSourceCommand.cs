// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.DefaultShellAdapter
{
    public class MigrationSourceCommand : Command
    {
        #region Fields
        private const string m_commandName = "Connect";
        #endregion
        
        #region Properties
        public string CommandName 
        {
            get
            {
                return m_commandName;
            }
        }

        public BitmapImage ButtonImage
        {
            get
            {
                return null;
            }
        }
        #endregion

        #region Public Methods
        public override bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Execute(object parameter)
        {
            MigrationSource migrationSource = parameter as MigrationSource;

            if (migrationSource == null)
            {
                throw new ArgumentException("Invalid argument: parameter: " + parameter.ToString());
            }

            GenericMigrationSourceDialog dialog = new GenericMigrationSourceDialog(migrationSource);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() != true)
            {
                migrationSource.ProviderReferenceName = null;
            }
        }
        #endregion
    }
}
