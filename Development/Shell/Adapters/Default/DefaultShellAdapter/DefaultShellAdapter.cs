// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Extensibility;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.DefaultShellAdapter
{
    [PluginDescription(m_adapterGuid, m_adapterName)]
    public class DefaultShellAdapter : IPlugin
    {
        #region Fields
        private const string m_adapterGuid = "3855EF2F-C427-40bc-AE2C-599E80CE2EE3";
        private const string m_adapterName = "Default Migration Shell Adapter";
        #endregion

        #region IPlugin Members
        public void OnContextEnter(object contextInstance)
        {
            // do nothing
        }

        public void OnContextLeave(object contextInstance)
        {
            // do nothing
        }

        private MigrationSourceView m_migrationSourceView;
        public IMigrationSourceView GetMigrationSourceView()
        {
            if (m_migrationSourceView == null)
            {
                MigrationSourceCommand command = new MigrationSourceCommand();
                m_migrationSourceView = new MigrationSourceView(command.CommandName, Guid.Empty, command.ButtonImage, command.Execute, GetMigrationSourceProperties);
            }
            return m_migrationSourceView;
        }

        public IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            throw new NotImplementedException();
        }

        public ExecuteFilterStringExtension FilterStringExtension
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private Dictionary<string, string> GetMigrationSourceProperties(MigrationSource migrationSource)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties["Server URL"] = migrationSource.ServerUrl;
            properties["Source Identifier"] = migrationSource.SourceIdentifier;

            return properties;
        }

        #endregion
    }
}
