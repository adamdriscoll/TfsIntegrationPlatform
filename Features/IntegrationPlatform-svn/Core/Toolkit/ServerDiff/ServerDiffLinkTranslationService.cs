// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Linking;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff
{
    internal class ServerDiffLinkTranslationService : ILinkTranslationService, IServiceProvider
    {
        private ServerDiffEngine m_serverDiffEngine;
        private LinkConfigurationLookupService m_linkConfigurationLookupService;
        private Guid m_sourceId;

        internal ServerDiffLinkTranslationService(
            ServerDiffEngine serverDiffEngine, 
            Guid sourceId, 
            LinkConfigurationLookupService linkConfigurationLookupService)
        {
            m_serverDiffEngine = serverDiffEngine;
            m_sourceId = sourceId;
            m_linkConfigurationLookupService = linkConfigurationLookupService;
        }

        #region ILinkTranslationService Members
        public LinkConfigurationLookupService LinkConfigurationLookupService
        {
            get { return m_linkConfigurationLookupService; }
        }

        public bool LinkTypeSupportedByOtherSide(string linkTypeReferenceName)
        {
            return m_serverDiffEngine.LinkTypeSupportedByOtherSide(linkTypeReferenceName, m_sourceId);
        }
        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (!serviceType.Equals(typeof(ILinkTranslationService)))
            {
                return null;
            }

            return this;
        }

        #endregion
    }
}