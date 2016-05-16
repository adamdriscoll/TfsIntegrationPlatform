// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    [Serializable]
    public class ArtifactType
    {
        public ArtifactType()
        {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="friendlyName"></param>
        /// <param name="contentTypeRefName"></param>
        public  ArtifactType(
            string referenceName,
            string friendlyName,
            string contentTypeRefName)
        {
            if (string.IsNullOrEmpty(referenceName))
            {
                throw new ArgumentNullException("referenceName");
            }

            if (string.IsNullOrEmpty(friendlyName))
            {
                throw new ArgumentNullException("friendlyName");
            }

            if (string.IsNullOrEmpty(contentTypeRefName))
            {
                throw new ArgumentNullException("contentTypeRefName");
            }

            ReferenceName = referenceName;
            FriendlyName = friendlyName;
            ContentTypeReferenceName = contentTypeRefName;
        }

        public string ReferenceName
        {
            get; 
            set;
        }

        public string FriendlyName
        {
            get;
            set;
        }

        public string ContentTypeReferenceName
        {
            get;
            set;
        }
    }
}
