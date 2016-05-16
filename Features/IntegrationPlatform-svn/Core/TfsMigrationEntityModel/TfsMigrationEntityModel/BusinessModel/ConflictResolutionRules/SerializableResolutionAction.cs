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
    public class SerializableResolutionAction
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

        public string FriendlyName 
        {
            get;
            set;
        }
        
        public bool? IsActive
        {
            get;
            set;
        }

        public SerializableProvider Provider
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SerializableResolutionAction()
        { }

        public SerializableResolutionAction(ConfigConflictResolutionAction edmAction)
        {
            if (null == edmAction)
            {
                throw new ArgumentNullException("edmAction");
            }

            this.StorageId = edmAction.Id;
            this.ReferenceName = edmAction.ReferenceName;
            this.FriendlyName = edmAction.FriendlyName;
            this.IsActive = edmAction.IsActive;
            edmAction.ProviderReference.Load();
            this.Provider = edmAction.Provider == null ?
                null : new SerializableProvider(edmAction.Provider);
        }
    }
}
