// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static class GenerateLabelActionsHelper
    {
        public static void AddLabelActionsToChangeGroup(ChangeGroup changeGroup, ILabel label)
        {
            if (changeGroup != null && changeGroup.Actions.Count > 0 && label != null && label.LabelItems.Count > 0)
            {
                LabelProperties labelProperties = new LabelProperties(label);

                changeGroup.CreateAction(
                    WellKnownChangeActionId.Add,
                    null,
                    null,
                    // HACK: This is the ToPath which we shouldn't need for a label, but it is not currently nullable, so we use the first action Path
                    changeGroup.Actions[0].Path, 
                    null,
                    null,
                    WellKnownContentType.VersionControlLabel.ReferenceName,
                    labelProperties.ToXmlDocument());

                // Create a MigrationAction for each item in the label
                foreach (ILabelItem labelItem in label.LabelItems)
                {
                    IMigrationAction action = changeGroup.CreateAction(
                        WellKnownChangeActionId.Add,
                        null,
                        null,
                        labelItem.ItemCanonicalPath,
                        labelItem.ItemVersion,
                        null,
                        labelItem.Recurse ?
                            WellKnownContentType.VersionControlRecursiveLabelItem.ReferenceName :
                            WellKnownContentType.VersionControlLabelItem.ReferenceName,
                        null);
                }
            }
        }
    }
}
