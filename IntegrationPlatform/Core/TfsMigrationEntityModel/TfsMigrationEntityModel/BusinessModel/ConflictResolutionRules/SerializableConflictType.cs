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
    public class SerializableConflictType
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
        
        public string DescriptionDoc
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
        /// Default constructor
        /// </summary>
        public SerializableConflictType()
        {
        }

        public SerializableConflictType(ConfigConflictType edmConflictType)
        {
            if (null == edmConflictType)
            {
                throw new ArgumentNullException("edmConflictType");
            }

            this.StorageId = edmConflictType.Id;            
            this.ReferenceName = edmConflictType.ReferenceName;
            this.FriendlyName = edmConflictType.FriendlyName;
            this.DescriptionDoc = edmConflictType.DescriptionDoc;
            this.IsActive = edmConflictType.IsActive;
            edmConflictType.ProviderReference.Load();
            this.Provider = edmConflictType.Provider == null 
                ? null : new SerializableProvider(edmConflictType.Provider);
        }
    }
}
