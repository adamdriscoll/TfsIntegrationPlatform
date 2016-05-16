// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions
{
    internal class ClearQuestInsufficientPrivilegeException : Exception
    {
        public ClearQuestInsufficientPrivilegeException(
            string userName,
            string userPrivilegeValue)
        {
            UserName = userName;
            UserPrivilegeValue = userPrivilegeValue;
        }

        public string UserName
        {
            get;
            private set;
        }

        public string UserPrivilegeValue
        {
            get;
            private set;
        }
    }
}
