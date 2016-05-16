// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Class for toolkit default handlers
    /// </summary>
    public abstract class ChangeActionHandlers
    {
        IAnalysisProvider m_analysisProvider;

        protected IAnalysisProvider AnalysisProvider
        {
            get
            {
                return m_analysisProvider;
            }
        }

        protected ChangeActionHandlers(IAnalysisProvider analysisProvider)
        {
            m_analysisProvider = analysisProvider;
        }

        /// <summary>
        /// Basic action handler that copy all fields of given action 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="group"></param>
        public virtual void BasicActionHandler(MigrationAction action, ChangeGroup group)
        {
            group.CreateAction(action.Action, 
                action.SourceItem, 
                action.FromPath, 
                action.Path, action.Version,
                action.MergeVersionTo,
                action.ItemTypeReferenceName, 
                action.MigrationActionDescription);
        }
    }
}
