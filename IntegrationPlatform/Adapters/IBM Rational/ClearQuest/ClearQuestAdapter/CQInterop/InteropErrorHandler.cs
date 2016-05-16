// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.Exceptions;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop
{
    internal class InteropErrorHandler
    {
        /// <summary>
        /// Generic handler for all CQ calls.
        /// Except for user logon and admin logon case.
        /// </summary>
        /// <param name="cEx"></param>
        public static void HandleCQException(COMException cEx)
        {
            string errMsg = UtilityMethods.Format(CQResource.CQ_COM_ERROR, cEx.Message);

            if (cEx.Message.Contains("80040154"))
            {
                //System.Runtime.InteropServices.COMException (0x80040154)
                throw new ClearQuestCOMDllNotFoundException(errMsg, cEx);
            }
            else
            {
                throw new ClearQuestCOMCallException(errMsg, cEx);
            }
        }

        // if the COM dll is not found by .NET, it throws IOException
        public static void HandleIOException(IOException cEx)
        {
            string errMsg = UtilityMethods.Format(CQResource.CQ_COM_ERROR, cEx.Message);
            TraceManager.TraceException(cEx);
            throw new ClearQuestCOMDllNotFoundException(errMsg, cEx);
        }
    }
}
