// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    internal static class Constants
    {
        internal const string HwmDelta = "HWMDelta";
    }

    internal static class ClearToolCommand
    {
        internal const string setcs = "setcs  '{0}' ";
        internal const string catcs = "catcs ";
        internal const string startview = "startview {0} ";
        internal const string update = "update -noverwrite {0} ";
        internal const string mount = "mount {0} ";
        internal const string mkviewSnapshot = "mkview -snapshot -tag {0} {1} ";
        internal const string rmview = "rmview -force {0} ";
        internal const string lscheckoutNonRecursive = "lscheckout -short -cview -directory {0}";
        internal const string lscheckoutRecursive = "lscheckout -short -cview -recurse {0}";
        internal const string findSymbolicLink = "find {0} -type l -print";
    }
}
