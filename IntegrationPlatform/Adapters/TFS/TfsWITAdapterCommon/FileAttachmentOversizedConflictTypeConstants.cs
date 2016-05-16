// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common
{
    public static class FileAttachmentOversizedConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();
        private static List<string> m_supportedConflictDetailsPropertyKeys =
            new List<string>();

        static FileAttachmentOversizedConflictTypeConstants()
        {
            m_supportedActions.Add(new FileAttachmentOversizedConflictDropAttachmentAction());
            m_supportedActions.Add(new ManualConflictResolutionAction());
            m_supportedActions.Add(new SkipConflictedActionResolutionAction());

            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_AttachmentName);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_FileSize);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_MaxAttachmentSize);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_WorkItemId);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_ServerName);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TeamProject);
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("5EC7F170-E36C-4ea2-96F8-69DECDE0279C");
            }
        }

        public const string FriendlyName = "TFS WIT attachment file oversized";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }

        public const string ConflictDetailsKey_AttachmentName = "AttachmentName";
        public const string ConflictDetailsKey_FileSize = "FileSize";
        public const string ConflictDetailsKey_MaxAttachmentSize = "MaxAttachmentSize";
        public const string ConflictDetailsKey_WorkItemId = "WorkItemId";
        public const string ConflictDetailsKey_ServerName = "ServerName";
        public const string ConflictDetailsKey_TeamProject = "TeamProject";

        public static ReadOnlyCollection<string> SupportedConflictDetailsPropertyKeys
        {
            get
            {
                return m_supportedConflictDetailsPropertyKeys.AsReadOnly();
            }
        }
    }
}
