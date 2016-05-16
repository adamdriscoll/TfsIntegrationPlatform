// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// 
    /// </summary>
    public class LabelItemFromMigrationAction : ILabelItem
    {
        public LabelItemFromMigrationAction(IMigrationAction labelItemAction)
        {
            ItemCanonicalPath = labelItemAction.Path;
            ItemVersion = labelItemAction.Version;
            Recurse = labelItemAction.Recursive;
        }

        /// <summary>
        /// The path of the item to be label in canonical form (as defined by the interface IServerPathTranslationService)
        /// </summary>
        public string ItemCanonicalPath { get; internal set; }

        /// <summary>
        /// The string representation of the version of the Item to be labeled
        /// </summary>
        public string ItemVersion { get; internal set; }

        /// <summary>
        /// Whether or not to recursive include all items under the item specified by the ItemId in the label
        /// </summary>
        public bool Recurse { get; internal set; }
    }
}
