// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.GrantAccessControlCmd
{
    class GrantAccessControlRslt : CommandResultBase
    {
        /// <summary>
        /// Constructor for succeeded execution
        /// </summary>
        /// <param name="command"></param>
        public GrantAccessControlRslt(
            ICommand command)
            : base(true, command)
        { }

        /// <summary>
        /// Constructor for failed execution
        /// </summary>
        /// <param name="e"></param>
        /// <param name="command"></param>
        /// <param name="sessionGroupId"></param>
        public GrantAccessControlRslt(
            string errorDetails,
            ICommand command)
            : base(false, command)
        {
            ErrorDetails = errorDetails;
        }

        public string ErrorDetails
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
                if (!string.IsNullOrEmpty(this.ErrorDetails))
                {
                    sb.AppendLine(this.ErrorDetails);
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }
}
