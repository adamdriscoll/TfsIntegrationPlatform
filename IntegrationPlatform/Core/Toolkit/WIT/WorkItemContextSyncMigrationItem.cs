// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    [Serializable]
    public class WorkItemContextSyncMigrationItem : IMigrationItem
    {
        ContentType m_contextInfoContentType;

        /// <summary>
        /// The default constructor required by serialization
        /// </summary>
        public WorkItemContextSyncMigrationItem()
        { 
        }

        public ContentType ContextInfoContentType
        {
            get
            {
                return m_contextInfoContentType;
            }
            set
            {
                m_contextInfoContentType = value;
            }
        }

        public WorkItemContextSyncMigrationItem(ContentType contextInfoContentType)
        {
            if (null == contextInfoContentType)
            {
                throw new ArgumentNullException("contextInfoContentType");
            }
            m_contextInfoContentType = contextInfoContentType;
        }

        #region IMigrationItem Members

        public void Download(string localPath)
        {
        }

        public string DisplayName
        {
            get
            {
                return ContextInfoContentType.FriendlyName;
            }
        }

        #endregion
    }
}
