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
    public class SerializableProvider
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

	    public string ProviderVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor to support serialization
        /// </summary>
        public SerializableProvider()
        {
        }

        public SerializableProvider(Provider edmProvider)
        {
            if (null == edmProvider)
            {
                throw new ArgumentNullException("edmProvider");
            }

            this.StorageId = edmProvider.Id;
            this.ReferenceName = edmProvider.ReferenceName;
            this.FriendlyName = edmProvider.FriendlyName;
            this.ProviderVersion = edmProvider.ProviderVersion;
        }
    }
}
