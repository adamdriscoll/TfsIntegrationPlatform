// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    public class CQStringComparer : StringComparer
    {
        private StringComparison m_stringComparison;
        private StringComparer m_stringComparer;

        private CQStringComparer(StringComparison stringComparison)
            : base()
        {
            m_stringComparison = stringComparison;
        }

        // pass-through implementations based on our current StringComparison setting
        public override int Compare(string x, string y) { return String.Compare(x, y, m_stringComparison); }
        public override bool Equals(string x, string y) { return String.Equals(x, y, m_stringComparison); }
        public override int GetHashCode(string x) { return MatchingStringComparer.GetHashCode(x); }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public int Compare(string x, int indexX, string y, int indexY, int length) { return String.Compare(x, indexX, y, indexY, length, m_stringComparison); }

        // add new useful methods here
        public bool Contains(string main, string pattern)
        {
            CheckForNull(main, "main");
            CheckForNull(pattern, "pattern");

            return main.IndexOf(pattern, m_stringComparison) >= 0;
        }

        private void CheckForNull(string argValue, string argName)
        {
            if (null == argValue)
            {
                throw new ArgumentNullException(argName);
            }
        }

        public int IndexOf(string main, string pattern)
        {
            CheckForNull(main, "main");
            CheckForNull(pattern, "pattern");

            return main.IndexOf(pattern, m_stringComparison);
        }

        public bool StartsWith(string main, string pattern)
        {
            CheckForNull(main, "main");
            CheckForNull(pattern, "pattern");

            return main.StartsWith(pattern, m_stringComparison);
        }

        public bool EndsWith(string main, string pattern)
        {
            CheckForNull(main, "main");
            CheckForNull(pattern, "pattern");

            return main.EndsWith(pattern, m_stringComparison);
        }

        private StringComparer MatchingStringComparer
        {
            get
            {
                if (m_stringComparer == null)
                {
                    switch (m_stringComparison)
                    {
                        case StringComparison.CurrentCulture:
                            m_stringComparer = StringComparer.CurrentCulture;
                            break;

                        case StringComparison.CurrentCultureIgnoreCase:
                            m_stringComparer = StringComparer.CurrentCultureIgnoreCase;
                            break;

                        case StringComparison.Ordinal:
                            m_stringComparer = StringComparer.Ordinal;
                            break;

                        case StringComparison.OrdinalIgnoreCase:
                            m_stringComparer = StringComparer.OrdinalIgnoreCase;
                            break;

                        default:
                            Debug.Fail("Unknown StringComparison value");
                            m_stringComparer = StringComparer.Ordinal;
                            break;
                    }
                }
                return m_stringComparer;
            }
        }

        private static CQStringComparer s_ordinal = new CQStringComparer(StringComparison.Ordinal);
        private static CQStringComparer s_ordinalIgnoreCase = new CQStringComparer(StringComparison.OrdinalIgnoreCase);
        private static CQStringComparer s_currentCulture = new CQStringComparer(StringComparison.CurrentCulture);
        private static CQStringComparer s_currentCultureIgnoreCase = new CQStringComparer(StringComparison.CurrentCultureIgnoreCase);

        public static CQStringComparer EntityName { get { return s_ordinalIgnoreCase; } }
        public static CQStringComparer RecordName { get { return s_ordinal; } }
        public static CQStringComparer FieldName { get { return s_ordinal; } }
        public static CQStringComparer StateName { get { return s_ordinal; } }  

        // these are adapter-specific comparers
        public static CQStringComparer LinkArtifactType { get { return s_ordinal; } }
        public static CQStringComparer LinkType { get { return s_ordinal; } }
    }
}
