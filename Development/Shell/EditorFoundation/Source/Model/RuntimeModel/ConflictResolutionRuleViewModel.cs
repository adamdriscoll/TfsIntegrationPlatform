// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ConflictResolutionRuleViewModel
    {
        RTResolutionRule m_resolutionRule;

        public ConflictResolutionRuleViewModel(RTResolutionRule resolutionRule)
        {
            m_resolutionRule = resolutionRule;
        }

        #region Properties
        // Selected RTResolutionRule properties
        public int Id { get { return m_resolutionRule.Id; } }
        public DateTime CreationTime { get { return m_resolutionRule.CreationTime; } }
        public DateTime? DeprecationTime { get { return m_resolutionRule.DeprecationTime; } }
        public string RuleData { get { return m_resolutionRule.RuleData; } }
        public Guid ReferenceName { get { return m_resolutionRule.ReferenceName; } }
        public Guid ScopeInfoUniqueId { get { return m_resolutionRule.ScopeInfoUniqueId; } }
        public Guid SourceInfoUniqueId { get { return m_resolutionRule.SourceInfoUniqueId; } }
        // TODO: Translate to human readable form/enum
        public int Status { get { return m_resolutionRule.Status; } }

        // Flatten the RuntimeEntityModel a bit in the view model
        public string Scope 
        {
            get
            {
                if (! m_resolutionRule.ScopeReference.IsLoaded)
                {
                    m_resolutionRule.ScopeReference.Load();
                }

                return m_resolutionRule.Scope.Scope;
            }
        }
        #endregion
    }
}
