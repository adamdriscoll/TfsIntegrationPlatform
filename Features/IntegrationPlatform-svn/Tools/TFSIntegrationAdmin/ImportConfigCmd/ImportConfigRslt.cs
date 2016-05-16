// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using TFSIntegrationAdmin.Exceptions;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.ImportConfigCmd
{
    class ImportConfigRslt : CommandResultBase
    {
        public ImportConfigRslt(bool succeeded, ICommand command)
            : base(succeeded, command)
        { }

        public Exception Exception
        {
            get;
            set;
        }

        public Configuration ImportedConfig
        {
            get;
            set;
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(base.Print());

            if (!this.Succeeded)
            {
                if (null != Exception && Exception is ConfigNotExistInPackageException)
                {
                    sb.AppendLine(ResourceStrings.ImportConfigConfigNotExist);
                }
                else if (null != Exception)
                {
                    sb.AppendLine(Exception.Message);
                }
            }
            else
            {
                Debug.Assert(null != ImportedConfig, "ImportedConfig is NULL");
                sb.AppendFormat(ResourceStrings.ImportConfigSuccessInfoFormat, ImportedConfig.FriendlyName, ImportedConfig.SessionGroupUniqueId.ToString());
                sb.AppendLine();
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }
}
