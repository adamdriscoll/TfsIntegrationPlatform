// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions
{
    public class FileAttachmentOversizedConflictDropAttachmentAction : ResolutionAction
    {
        public static readonly string DATAKEY_MIN_FILE_SIZE_TO_DROP = "MinFileSizeToDrop";

        static FileAttachmentOversizedConflictDropAttachmentAction()
        {
            s_actionReferenceName = new Guid("E3EE75A3-8BDC-40a5-903E-52D7EFA0DDDD");
            s_actionFriendlyName = "Resolve file attachment oversized conflict by dropping it.";

            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_MIN_FILE_SIZE_TO_DROP);
        }

        public override Guid ReferenceName
        {
            get 
            {
                return s_actionReferenceName;
            }
        }

        public override string FriendlyName
        {
            get
            {
                return s_actionFriendlyName;
            }
        }

        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid s_actionReferenceName;
        private static readonly string s_actionFriendlyName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
