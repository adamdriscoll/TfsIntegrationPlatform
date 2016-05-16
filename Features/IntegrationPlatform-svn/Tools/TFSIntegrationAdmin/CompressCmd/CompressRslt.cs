// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.CompressCmd
{
    class CompressRslt : CommandResultBase
    {
        /// <summary>
        /// Constructor for failed execution
        /// </summary>
        /// <param name="e"></param>
        /// <param name="command"></param>
        /// <param name="sessionGroupId"></param>
        public CompressRslt(
            Exception e,
            ICommand command)
            : base(e == null, command)
        {
            Exception = e;
        }

        public Exception Exception
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
                if (null != Exception)
                {
                    sb.AppendLine(Exception.Message);
                }
            }
            
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
