// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    class Tfs2010WorkItemServer : ITfsWorkItemServer
    {
        private WorkItemServer m_svc;

        public Tfs2010WorkItemServer(WorkItemServer svc)
        {
            m_svc = svc;
        }

        #region ITfsWorkItemServer Members

        public string NewRequestId()
        {
            return WorkItemServer.NewRequestId();
        }

        public void Update(string requestId, System.Xml.XmlElement package, out System.Xml.XmlElement result, Microsoft.TeamFoundation.WorkItemTracking.Proxy.MetadataTableHaveEntry[] metadataHave, out string dbStamp, out Microsoft.TeamFoundation.WorkItemTracking.Proxy.IMetadataRowSets metadata)
        {
            m_svc.Update(requestId, package, out result, metadataHave, out dbStamp, out metadata);
        }

        public void UploadFile(FileAttachment fileAttachment)
        {
            m_svc.UploadFile(fileAttachment);
        }

        /// <summary>
        /// An enumerable of work items link changes, ordered by the database rowversion at the time they were changed.
        /// <param name="rowVersion">
        /// The maximum row version seen by the client thus far.
        /// </param>
        /// <returns>All work item link changes since the requested rowversion (in ascending order by rowversion)</returns>
        /// </summary>
        public IEnumerable<WorkItemLinkChange> GetWorkItemLinkChanges(string requestId, long rowVersion)
        {
            try
            {
                return m_svc.GetWorkItemLinkChanges(requestId, rowVersion);
            }
            catch(Exception ex)
            {
                TraceManager.TraceError(ex, false);
                return new WorkItemLinkChange[0];
            }
        }

        public void Dispose()
        {
            return;
        }

        public long MaxAttachmentSize
        {
            get { return m_svc.MaxAttachmentSize; }
        }

        #endregion
    }
}
