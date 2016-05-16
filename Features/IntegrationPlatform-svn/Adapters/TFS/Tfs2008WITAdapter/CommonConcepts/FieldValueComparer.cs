// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// Field value comparer; used by the analysis engine to detect field changes across revisions.
    /// </summary>
    public class FieldValueComparer
    {
        private StringComparer m_stringComparer;            // Object comparing strings in the server locale

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stringComparer">String comparer</param>
        public FieldValueComparer(
            StringComparer stringComparer)
        {
            Debug.Assert(stringComparer != null, "String comparer cannot be null!");
            m_stringComparer = stringComparer;
        }

        /// <summary>
        /// Compares two objects.
        /// </summary>
        /// <param name="o1">Object 1</param>
        /// <param name="o2">Object 2</param>
        /// <returns>True if objects are equal</returns>
        public new bool Equals(
            object o1,
            object o2)
        {
            if (IsNullOrEmptyString(o1))
            {
                return IsNullOrEmptyString(o2);
            }
            else if (IsNullOrEmptyString(o2))
            {
                return false;
            }

            // Cast to the common type
            try
            {
                o1 = Convert.ChangeType(o1, o2.GetType(), CultureInfo.InvariantCulture);

                string s1 = o1 as string;

                return s1 == null? o1.Equals(o2) : m_stringComparer.Equals(s1, (string)o2);
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether given object is null or represents an empty string.
        /// </summary>
        /// <param name="o">Object</param>
        /// <returns>True if object is null or an empty string</returns>
        private static bool IsNullOrEmptyString(
            object o)
        {
            if (o == null)
            {
                return true;
            }
            string s = o as string;
            return s != null && s.Length == 0;
        }
    }
}
