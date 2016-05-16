// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    internal class SubversionVCMigrationProvider : IMigrationProvider
    {
        #region Private Members

        private IServiceContainer m_analysisServiceContainer;
        private ChangeGroupService m_changeGroupService;

        #endregion

        #region IMigrationProvider impelementation

        public ConversionResult ProcessChangeGroup(ChangeGroup changeGroup)
        {
            throw new NotImplementedException();
        }

        public void InitializeServices(IServiceContainer analysisServiceContainer)
        {
            m_analysisServiceContainer = analysisServiceContainer;

            m_changeGroupService = (ChangeGroupService)m_analysisServiceContainer.GetService(typeof(ChangeGroupService));
            m_changeGroupService.RegisterDefaultSourceSerializer(new SubversionMigrationItemSerialzier());
        }

        public void InitializeClient()
        {
        }

        public void RegisterConflictTypes(ConflictManager conflictManager)
        {
            
        }

        public void EstablishContext(ChangeGroupService sourceSystemChangeGroupService)
        {
            
        }

        #endregion

        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            return (IServiceProvider)this;
        }
        #endregion

        #region IDispose implementation
        public void Dispose()
        {
        }
        #endregion
    }
}
