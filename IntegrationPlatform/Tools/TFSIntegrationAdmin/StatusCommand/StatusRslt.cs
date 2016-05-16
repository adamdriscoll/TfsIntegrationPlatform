// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.StatusCommand
{
    class StatusRslt : CommandResultBase
    {
        private Dictionary<Guid, string> m_sessionGroupDescription;

        /// <summary>
        /// Constructor for failed execution
        /// </summary>
        /// <param name="e"></param>
        /// <param name="command"></param>
        /// <param name="sessionGroupId"></param>
        public StatusRslt(
            string message,
            ICommand command,
            bool commandSucceeded)
            : base(commandSucceeded, command)
        {
            Message = message;
        }

        public StatusRslt(
            Dictionary<Guid, string> sessionGroupDescription,
            ICommand command)
            : base(true, command)
        {
            this.m_sessionGroupDescription = sessionGroupDescription;
        }

        public string Message
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
                if (!string.IsNullOrEmpty(this.Message))
                {
                    sb.AppendLine(this.Message);
                }
            }
            else
            {
                sb.AppendFormat("  {0}: {1}", ResourceStrings.StatusServiceHostInfo, GlobalConfiguration.UseWindowsService.ToString());
                sb.AppendLine();

                if (!string.IsNullOrEmpty(this.Message))
                {
                    sb.AppendLine("  " + this.Message);
                }

                if (null != m_sessionGroupDescription)
                {
                    sb.Append("  " + ResourceStrings.RunningSessionGroupsHeader);
                    if (m_sessionGroupDescription.Count == 0)
                    {
                        sb.AppendLine(ResourceStrings.None);
                    }
                    else
                    {
                        sb.AppendLine();
                        foreach (var g in m_sessionGroupDescription)
                        {
                            sb.AppendLine(string.Format("    {0} (Unique Id: {1})",
                                g.Value, g.Key.ToString()));
                        }
                    }
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }
}
