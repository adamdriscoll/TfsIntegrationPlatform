// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts.HistoryNotFoundResolution
{
    internal class MigrationException : Exception
    {
        public ConversionResult ConversionResult { get; private set; }

        public MigrationException(ConversionResult changeResult, Exception ex)
            : base("Migration failed", ex)
        {
            ConversionResult = changeResult;
        }
    }

}
