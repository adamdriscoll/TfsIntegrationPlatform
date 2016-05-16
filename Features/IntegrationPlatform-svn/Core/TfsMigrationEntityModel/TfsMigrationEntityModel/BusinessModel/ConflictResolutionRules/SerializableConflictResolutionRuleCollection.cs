// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules
{
    [Serializable]
    public class SerializableConflictResolutionRuleCollection
    {
        public List<SerializableConflictResolutionRule> Rules
        {
            get;
            set;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SerializableConflictResolutionRuleCollection()
        {
            Rules = new List<SerializableConflictResolutionRule>();
        }

        public void AddRule(SerializableConflictResolutionRule rule)
        {
            if (!Rules.Contains(rule))
            {
                Rules.Add(rule);
            }
        }
    }
}
