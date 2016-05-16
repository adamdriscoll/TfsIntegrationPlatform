// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.ConflictManagement
{
    /// <summary>
    /// ClearQuestInsufficentPrivilegeConflictType class
    /// </summary>
    public class ClearQuestInsufficentPrivilegeConflictType : ConflictType
    {
        public const string ConflictDetailsKey_UserName = "UserName";
        public const string ConflictDetailsKey_MissingPrivilegeName = "MissingPrivilegeName";

        public ClearQuestInsufficentPrivilegeConflictType()
            : base(new ClearQuestInsufficentPrivilegeConflictHandler())
        { }

        public static MigrationConflict CreateConflict(
            string userName,
            string missingPrivilegeName)
        {
            return new MigrationConflict(
                new ClearQuestInsufficentPrivilegeConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(userName, missingPrivilegeName),
                CreateScopeHint(userName, missingPrivilegeName));
        }

        public override Guid ReferenceName
        {
            get { return new Guid("{D4D900FC-0077-41c3-B764-9AF1125CB08A}"); }
        }

        public override string FriendlyName
        {
            get { return ClearQuestResource.ClearQuest_Conflict_InsufficentPrivilege_Name; }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_ClearQuestInsufficentPrivilegeConflictType";
            }
        }

        /// <summary>
        /// Gets whether this conflict type is countable
        /// </summary>
        public override bool IsCountable
        {
            get
            {
                return true;
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            AddSupportedResolutionAction(new MultipleRetryResolutionAction());
            AddSupportedResolutionAction(new ManualConflictResolutionAction());
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_UserName);
            RegisterConflictDetailsPropertyKey(ConflictDetailsKey_MissingPrivilegeName);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);

                if (!string.IsNullOrEmpty(properties[ConflictDetailsKey_UserName])
                    && !string.IsNullOrEmpty(properties[ConflictDetailsKey_MissingPrivilegeName]))
                {
                    return CreateOldStyleConflictDetails(
                        properties[ConflictDetailsKey_UserName], properties[ConflictDetailsKey_MissingPrivilegeName]);
                }
                else
                {
                    // no expected data, just return raw details string
                    return dtls;
                }
            }
            catch (Exception)
            {
                // old style conflict details, just return raw details string
                return dtls;
            }
        }

        private static string CreateScopeHint(string userName, string missingPrivilegeName)
        {
            return string.Format("/{0}/{1}", userName, missingPrivilegeName);
        }

        private static string CreateConflictDetails(string userName, string missingPrivilegeName)
        {
            ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();
            detailsProperties.Properties.Add(ConflictDetailsKey_UserName, userName);
            detailsProperties.Properties.Add(ConflictDetailsKey_MissingPrivilegeName, missingPrivilegeName);
            return detailsProperties.ToString();
        }

        private string CreateOldStyleConflictDetails(string userName, string missingPrivilegeName)
        {
            return string.Format(ClearQuestResource.ClearQuest_Conflict_InsufficientPrivilege_DetailsFormat,
                userName, missingPrivilegeName);
        }
    }
}
