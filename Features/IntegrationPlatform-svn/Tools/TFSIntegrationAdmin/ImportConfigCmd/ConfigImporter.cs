// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules;
using Microsoft.TeamFoundation.Migration.Toolkit;
using TFSIntegrationAdmin.Exceptions;
using TFSIntegrationAdmin.ExportConfigCmd;
using TFSIntegrationAdmin.Interfaces;
using Toolkit = Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.ImportConfigCmd
{
    class ConfigImporter
    {
        ImportConfigCmd m_command;
        Toolkit.ConfigImporter m_configImporter;

        public ConfigImporter(string configPackage, bool excludeRules, ImportConfigCmd command)
        {
            m_configImporter = new Toolkit.ConfigImporter(configPackage, excludeRules);
            m_command = command;
        }

        public TFSIntegrationAdmin.Interfaces.ICommandResult Import()
        {
            try
            {
                Configuration importedConfig = m_configImporter.Import();

                var retVal = new ImportConfigRslt(true, m_command);
                retVal.ImportedConfig = importedConfig;
                return retVal;
            }
            catch (Exception e)
            {
                var retVal = new ImportConfigRslt(false, m_command);
                retVal.Exception = e;
                return retVal;
            }
        }
    }
}
