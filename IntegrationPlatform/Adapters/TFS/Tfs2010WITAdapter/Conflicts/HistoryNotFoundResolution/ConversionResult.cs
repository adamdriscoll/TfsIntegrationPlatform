// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts.HistoryNotFoundResolution
{
    internal class ConversionResult
    {
        private bool m_continueProcessing = true;

        List<ItemConversionHistory> m_itemConversionHistory = new List<ItemConversionHistory>();

        public string ChangeId { get; set; }

        public List<ItemConversionHistory> ItemConversionHistory
        {
            get
            {
                return m_itemConversionHistory;
            }
        }

        public bool ContinueProcessing
        {
            get
            {
                return m_continueProcessing;
            }

            set
            {
                m_continueProcessing = value;
            }
        }

    }
}
