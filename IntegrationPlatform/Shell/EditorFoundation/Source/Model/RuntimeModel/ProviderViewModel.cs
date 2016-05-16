// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ProviderViewModel : ModelObject
    {
        RTProvider m_provider;

        public ProviderViewModel(RTProvider provider)
        {
            m_provider = provider;
        }

        #region Properties
        // RTMigrationSource
        public int Id
        {
            get { return m_provider.Id; }
        }

        public Guid ReferenceName
        {
            get { return m_provider.ReferenceName; }
        }

        public string FriendlyName
        {
            get { return m_provider.FriendlyName; }
        }
        #endregion
    }
}
