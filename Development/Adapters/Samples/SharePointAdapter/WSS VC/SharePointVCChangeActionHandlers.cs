//------------------------------------------------------------------------------
// <copyright file="SharePointVCChangeActionHandlers.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using Microsoft.TeamFoundation.Migration.Toolkit;

    internal class SharePointVCChangeActionHandler : ChangeActionHandlers
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointVCChangeActionHandler"/> class.
        /// </summary>
        /// <param name="analysisProvider">The analysis provider.</param>
        internal SharePointVCChangeActionHandler(IAnalysisProvider analysisProvider)
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
            TraceManager.TraceInformation("\tWSSVC:ChangeActionHandler:Basic - {0} - {1}", action.State, group.Name);
            base.BasicActionHandler(action, group);
        }

    }
}
