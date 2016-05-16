// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Shell.View;
namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// Represents a command that a plugin supports.
    /// </summary>
    public delegate void Command ();

    /// <summary>
    /// Provides an interface for all Plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Called when a new context that this Plugin uses becomes available.
        /// </summary>
        /// <param name="contextInstance">The instance of the context.</param>
        void OnContextEnter (object contextInstance);

        /// <summary>
        /// Called when an existing context that this Plugin uses becomes unavailable.
        /// </summary>
        /// <param name="contextInstance">The instance of the context.</param>
        void OnContextLeave (object contextInstance);

        IMigrationSourceView GetMigrationSourceView();

        IEnumerable<IConflictTypeView> GetConflictTypeViews();

        ExecuteFilterStringExtension FilterStringExtension { get; }
    }
}
