// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
// using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// An interface that defines the contents of a VC label item involved in a MigrationAction
    /// </summary>
    public interface ILabelItem
    {
        /// <summary>
        /// The path of the item to be label in canonical form (as defined by the interface IServerPathTranslationService)
        /// </summary>
        string ItemCanonicalPath { get; }

        /// <summary>
        /// The string representation of the version of the Item to be labeled
        /// </summary>
        string ItemVersion { get; }

        /// <summary>
        /// Whether or not to recursive include all items under the item specified by the ItemId in the label
        /// </summary>
        bool Recurse { get; }
    }
}
