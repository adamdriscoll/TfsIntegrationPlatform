// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// This interface represents the TFS Work Item Server
    /// </summary>
    public interface ITfsWorkItemServer
    {
        /// <summary>
        /// Gets the maximum attachment file size
        /// </summary>
        long MaxAttachmentSize { get; }

        /// <summary>
        /// Gets a new request Id from the TFS Work Item Server
        /// </summary>
        /// <returns></returns>
        string NewRequestId();

        /// <summary>
        /// Update a change to the TFS Work Item Server
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="package"></param>
        /// <param name="result"></param>
        /// <param name="metadataHave"></param>
        /// <param name="dbStamp"></param>
        /// <param name="metadata"></param>
        void Update(string requestId, XmlElement package, out XmlElement result, MetadataTableHaveEntry[] metadataHave, out string dbStamp, out IMetadataRowSets metadata);

        /// <summary>
        /// Update file attachment
        /// </summary>
        /// <param name="fileAttachment"></param>
        void UploadFile(FileAttachment fileAttachment);

        /// <summary>
        /// Dispose the server proxy instance.
        /// </summary>
        void Dispose();
    }
}
