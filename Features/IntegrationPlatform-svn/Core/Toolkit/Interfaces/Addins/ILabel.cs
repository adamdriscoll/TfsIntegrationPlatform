// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
// using System.ComponentModel.Design;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// An interface that defines the contents of a VC label involved in a MigrationAction
    /// </summary>
    public interface ILabel
    {
        /// <summary>
        /// The name of the label (a null or empty value is invalid)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The comment associated with the label
        /// It may be null or empty
        /// </summary>
        string Comment { get; }

        /// <summary>
        /// The name of the owner (it may be null or empty)
        /// </summary>
        string OwnerName { get; }

        /// <summary>
        /// The scope is a server path that defines the namespace for labels in some VC servers
        /// In this case, label names must be unique within the scope, but two or more labels with the
        /// same name may exist as long as their Scopes are distinct.
        /// It may be string.Empty is source from a VC server that does not have the notion of label scopes
        /// </summary>
        string Scope { get; }

        /// <summary>
        /// The set of items included in the label
        /// </summary>
        List<ILabelItem> LabelItems { get; }
    }


}
