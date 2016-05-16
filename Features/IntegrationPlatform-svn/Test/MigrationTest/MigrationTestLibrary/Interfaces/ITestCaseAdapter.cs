// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    /// <summary>
    /// An interface that E2E migration test case adapter should implement
    /// </summary>
    public interface ITestCaseAdapter
    {
        string FilterString { get; }

        void Initialize(TCAdapterEnvironment adapterEnvironment);
        void Cleanup();
    }
}
