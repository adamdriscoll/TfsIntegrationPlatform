// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// WellKnownContentType provides a list of predefined ContentTypes
    /// </summary>
    public static class WellKnownContentType
    {
        public readonly static ContentType GenericContent =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.GenericContent", "Place holder for content type"); // ToDo, this needs to be removed.

        // Version Control content types
        /// <summary>
        /// Version control change group/set content type
        /// </summary>
        public readonly static ContentType VersionControlChangeGroup =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlChangeGroup", "Version control change group/set content type");
        /// <summary>
        /// Version controlled artifact content type
        /// </summary>
        public readonly static ContentType VersionControlledArtifact =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlledArtifact", "Version controlled artifact contenct type");
        /// <summary>
        /// Version Control File content type
        /// </summary>
        public readonly static ContentType VersionControlledFile =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlledFile", "Version Control File content type");
        /// <summary>
        /// Version Control Folder content type
        /// </summary>
        public readonly static ContentType VersionControlledFolder =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlledFolder", "Version Control Folder content type");

        /// <summary>
        /// Version Control Label content type
        /// </summary>
        public readonly static ContentType VersionControlLabel =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlLabel", "Version Control Label content type");

        /// <summary>
        /// Version Control LabelItem content type
        /// </summary>
        public readonly static ContentType VersionControlLabelItem =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlLabelItem", "Version Control LabelItem content type");

        /// <summary>
        /// Version Control LabelItem content type
        /// </summary>
        public readonly static ContentType VersionControlRecursiveLabelItem =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.VersionControlRecursiveLabelItem", "Version Control RecursiveLabelItem content type");

        // Work Item Tracking content types
        /// <summary>
        /// Work Item content type
        /// </summary>
        public readonly static ContentType WorkItem =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.WorkItem", "Work Item content type");
        /// <summary>
        /// User group content type
        /// </summary>
        public readonly static ContentType UserGroupList =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.UserGroupList", "User group content type");
        /// <summary>
        /// Value list collection content type
        /// </summary>
        public readonly static ContentType ValueListCollection =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.ValueListCollection", "Value list collection content type");
        /// <summary>
        /// Generic work item metadata content type
        /// </summary>
        public readonly static ContentType GenericWorkItemFieldMetadata =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.GenericWorkItemFieldMetadata", "Generic work item metadata content type");
        /// <summary>
        /// TFS 2005 work item metadata content type
        /// </summary>
        public readonly static ContentType Tfs2005WorkItemFieldMetadata =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.Tfs2005WorkItemFieldMetadata", "TFS 2005 work item metadata content type");
        /// <summary>
        /// TFS 2008 work item metadata content type
        /// </summary>
        public readonly static ContentType Tfs2008WorkItemFieldMetadata =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.Tfs2008WorkItemFieldMetadata", "TFS 2008 work item metadata content type");
        /// <summary>
        /// TFS 2010 work item metadata content type
        /// </summary>
        public readonly static ContentType Tfs2010WorkItemFieldMetadata =
            new ContentType("Microsoft.TeamFoundation.Migration.Toolkit.Tfs2010WorkItemFieldMetadata", "TFS 2010 work item metadata content type");
    }

    /// <summary>
    /// Content type class
    /// </summary>
    [Serializable]
    public sealed class ContentType
    {
        string m_referenceName;
        string m_friendlyName;

        /// <summary>
        /// Default constructor needed by Serializer
        /// </summary>
        public ContentType()
        {
        }

        /// <summary>
        /// Instantiates a new ContentType object
        /// </summary>
        /// <param name="refName">Specifies the reference name for the content type</param>
        /// <param name="friendlyName">Specifies the friendly name or description for the content type</param>
        public ContentType(string refName, string friendlyName)
        {
            if (string.IsNullOrEmpty(refName))
            {
                throw new ArgumentNullException("refName");
            }

            if (string.IsNullOrEmpty(friendlyName))
            {
                throw new ArgumentNullException("friendlyName");
            }

            m_referenceName = refName;
            m_friendlyName = friendlyName;
        }

        /// <summary>
        /// Get reference name
        /// </summary>
        public string ReferenceName
        {
            get
            {
                return m_referenceName;
            }
            set
            {
                m_referenceName = value;
            }
        }

        /// <summary>
        /// Get friendly name
        /// </summary>
        public string FriendlyName
        {
            get
            {
                return m_friendlyName;
            }
            set
            {
                m_friendlyName = value;
            }
        }
    }

    /// <summary>
    /// ContentTypeRegistrationService is the service provider used by the migration tools to manage content types.
    /// </summary>
    public class ContentTypeRegistrationService : IServiceProvider
    {
        Dictionary<string, ContentType> m_contentTypes;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal ContentTypeRegistrationService()
        {
            m_contentTypes = new Dictionary<string, ContentType>();
        }

        /// <summary>
        /// Registered content types
        /// </summary>
        public Dictionary<string, ContentType> ContentTypes
        {
            get
            {
                return m_contentTypes;
            }
        }

        /// <summary>
        /// RegisterContentType is used to register ContentType object with the ContentTypeMigrationService.
        /// </summary>
        /// <param name="contentType">The ContentType to register</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.Services.ContentType" />
        public void RegisterContentType(ContentType contentType)
        {
            if (m_contentTypes.ContainsKey(contentType.ReferenceName))
            {
                Debug.Fail(string.Format(CultureInfo.InvariantCulture,
                                         "ContentTypeReferenceName '{0}:{1}' has already been added",
                                         contentType.ReferenceName,
                                         contentType.FriendlyName));
            }
            else
            {
                m_contentTypes.Add(contentType.ReferenceName, contentType);
            }
        }

        #region IServiceProvider Members

        /// <summary>
        /// Provides a method to get the service of the current object
        /// </summary>
        /// <param name="serviceType">Type of the service being requested</param>
        /// <returns>Returns this service object if the requested type is ContentTypeRegistrationService; otherwise, null is returned.</returns>

        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(ContentTypeRegistrationService)))
            {
                return this;
            }

            return null;
        }

        #endregion


    }
}
