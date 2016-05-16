// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;
using System.Xml;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    class Tfs2008WorkItemServer : ITfsWorkItemServer
    {
        TeamFoundationServer m_teamFoundationServer;
        ClientService m_svc;
        ConfigurationSettingsServiceProxy m_configProxy;

        public Tfs2008WorkItemServer(TeamFoundationServer teamFoundationServer, ClientService svc)
        {
            m_teamFoundationServer = teamFoundationServer;
            m_svc = svc;
        }

        #region ITfsWorkItemServer Members

        public string NewRequestId()
        {
            return ClientService.NewRequestId();
        }

        public void Update(string requestId, XmlElement package, out XmlElement result, MetadataTableHaveEntry[] metadataHave, out string dbStamp, out IMetadataRowSets metadata)
        {
            m_svc.Update(requestId, package, out result, metadataHave, out dbStamp, out metadata);
        }

        public void UploadFile(FileAttachment fileAttachment)
        {
            m_svc.UploadFile(fileAttachment);
        }

        public void Dispose()
        {
            m_svc.Dispose();
        }

        public long MaxAttachmentSize
        {
            get 
            {
                return ConfigProxy.GetMaxAttachmentSize();
            }
        }

        #endregion

        ConfigurationSettingsServiceProxy ConfigProxy
        {
            get
            {
                if (m_configProxy == null)
                {
                    var proxyUrl = RegistrationUtilities.GetServiceUrlForTool(m_teamFoundationServer,
                                                                          ToolNames.WorkItemTracking,
                                                                          "ConfigurationSettingsUrl");
                    m_configProxy = new ConfigurationSettingsServiceProxy();
                    m_configProxy.TeamFoundationServer = m_teamFoundationServer;
                    m_configProxy.Url = proxyUrl;
                    m_configProxy.UserAgent = "migration tools";
                }
                return m_configProxy;
            }
        }
    }
}
