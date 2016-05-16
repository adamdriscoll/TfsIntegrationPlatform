// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WIT
{
    internal class DefaultMigrationFileAttachmentComparer : IComparer<IMigrationFileAttachment>
    {
        #region IComparer<IMigrationFileAttachment> Members

        public int Compare(IMigrationFileAttachment x, IMigrationFileAttachment y)
        {
            int res = 0;

            res = x.Length.CompareTo(y.Length);
            if (res != 0)
            {
                return res;
            }

            res = x.Name.CompareTo(y.Name);
            if (res != 0)
            {
                return res;
            }

            if (x.Comment == null)
            {
                if (y.Comment != null)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
            if (y.Comment == null)
            {
                return 1;
            }

            res = x.Comment.CompareTo(y.Comment);
            return res;
        }

        #endregion
    }
}
