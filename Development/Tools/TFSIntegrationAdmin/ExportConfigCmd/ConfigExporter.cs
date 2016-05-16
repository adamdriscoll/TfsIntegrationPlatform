// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using TFSIntegrationAdmin.Interfaces;
using Toolkit = Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.ExportConfigCmd
{
    internal class ConfigExporter
    {
        Toolkit.ConfigExporter m_configExporter;

        ExportConfigCmd ExportConfigCommand { get; set; }

        public ConfigExporter(ExportConfigCmd exportCmd, bool exportConfigFileOnly, string exportConfigPath)
        {
            Initialize(exportCmd, exportConfigFileOnly, exportConfigPath);
        }
        
        public ICommandResult Export(Guid sessionGroupUniqueId)
        {
            try
            {
                string exportedFile = m_configExporter.Export(sessionGroupUniqueId);
                var succeedRslt = new ExportConfigRslt(true, ExportConfigCommand);
                succeedRslt.OutputFilePath = Path.GetFullPath(exportedFile);
                return succeedRslt;
            }
            catch (Exception e)
            {
                var failedRslt = new ExportConfigRslt(ExportConfigCommand, e);
                return failedRslt;
            }
        }

        private void Initialize(ExportConfigCmd exportCmd, bool exportConfigFileOnly, string exportConfigPath)
        {
            ExportConfigCommand = exportCmd;
            m_configExporter = new Toolkit.ConfigExporter(exportConfigFileOnly, exportConfigPath);
        }
    }
}
