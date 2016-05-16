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
    /// The action resolves the history-not-found conflict by submitting the missing changes and then updating the conversion history
    /// </summary>
    public class HistoryNotFoundSubmitMissingChangesAction : ResolutionAction
    {
        public static readonly string DATAKEY_REVISION_RANGE = "Revisions to Repair";

        private static readonly List<string> s_supportedActionDataKeys;

        static HistoryNotFoundSubmitMissingChangesAction()
        {
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_REVISION_RANGE);
        }

        /// <summary>
        /// Gets the reference name of this action.
        /// </summary>
        public override Guid ReferenceName
        {
            get { return new Guid("1B70ABD4-6FCC-4fac-8159-98528D7367CA"); }
        }

        /// <summary>
        /// Gets the friendly name of this action.
        /// </summary>
        public override string FriendlyName
        {
            get { return "Resolve Work Item history-not-found conflict by submitting the missing changes."; }
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
