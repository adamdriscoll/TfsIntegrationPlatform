// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This attribute is used to describe the capability of a migration Provider, i.e. 
    /// what session type and which endpoint system it supports.
    /// This is an optional attribute for classes that implements the IProvider interface.
    /// </summary>
    public class ProviderCapabilityAttribute : Attribute
    {
        /// <summary>
        /// Gets the session type that this provider can be used for.
        /// </summary>
        public SessionTypeEnum? SessionType { get; set; }

        /// <summary>
        /// Gets the Endpoint System's Name, e.g. TFS
        /// </summary>
        public string EndpointSystemName { get; set; }

        /// <summary>
        /// Default constructor that sets SessionType to NULL and EndpointSystemName to "Unknown"
        /// </summary>
        public ProviderCapabilityAttribute()
        {
            Initialize(null, MigrationToolkitResources.UnknownEndpointSystem);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sessionType">Session type that the provider supports</param>
        /// <param name="endpointSystemName">The name of the endpoint system that the provider supports</param>
        public ProviderCapabilityAttribute(
            SessionTypeEnum sessionType,
            string endpointSystemName)
        {
            Initialize(sessionType, endpointSystemName);
        }

        private void Initialize(SessionTypeEnum? sessionType, string endpointSystemName)
        {
            SessionType = sessionType;
            EndpointSystemName = endpointSystemName;
        }
    }
}
