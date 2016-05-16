// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    public enum OperationType
    {
        Checkin,
        Lnname,
        Mkattr,
        Mkbranch,
        Mkelem,
        Mkhlink,
        Mklabel,
        Mkpool,
        Mkreplica,
        Mktype,
        Mkvob,
        Rmname, 
        Undefined
    }

    public enum OperationState
    {
        AddDirectoryToParent,
        CreateDirectoryElement,
        CreateDirectoryBranch,  //Create a directory under a specific branch
        CreateDirectoryVersion,
        Initialized
    }

    public static class OperationDescription
    {
        internal const string DirectoryElement = "directory element";
        internal const string Branch = "branch";
        internal const string DirectoryVersion = "directory version";
        internal const string Version = "version";
    }

    public class Operation
    {
        /// <summary>
        /// Matches string to EnumType and returns corresponding operation type
        /// </summary>
        /// <param name="strOperation">Operation as a string</param>
        /// <returns>Returns operation as a Enum </returns>
        public static OperationType GetOperationType(string strOperation)
        {
            OperationType operation;
            switch (strOperation)
            {
                case "checkin":
                    operation = OperationType.Checkin;
                    break;
                case "mkattr":
                    operation = OperationType.Mkattr;
                    break;
                case "mkelem":
                    operation = OperationType.Mkelem;
                    break;
                case "mkhlink":
                    operation = OperationType.Mkhlink;
                    break;
                case "mklabel":
                    operation = OperationType.Mklabel;
                    break;
                case "mkbranch":
                    operation = OperationType.Mkbranch;
                    break;
                case "mkpool":
                    operation = OperationType.Mkpool;
                    break;
                case "mkreplica":
                    operation = OperationType.Mkreplica;
                    break;
                case "lnname":
                    operation = OperationType.Lnname;
                    break;
                case "rmname":
                    operation = OperationType.Rmname;
                    break;
                case "mktype":
                    operation = OperationType.Mktype;
                    break;
                case "mkvob":
                    operation = OperationType.Mkvob;
                    break;
                default:
                    operation = OperationType.Undefined;
                    break;

            }
            return operation;
        }
    }
}