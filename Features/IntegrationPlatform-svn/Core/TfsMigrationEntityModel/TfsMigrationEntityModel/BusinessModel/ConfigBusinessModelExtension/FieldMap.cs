// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{
    public partial class FieldMap
    {
        /// <summary>
        /// Given the migration source side and field reference name, tells if it is a UserIdentity field.
        /// </summary>
        /// <param name="fromSide"></param>
        /// <param name="srcFieldRefName"></param>
        /// <returns></returns>
        public bool IsUserIdField(
            SourceSideTypeEnum fromSide, 
            string srcFieldRefName)
        {
            return (null != GetUserIdField(fromSide, srcFieldRefName));
        }

        /// <summary>
        /// Given the migration source side and field reference name, gets the UserIdentityField configuration element.
        /// </summary>
        /// <param name="fromSide"></param>
        /// <param name="srcFieldRefName"></param>
        /// <returns>The UserIdFieldElement for the subject field; NULL if no configuration is present.</returns>
        public UserIdFieldElement GetUserIdField(
            SourceSideTypeEnum fromSide, 
            string srcFieldRefName)
        {
            NotifyingCollection<UserIdFieldElement> userIdFields;

            switch (fromSide)
            {
                case SourceSideTypeEnum.Left:
                    userIdFields = UserIdentityFields.LeftUserIdentityFields.UserIdField;
                    break;
                case SourceSideTypeEnum.Right:
                    userIdFields = UserIdentityFields.RightUserIdentityFields.UserIdField;
                    break;
                default:
                    return null;
            }

            foreach (var userIdField in userIdFields)
            {
                if (userIdField.FieldReferenceName.Equals(srcFieldRefName, StringComparison.OrdinalIgnoreCase))
                {
                    return userIdField;
                }
            }

            return null;
        }
    }
}
