// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;

namespace TFSMigrationService
{
    [ServiceContract]
    public interface IRuntimeTrace
    {
        [OperationContract]
        string[] GetTraceMessages();
    }
}
