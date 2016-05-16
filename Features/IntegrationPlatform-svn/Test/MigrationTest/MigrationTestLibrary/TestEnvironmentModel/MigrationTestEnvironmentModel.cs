// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace MigrationTestLibrary
{
    public struct Declarations
    {
        public const string SchemaVersion = "";
    }

    [Serializable]
    public enum TFSVersionEnum
    {
        TFS2008,
        TFS2010,
        ClearCaseV6,
        ClearCaseTFS,
    }
}
