// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal static class SessionContentTypeBinding
    {
        private static readonly Dictionary<string, SessionTypeEnum> s_contentTypeToSessionTypeBinding;

        static SessionContentTypeBinding()
        {
            s_contentTypeToSessionTypeBinding = new Dictionary<string, SessionTypeEnum>(2);

            s_contentTypeToSessionTypeBinding.Add(
                WellKnownContentType.VersionControlChangeGroup.ReferenceName, 
                SessionTypeEnum.VersionControl);

            s_contentTypeToSessionTypeBinding.Add(
                WellKnownContentType.VersionControlledArtifact.ReferenceName,
                SessionTypeEnum.VersionControl);

            s_contentTypeToSessionTypeBinding.Add(
                WellKnownContentType.VersionControlledFile.ReferenceName,
                SessionTypeEnum.VersionControl);

            s_contentTypeToSessionTypeBinding.Add(
                WellKnownContentType.WorkItem.ReferenceName, 
                SessionTypeEnum.WorkItemTracking);
        }

        internal static SessionTypeEnum? GetSessionType(
            string contentTypeReferenceName)
        {
            if (string.IsNullOrEmpty(contentTypeReferenceName))
            {
                throw new ArgumentNullException("contentTypeReferenceName");
            }

            SessionTypeEnum? sessionType = null;

            if (s_contentTypeToSessionTypeBinding.ContainsKey(contentTypeReferenceName))
            {
                sessionType = s_contentTypeToSessionTypeBinding[contentTypeReferenceName];
            }

            return sessionType;
        }

    }
}