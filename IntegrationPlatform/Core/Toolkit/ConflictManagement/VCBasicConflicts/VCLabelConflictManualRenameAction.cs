// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCLabelConflictManualRenameAction : ResolutionAction
    {
        public static readonly string DATAKEY_RENAME_LABEL = "Rename Label";

        static VCLabelConflictManualRenameAction()
        {
            s_actionRefName = new Guid("36696449-36DA-4661-AD7B-94DBD398D734");
            s_ationDispName = "Resolve label conflict by manually renaming the label";

            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_RENAME_LABEL);
        }

        public override Guid ReferenceName
        {
            get 
            { 
                return s_actionRefName; 
            }
        }

        public override string FriendlyName
        {
            get
            {
                return s_ationDispName;
            }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get 
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }


        private static readonly Guid s_actionRefName;
        private static readonly string s_ationDispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
