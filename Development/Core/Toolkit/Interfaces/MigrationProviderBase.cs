// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This base class provides the basic implementation of IMigrationProvider interface
    /// </summary>
    public abstract class MigrationProviderBase : IMigrationProvider
    {
        public abstract ConversionResult ProcessChangeGroup(ChangeGroup changeGroup);
        
        public abstract void InitializeServices(IServiceContainer analysisServiceContainer);
        
        public abstract void InitializeClient();
        
        public abstract void RegisterConflictTypes(ConflictManager conflictManager);
        
        public virtual void EstablishContext(ChangeGroupService sourceSystemChangeGroupService)
        {
            return;
        }

        public virtual object GetService(Type serviceType)
        {
            if (serviceType == typeof(IMigrationProvider))
            {
                return this;
            }

            return null;
        }

        public virtual void Dispose()
        {
        }
    }
}
