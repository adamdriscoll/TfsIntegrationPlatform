// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This resolution action resolves a conflict by attempting multiple retries
    /// </summary>
    public class MultipleRetryResolutionAction : ResolutionAction
    {
        public const string Infinite = "Infinite"; // do not localize
        public const string DATAKEY_NUMBER_OF_RETRIES = "NumberOfRetries"; // do not localize

        static MultipleRetryResolutionAction()
        {
            s_actionRefName = new Guid("{01EFD898-60A3-4c62-9088-E8B7AE71C3C7}");
            s_dispName = "Resolve the conflict by attempting multiple retries";
            
            s_supportedActionDataKeys = new List<string>();
            s_supportedActionDataKeys.Add(DATAKEY_NUMBER_OF_RETRIES);
        }

        public static ConflictResolutionResult TryResolve(ConflictResolutionRule rule, MigrationConflict conflict)
        {
            throw new NotImplementedException();
        }

        public MultipleRetryResolutionAction()
        {
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
                return s_dispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return s_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid s_actionRefName;
        private static readonly string s_dispName;
        private static readonly List<string> s_supportedActionDataKeys;
    }
}
