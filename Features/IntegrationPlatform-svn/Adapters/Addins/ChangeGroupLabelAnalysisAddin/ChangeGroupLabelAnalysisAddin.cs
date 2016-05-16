// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace ChangeGroupLabelAnalysisAddin
{
    /// <summary>
    /// An AnalysisAddin for the source side of a version control migration or sync that generates migration instructions to create a label on the 
    /// target side of the migration.   The resulting label on the target side will label all folders and files that were modified in the process
    /// of migrating that change group (with the exception of deleted items).  A label name will be automatically generated that includes identifies
    /// the Change Group on the source side as well as the date and time.
    /// </summary>
    public class ChangeGroupLabelAnalysisAddin : AnalysisAddin
    {
        /// <summary>
        /// The GUID string of the Reference Name of this Add-in
        /// </summary>
        public const string ReferenceNameString = "356F2115-223D-46d5-90BF-3027EEAA271B";

        #region IAddin Members

        /// <summary>
        /// The Reference Name of this Add-in
        /// </summary>
        public override Guid ReferenceName
        {
            get { return new Guid(ReferenceNameString); }
        }

        public override string FriendlyName
        {
            get
            {
                return ChangeGroupLabelAnalysisAddinResources.AddinFriendlyName;
            }
        }

        #endregion

        #region AnalysisAddin implementation
        public override void PostChangeGroupDeltaComputation(AnalysisContext analysisContext, ChangeGroup changeGroup)
        {
            // TraceManager.TraceInformation("Entering ChangeGroupLabelAnalysisAddin.PostChangeGroupDeltaComputation");
            ILabel changeGroupLabel = GetChangeGroupLabel(analysisContext, changeGroup);
            GenerateLabelActionsHelper.AddLabelActionsToChangeGroup(changeGroup, changeGroupLabel);
            // TraceManager.TraceInformation("Leaving ChangeGroupLabelAnalysisAddin.PostChangeGroupDeltaComputation");
        }

        #endregion

        #region private methods
        private ILabel GetChangeGroupLabel(AnalysisContext analysisContext, ChangeGroup changeGroup)
        {
            ILabel label = new ChangeGroupLabel(changeGroup, analysisContext);
            return label;
        }
        #endregion

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public override object GetService(Type serviceType)
        {
            if (serviceType == typeof(ChangeGroupLabelAnalysisAddin) ||
                serviceType == typeof(AnalysisAddin))
            {
                return this;
            }

            return null;
        }

        #endregion
    }
}
