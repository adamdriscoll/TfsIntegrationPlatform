//------------------------------------------------------------------------------
// <copyright file="WSSChangeActionHandlers.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointWITAdapter
{
    using System;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// 
    /// </summary>
    internal class SharePointChangeActionHandlers : ChangeActionHandlers
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointChangeActionHandlers"/> class.
        /// </summary>
        /// <param name="analysisProvider">The analysis provider.</param>
        internal SharePointChangeActionHandlers(IAnalysisProvider analysisProvider)
            : base(analysisProvider)
        {
        }

        /// <summary>
        /// Basic action handler that copy all fields of given action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="group"></param>
        public override void BasicActionHandler(MigrationAction action, ChangeGroup group)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            group.CreateAction(action.Action,
                action.SourceItem,
                action.FromPath,
                action.Path,
                action.Version,
                action.MergeVersionTo,
                action.ItemTypeReferenceName,
                action.MigrationActionDescription);
        }
    }
}
