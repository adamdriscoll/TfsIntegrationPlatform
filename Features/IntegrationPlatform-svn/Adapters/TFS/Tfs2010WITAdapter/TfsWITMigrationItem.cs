// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// TfsWITMigrationItem implements IMigrationItem, representing item data being migrated.
    /// </summary>
    [Serializable]
    public sealed class TfsWITMigrationItem : IMigrationItem
    {
        /// <summary>
        /// The default constructor required by serialization
        /// </summary>
        public TfsWITMigrationItem()
        {
        }

        public TfsWITMigrationItem(WorkItem workItem, int revIndex)
        {
            m_workItem = workItem;
            m_workItemId = workItem.Id;
            m_revision = revIndex;
            m_workItemUri = workItem.Uri;
            m_workItemUriString = m_workItemUri.AbsoluteUri;
        }

        public string WorkItemUriString
        {
            get 
            { 
                return m_workItemUriString; 
            }
            set 
            { 
                m_workItemUriString = value; 
            }
        }

        /// <summary>
        /// Uri of the TFS work item
        /// </summary>
        [XmlIgnore]
        public Uri WorkItemUri
        {
            get
            {
                if (null == m_workItemUri)
                {
                    m_workItemUri = new System.Uri(m_workItemUriString);
                }
                return m_workItemUri;
            }
        }

        /// <summary>
        /// id of the TFS work item
        /// </summary>
        public int WorkItemId
        {
            get 
            { 
                return m_workItemId; 
            }
            set
            {
                m_workItemId = value;
            }
        }

        /// <summary>
        /// revision of the TFS work item for this migration item
        /// </summary>
        public int Revision
        {
            get 
            { 
                return m_revision; 
            }
            set
            {
                m_revision = value;
            }
        }

        #region IMigrationItem Members

        public void Download(string localPath)
        {
            throw new NotImplementedException();
        }

        public string DisplayName
        {
            get 
            { 
                return string.Format("TFS Work Item {0} (revision: {1})", m_workItemId, m_revision); 
            }
        }

        #endregion


        [NonSerialized]
        string m_updateDetails = string.Empty;

        [NonSerialized]
        WorkItem m_workItem;

        int m_workItemId;
        int m_revision;
        string m_workItemUriString;

        [NonSerialized]
        Uri m_workItemUri;
    }
}
