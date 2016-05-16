// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    internal static class CCTextParser
    {
        /// <summary>
        /// Parse one line of  error output from clearfsimport. 
        /// Return the error message if it is a real exception.
        /// </summary>
        /// <param name="errorOutput"></param>
        internal static string ParseClearfsimportErrorOutput(string errorOutputLine)
        {
            TraceManager.TraceWarning(errorOutputLine);
            if (string.IsNullOrEmpty(errorOutputLine))
            {
                return null;
            }
            if (!errorOutputLine.StartsWith(
                "clearfsimport: Warning:")
            && !errorOutputLine.StartsWith(
                "A separate update may need to be performed in order to reflect the results of the operation in the snapshot view."))
            {
                return null;
            }
            return errorOutputLine;
        }

        internal static List<string> ParseLSOutput(string LSOutput)
        {
            if (string.IsNullOrEmpty(LSOutput))
            {
                return null;
            }
            List<string> LSList = new List<string>();
            string currentLine;
            using (StringReader outputReader = new StringReader(LSOutput))
            {
                while (true)
                {
                    currentLine = outputReader.ReadLine();
                    if (currentLine == null)
                    {
                        break;
                    }
                    else
                    {
                        // Todo verify it is a valid path
                        LSList.Add(currentLine);
                    }
                }
            }
            return LSList;
        }

        #region Com Exception
        /// <summary>
        /// Parse the text message of a COM exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        internal static COMExceptionResult ProcessComException(COMException ex)
        {
            if (ex.Message.StartsWith("Attribute type not found"))
            {
                return COMExceptionResult.AttributeTypeNotFound;
            }
            else if (ex.Message.StartsWith("Invalid name"))
            {
                return COMExceptionResult.InvalidName;
            }
            else if (ex.Message.StartsWith("Pathname not found"))
            {
                return COMExceptionResult.PathNotFound;
            }
            else if (ex.Message.StartsWith("Unable to access"))
            {
                return COMExceptionResult.NotAccessible;
            }
            else if (ex.Message.StartsWith("Unable to determine view"))
            {
                return COMExceptionResult.UnableToDetermineDynamicView;
            }
            else if (ex.Message.StartsWith("Branch type not found"))
            {
                return COMExceptionResult.BranchTypeNotFound;
            }
            else
            {
                return COMExceptionResult.Unhandled;
            }
        }
        #endregion
    }

    enum COMExceptionResult
    {
        AttributeTypeNotFound,
        InvalidName,
        PathNotFound,
        NotAccessible,
        UnableToDetermineDynamicView,
        BranchTypeNotFound,
        Unhandled
    }


    enum ClearfsimportResult
    {
        CreateElement,
        CreateDirectory,
        NoChange,
        EditElement,
        ValidatingElement,
        ValidatingDirectory,
        UpdateElement,
        IdenticalElement,
        Initialize
    }
}
