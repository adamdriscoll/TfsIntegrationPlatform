// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    internal static class IntegerRange
    {
        public static readonly string RevisionRangeDelimiter = "-";
        public static readonly string RevisionListDelimiter = ",";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="revisionRanges"></param>
        /// <param name="revisions"></param>
        /// <returns></returns>
        /// <remarks>
        /// revisionRanges can be in the following syntax:
        ///     - rev1
        ///     - rev1,rev3,rev4
        ///     - rev2-rev5 (only applies to integer)
        /// </remarks>
        internal static bool TryParseRangeString(string revisionRanges, out int[] revisions)
        {
            revisions = null;

            if (string.IsNullOrEmpty(revisionRanges))
            {
                // cannot recognize empty string
                return false;
            }

            if (revisionRanges.Contains(RevisionListDelimiter) && !revisionRanges.Contains(RevisionRangeDelimiter))
            {
                // likely in this form: rev1,rev3,rev4
                string[] splits = revisionRanges.Trim().Split(RevisionListDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                revisions = new int[splits.Length];
                for (int i = 0; i < revisions.Length; ++i)
                {
                    int val;
                    if (!int.TryParse(splits[i], out val))
                    {
                        return false;
                    }
                    revisions[i] = val;
                }

                Array.Sort(revisions);
                return true;
            }
            else if (!revisionRanges.Contains(RevisionListDelimiter) && revisionRanges.Contains(RevisionRangeDelimiter))
            {
                // likely in this form: rev2-rev5 (only applies to integer)
                if (revisionRanges.IndexOf(RevisionRangeDelimiter) != revisionRanges.LastIndexOf(RevisionRangeDelimiter))
                {
                    // more than two '-': incorrect syntax
                    return false;
                }

                string[] splits = revisionRanges.Trim().Split(RevisionRangeDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length != 2)
                {
                    return false;
                }

                int minRev;
                int maxRev;
                if (!int.TryParse(splits[0], out minRev)
                    || !int.TryParse(splits[1], out maxRev)
                    || minRev > maxRev)
                {
                    return false;
                }

                revisions = new int[maxRev - minRev + 1];
                for (int i = 0; i < revisions.Length; ++i)
                {
                    revisions[i] = minRev + i;
                }

                Array.Sort(revisions);
                return true;
            }
            else
            {
                // in this form: rev1
                int val;
                if (!int.TryParse(revisionRanges.Trim(), out val))
                {
                    return false;
                }

                revisions = new int[] { val };
                return true;
            }
        }
    }
}
