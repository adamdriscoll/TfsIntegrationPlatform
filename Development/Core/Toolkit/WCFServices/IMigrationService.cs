// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    public enum SessionGroupInitializationStatus
    {
        Unknown,
        Initializing,
        Initialized,
        NotInitialized,
    }

    [ServiceContract]
    public interface IMigrationService
    {
        [OperationContract]
        void StartSessionGroup(Guid sessionGroupUniqueId);

        [OperationContract]
        SessionGroupInitializationStatus GetSessionGroupInitializationStatus(Guid sessionGroupUniqueId);

        [OperationContract]
        void StartSingleSessionInSessionGroup(Guid sessionGroupUniqueId, Guid sessionUniqueId);

        [OperationContract]
        void StopSessionGroup(Guid sessionGroupUniqueId);

        [OperationContract]
        void PauseSessionGroup(Guid sessionGroupUniqueId);

        [OperationContract]
        void ResumeSessionGroup(Guid sessionGroupUniqueId);

        [OperationContract]
        List<Guid> GetRunningSessionGroups();
    }
}
