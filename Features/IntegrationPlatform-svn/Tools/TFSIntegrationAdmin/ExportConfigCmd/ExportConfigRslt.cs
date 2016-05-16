// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.ExportConfigCmd
{
    class ExportConfigRslt : CommandResultBase
    {
        public Exception Exception
        {
            get;
            private set;
        }

        public ExportConfigRslt(bool succeeded, ICommand command)
            : base(succeeded, command)
        {            
        }

        public ExportConfigRslt(ICommand command, Exception e)
            : base(false, command)
        {
            Exception = e;
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.Print());

            if (Succeeded)
            {
                sb.Append(" ");
                Debug.Assert(!string.IsNullOrEmpty(OutputFilePath), "OutputFilePath is NULL or Empty");
                sb.AppendFormat(ResourceStrings.ExportConfigSuccessInfoFormat, OutputFilePath);
            }
            else if (Exception != null)
            {
                if (Exception is NonExistingSessionGroupUniqueIdException)
                {
                    sb.Append(" ");
                    sb.AppendFormat(ResourceStrings.ExportConfigNonExistSessionGroupIdFormat,
                        ((NonExistingSessionGroupUniqueIdException)Exception).NonExistingSessionGroupId.ToString());
                }
            }

            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        public string OutputFilePath
        {
            get;
            set;
        }
    }
}
