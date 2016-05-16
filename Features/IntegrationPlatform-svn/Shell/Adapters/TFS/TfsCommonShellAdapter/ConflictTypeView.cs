// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.Shell.View;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public class ConflictTypeView : IConflictTypeView
    {
        #region IConflictTypeView Members

        public Guid Guid { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public Type Type { get; set; }

        #endregion
    }
}
