// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFSIntegrationAdmin.Interfaces;

namespace TFSIntegrationAdmin.DeleteSessionGroupCmd
{
    class DeleteSessionGroupRslt : CommandResultBase
    {
        Guid m_sessionGroupId;
        Dictionary<Guid, string> m_nonActiveGroups;

        /// <summary>
        /// Constructor for successful delete group execution
        /// </summary>
        /// <param name="command"></param>
        /// <param name="sessionGroupId"></param>
        public DeleteSessionGroupRslt(
            ICommand command,
            Guid sessionGroupId)
            : base(true, command)
        {
            m_sessionGroupId = sessionGroupId;
        }

        /// <summary>
        /// Constructor for successful list group execution
        /// </summary>
        /// <param name="command"></param>
        public DeleteSessionGroupRslt(
            ICommand command,
            Dictionary<Guid, string> nonActiveGroups)
            : base(true, command)
        {
            m_sessionGroupId = Guid.Empty;
            m_nonActiveGroups = nonActiveGroups;
        }

        /// <summary>
        /// Constructor for failed execution
        /// </summary>
        /// <param name="e"></param>
        /// <param name="command"></param>
        /// <param name="sessionGroupId"></param>
        public DeleteSessionGroupRslt(
            Exception e,
            ICommand command)
            : base(false, command)
        {
            m_sessionGroupId = Guid.Empty;
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
            else
            {
                if (!m_sessionGroupId.Equals(Guid.Empty))
                {
                    sb.AppendFormat(ResourceStrings.DeleteSessionGroupSucceedInfoFormat, m_sessionGroupId);
                    sb.AppendLine();
                }
                else if (m_nonActiveGroups != null)
                {
                    sb.Append(ResourceStrings.ListSessionGroupSuccessInfoHeader);
                    if (m_nonActiveGroups.Count > 0)
                    {
                        sb.AppendLine();
                        foreach (var sessionGroup in m_nonActiveGroups)
                        {
                            sb.AppendFormat("  '{0}' (Unique Id: {1})\n", sessionGroup.Value, sessionGroup.Key);
                        }
                    }
                    else
                    {
                        sb.Append("None");
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }
}
