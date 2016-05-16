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
    public static class InvalidFieldConflictTypeConstants
    {
        private static List<ResolutionAction> m_supportedActions =
            new List<ResolutionAction>();
        private static List<string> m_supportedConflictDetailsPropertyKeys =
            new List<string>();

        static InvalidFieldConflictTypeConstants()
        {
            m_supportedActions.Add(new InvalidFieldConflictUseFieldMapAction());
            m_supportedActions.Add(new InvalidFieldConflictDropFieldAction());
            m_supportedActions.Add(new UpdatedConfigurationResolutionAction());
            m_supportedActions.Add(new ManualConflictResolutionAction());
            m_supportedActions.Add(new SkipConflictedActionResolutionAction());

            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_SourceWorkItemId);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_SourceWorkItemRevision);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_SourceFieldRefName);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetTeamProject);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetWorkItemType);
            m_supportedConflictDetailsPropertyKeys.Add(ConflictDetailsKey_TargetTeamFoundationServerUrl);
        }

        public static Guid ReferenceName
        {
            get
            {
                return new Guid("EA1A518B-248B-43e3-90B2-62FE7EB5F366");
            }
        }

        public const string FriendlyName = "TFS WIT invalid field conflict type";

        public static ReadOnlyCollection<ResolutionAction> SupportedActions
        {
            get
            {
                return m_supportedActions.AsReadOnly();
            }
        }

        public const string ConflictDetailsKey_SourceWorkItemId = "SourceWorkItemID";
        public const string ConflictDetailsKey_SourceWorkItemRevision = "SourceWorkItemRevision";
        public const string ConflictDetailsKey_SourceFieldRefName = "SourceFieldRefName";
        public const string ConflictDetailsKey_TargetTeamProject = "TargetTeamProject";
        public const string ConflictDetailsKey_TargetWorkItemType = "TargetWorkItemType";
        public const string ConflictDetailsKey_TargetTeamFoundationServerUrl = "TargetTeamFoundationServerUrl";

        public static ReadOnlyCollection<string> SupportedConflictDetailsPropertyKeys
        {
            get
            {
                return m_supportedConflictDetailsPropertyKeys.AsReadOnly();
            }
        }
    }
}
