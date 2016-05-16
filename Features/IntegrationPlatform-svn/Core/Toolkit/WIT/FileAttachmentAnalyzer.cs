// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// File attchment analyzer; used to detect conflicts in file attachments collections.
    /// </summary>
    public class BasicFileAttachmentComparer : IComparer<IMigrationFileAttachment>, IEqualityComparer<IMigrationFileAttachment>
    {
        #region Comparison method

        /// <summary>
        /// Compares two attachments.
        /// </summary>
        /// <param name="a1">Attachment 1</param>
        /// <param name="a2">Attachment 2</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public int Compare(
            IMigrationFileAttachment a1,
            IMigrationFileAttachment a2)
        {
            Debug.Assert(a1 != null && a2 != null, "Null attachment!");

            // Size
            int res = a1.Length.CompareTo(a2.Length);

            //if (res == 0 && (m_flags & AttachmentComparisonAttributes.CreateTime) != 0)
            //{
            //    res = CompareTimes(a1.UtcCreationDate, a2.UtcCreationDate);
            //}
            //if (res == 0 && (m_flags & AttachmentComparisonAttributes.LastWriteTime) != 0)
            //{
            //    res = CompareTimes(a1.UtcLastWriteDate, a2.UtcLastWriteDate);
            //}
            if (res == 0)
            {
                res = string.Compare(a1.Name, a2.Name, true, CultureInfo.InvariantCulture);
            }
            return res;
        }

        #endregion

        public bool Equals(IMigrationFileAttachment x, IMigrationFileAttachment y)
        {
            return 0 == this.Compare(x, y);
        }

        public int GetHashCode(IMigrationFileAttachment obj)
        {
            return obj.GetHashCode();
        }
    }
}

