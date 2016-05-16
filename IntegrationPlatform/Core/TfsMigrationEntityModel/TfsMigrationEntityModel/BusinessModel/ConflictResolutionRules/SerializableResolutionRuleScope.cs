// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.ConflictResolutionRules
{
    [Serializable]
    public class SerializableResolutionRuleScope
    {
        public int StorageId
        {
            get;
            set;
        }

        public string Scope
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SerializableResolutionRuleScope()
        {
        }

        public SerializableResolutionRuleScope(ConfigConflictResolutionRuleScope edmScope)
        {
            if (null == edmScope)
            {
                throw new ArgumentNullException("scope");
            }

            this.StorageId = edmScope.Id;
            this.Scope = edmScope.Scope;
        }
    }
}
