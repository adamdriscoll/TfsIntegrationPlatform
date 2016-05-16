// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQNotesLogHeader
    {
        public const string NotesLogHeaderIdentifier = "====";

        static readonly string[] HeaderSplitStrs = new string[] { "State:", " by: ", " on " };        
        const int AuthorColumnIndex = 1;
        const int DateColumnIndex = 2;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headerStr"></param>
        /// <remarks>
        /// CQ standard Notes package uses a header like the following:
        /// "==== State: Assigned by: teyang on 16 November 2009 16:35:51 ===="
        /// </remarks>
        public CQNotesLogHeader(string headerStr)
        {
            var hSplits = SplitHeaderString(headerStr);
            Author = hSplits[AuthorColumnIndex];
            ChangeDate = DateTime.Parse(hSplits[DateColumnIndex]);
        }

        public string Author
        {
            get;
            private set;
        }

        public DateTime ChangeDate
        {
            get;
            private set;
        }

        public static bool TryParse(string headerStr)
        {
            try
            {
                var hSplits = SplitHeaderString(headerStr);
                DateTime.Parse(hSplits[DateColumnIndex]);
                return true;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        private static string[] SplitHeaderString(string headerStr)
        {
            headerStr = headerStr.Trim();
            headerStr = headerStr.Trim(NotesLogHeaderIdentifier.ToCharArray());
            headerStr = headerStr.Trim();

            var hSplits = headerStr.Split(HeaderSplitStrs, StringSplitOptions.RemoveEmptyEntries);
            return hSplits;
        }
    }
}
