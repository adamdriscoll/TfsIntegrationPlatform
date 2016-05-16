// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;

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
