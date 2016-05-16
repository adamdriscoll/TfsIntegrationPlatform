// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace SemaphoreFileAnalysisAddin
{
    public class RecursiveLabelItem : ILabelItem
    {
        private string m_itemPath;

        public RecursiveLabelItem(string itemPath)
        {
            if (string.IsNullOrEmpty(itemPath))
            {
                throw new ArgumentException();
            }
            m_itemPath = itemPath;
        }

        // Summary:
        //     The path of the item to be label in canonical form (as defined by the interface
        //     IServerPathTranslationService)
        public string ItemCanonicalPath
        {
            get
            {
                // Use a common WindowsFileSytsemPathTranslator class?
                return m_itemPath;
            }
        }

        //
        // Summary:
        //     The string representation of the version of the Item to be labeled
        public string ItemVersion
        {
            get { return null; }
        }

        //
        // Summary:
        //     Whether or not to recursive include all items under the item specified by
        //     the ItemId in the label
        public bool Recurse
        {
            get { return true; }
        }
    }
}
