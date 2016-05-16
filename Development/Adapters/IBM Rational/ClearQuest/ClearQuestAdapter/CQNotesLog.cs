// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQNotesLog
    {
        public static List<CQNotesLog> Parse(string notesLog)
        {
            List<CQNotesLog> retval = new List<CQNotesLog>();

            notesLog = notesLog.Trim("\r\n".ToCharArray());
            notesLog = notesLog.Trim("\n".ToCharArray());

            string[] splits = notesLog.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            int newHeadLineIndex = -1;
            for (int i = 0; i < splits.Length; ++i)
            {
                if (CQNotesLogHeader.TryParse(splits[i]))
                {
                    if (newHeadLineIndex >= 0)
                    {
                        // there was a recorded headline index
                        // now that we identified a newer one, let's create the CQNotesLog for that one first
                        CQNotesLog log = CreateNotesLog(newHeadLineIndex, i, splits);
                        retval.Add(log);
                    }

                    newHeadLineIndex = i;
                }
            }

            if (newHeadLineIndex >= 0)
            {
                // process the last note log
                CQNotesLog log = CreateNotesLog(newHeadLineIndex, splits.Length, splits);
                retval.Add(log);
            }

            return retval;
        }

        private static CQNotesLog CreateNotesLog(
            int headLineIndex, 
            int lastContentLineIndexNonInclusive,
            string[] allNotesLogContent)
        {
            StringBuilder sb = new StringBuilder();
            for (int logContentLineIndex = headLineIndex + 1; 
                 logContentLineIndex < lastContentLineIndexNonInclusive; 
                 ++logContentLineIndex)
            {
                sb.AppendLine(allNotesLogContent[logContentLineIndex]);
            }

            return new CQNotesLog(allNotesLogContent[headLineIndex], sb.ToString());
        }

        private CQNotesLog(
            string header,
            string logContent)
        {
            HeaderString = header;
            Content = logContent;
            Header = new CQNotesLogHeader(HeaderString);
        }

        public string HeaderString { get; private set; }
        public string Content { get; private set; }
        public CQNotesLogHeader Header { get; private set; }
    }
}
