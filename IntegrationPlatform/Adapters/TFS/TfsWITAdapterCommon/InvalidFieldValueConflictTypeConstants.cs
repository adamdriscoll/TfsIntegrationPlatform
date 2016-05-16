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
    public static class InvalidFieldValueConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();
        private static List<string> m_supportedConflictDetailsPropertyKeys =
            new List<string>();

        static InvalidFieldValueConflictTypeConstants()
        {
            m_supportedActions.Add(new InvalidFieldValueConflictUseValueMapAction());
            m_supportedActions.Add(new InvalidFieldConflictDropFieldAction());
            m_supportedActions.Add(new UpdatedConfigurationResolutionAction());
            m_supportedActions.Add(new ManualConflictResolutionAction());
            m_supportedActions.Add(new SkipConflictedActionResolutionAction());

            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_SourceWorkItemID);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_SourceWorkItemRevision);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetTeamProject);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetWorkItemType);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetFieldRefName);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetFieldDispName);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetFieldOriginalValue);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetFieldCurrentValue);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetTeamFoundationServerUrl);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_Reason);
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("8FA45DDA-60E3-4015-A1AA-66D538060080");
            }
        }

        public const string FriendlyName = "TFS WIT invalid field value conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }

        public const string ConflictDetailsKey_SourceWorkItemID = "SourceWorkItemID";
        public const string ConflictDetailsKey_SourceWorkItemRevision = "SourceWorkItemRevision";
        public const string ConflictDetailsKey_TargetTeamProject = "TargetTeamProject";
        public const string ConflictDetailsKey_TargetWorkItemType = "TargetWorkItemType";
        public const string ConflictDetailsKey_TargetFieldRefName = "TargetFieldRefName";
        public const string ConflictDetailsKey_TargetFieldDispName = "TargetFieldDispName";
        public const string ConflictDetailsKey_TargetFieldOriginalValue = "TargetFieldOriginalValue";
        public const string ConflictDetailsKey_TargetFieldCurrentValue = "TargetFieldCurrentValue";
        public const string ConflictDetailsKey_TargetTeamFoundationServerUrl = "TargetTeamFoundationServerUrl";
        public const string ConflictDetailsKey_Reason = "Reason";

        public static ReadOnlyCollection<string> SupportedConflictDetailsPropertyKeys
        {
            get
            {
                return m_supportedConflictDetailsPropertyKeys.AsReadOnly();
            }
        }
    }
}
