// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common.ResolutionActions
{
    /// <summary>
    /// This action resolves the history-not-found conflict by updating the conversion history with user-specified conversion information.
    /// </summary>
    public class HistoryNotFoundUpdateConversionHistoryAction : ResolutionAction
    {
        public static readonly string DATAKEY_SOURCE_ITEM_ID = "Source Item Id";
        public static readonly string DATAKEY_SOURCE_REVISION_RANGES = "Source Revisions";
        public static readonly string DATAKEY_TARGET_ITEM_ID = "Target Item Id";
        public static readonly string DATAKEY_TARGET_REVISION_RANGES = "Target Revisions";

        public static readonly string RevisionRangeDelimiter = "-";
        public static readonly string RevisionListDelimiter = ",";

        private static readonly List<string> s_supportedActionDataKeys;

        static HistoryNotFoundUpdateConversionHistoryAction()
        {
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_SOURCE_ITEM_ID);
            s_supportedActionDataKeys.Add(DATAKEY_SOURCE_REVISION_RANGES);
            s_supportedActionDataKeys.Add(DATAKEY_TARGET_ITEM_ID);
            s_supportedActionDataKeys.Add(DATAKEY_TARGET_REVISION_RANGES);
        }

        /// <summary>
        /// Gets the reference name of this action.
        /// </summary>
        public override Guid ReferenceName
        {
            get { return new Guid("58C2252B-CDB5-4511-9676-45103BE5ACC3") ; }
        }

        /// <summary>
        /// Gets the friendly name of this action.
        /// </summary>
        public override string FriendlyName
        {
            get { return "Resolve Work Item history-not-found conflict by updating the conversion history with user-specified revision pairs."; }
        }

        /// <summary>
        /// Gets the keys used in the action data.
        /// </summary>
        public override System.Collections.ObjectModel.ReadOnlyCollection<string> ActionDataKeys
        {
            get { return s_supportedActionDataKeys.AsReadOnly(); }
        }
    }
}
