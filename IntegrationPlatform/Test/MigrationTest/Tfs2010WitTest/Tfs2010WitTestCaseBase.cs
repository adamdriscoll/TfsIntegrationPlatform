// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TfsWitTest;

namespace Tfs2010WitTest
{
    public class Tfs2010WitTestCaseBase : TfsWITTestCaseBase
    {
        protected override string TestProjectName
        {
            get
            {
                return "Tfs2010WITTest";
            }
        }

    }
}
