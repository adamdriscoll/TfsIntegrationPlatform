// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.EntityModel;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules
{
    [Serializable]
    public class SerializableConflictResolutionRule
    {
        public int StorageId
        {
            get;
            set;
        }

        public Guid ReferenceName
        {
            get;
            set;
        }

        public SerializableConflictType ConflictType
        {
            get;
            set;
        }
    
        public SerializableResolutionAction ResolutionAction
        {
            get;
            set;
        }

        public SerializableResolutionRuleScope Scope
        {
            get;
            set;
        }

        public Guid ScopeInfoUniqueId
        {
            get;
            set;
        }

        public Guid SourceInfoUniqueId
        {
            get;
            set;
        }

        public string RuleDataXmlDocString
        {
            get;
            set;
        }

        public int Status
        {
            // This could be a param for exporting rules
            // int NOT NULL --Status is Valid (0), Proposed (1), or Deprecated (2)
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SerializableConflictResolutionRule()
        { }

        public SerializableConflictResolutionRule(ConfigConflictResolutionRule edmResolutionRule)
        {
            this.StorageId = edmResolutionRule.Id;
            this.ReferenceName = edmResolutionRule.ReferenceName;
            edmResolutionRule.ConflictTypeReference.Load();
            this.ConflictType = new SerializableConflictType(edmResolutionRule.ConflictType);
            edmResolutionRule.ResolutionActionReference.Load();
            this.ResolutionAction = new SerializableResolutionAction(edmResolutionRule.ResolutionAction);
            edmResolutionRule.RuleScopeReference.Load();
            this.Scope = new SerializableResolutionRuleScope(edmResolutionRule.RuleScope);
            this.ScopeInfoUniqueId = edmResolutionRule.ScopeInfoUniqueId;
            this.SourceInfoUniqueId = edmResolutionRule.SourceInfoUniqueId;
            this.RuleDataXmlDocString = edmResolutionRule.RuleData;
            this.Status = edmResolutionRule.Status;
        }
    }
}

	
		
	
	
	