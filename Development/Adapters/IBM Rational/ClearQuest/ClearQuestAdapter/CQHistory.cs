// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter
{
    internal class CQHistory
    {
        public CQHistory(OAdHistory aHistory)
        {
            Initialize(CQWrapper.HistoryValue(aHistory));
        }

        public CQHistory(string historyValue)
        {
            Initialize(historyValue);
        }

        public string Date { get; private set; }
        public string User { get; private set; }
        public string Action { get; private set; }
        public string Oldstate { get; private set; }
        public string Newstate { get; private set; }

        private void Initialize(string historyValue)
        {
            string[] parsedString = historyValue.Split('\t');
            Debug.Assert(parsedString != null && parsedString.Length >= 6);

            Date = parsedString[1];
            User = parsedString[2];
            Action = parsedString[3];
            Oldstate = parsedString[4];
            Newstate = parsedString[5];
        }
    }
}
