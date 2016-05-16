// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions
{
    public class ClearQuestCOMDllNotFoundException : Exception
    {
        public ClearQuestCOMDllNotFoundException()
        {
        }

        public ClearQuestCOMDllNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
