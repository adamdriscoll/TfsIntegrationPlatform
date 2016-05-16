// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.ServiceModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    [ServiceContract]
    public interface IRuntimeTrace
    {
        [OperationContract]
        string[] GetTraceMessages();
    }
}
