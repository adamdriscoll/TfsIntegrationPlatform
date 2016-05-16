// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    internal interface IVCServerPathTranslationService
    {
        /// <summary>
        /// Translates a source-endpoint-specific server path to the target-endpoint-specific one.
        /// </summary>
        /// <param name="srcMigrationSourceId">The migration source unique id, to which the source server path belongs</param>
        /// <param name="srcServerPath">The source server path to be translated</param>
        /// <returns>A server path that's specific the other side of the migration pipeline</returns>
        string Translate(Guid srcMigrationSourceId, string srcServerPath);
    }
}
