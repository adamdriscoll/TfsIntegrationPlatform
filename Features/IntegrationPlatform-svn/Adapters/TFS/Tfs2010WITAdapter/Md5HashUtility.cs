// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    class Md5HashUtility
    {
        private readonly MD5Producer m_md5Producer = new MD5Producer();

        public void UpdateDocHash(ref byte[] docHashToUpdate, byte[] newDocHash)
        {
            if (newDocHash.Length > 0)
            {
                docHashToUpdate = newDocHash;
            }
        }

        public bool CompareDocHash(XmlDocument xmlDocument, byte[] oldDocHash, ref byte[] newDocHash)
        {
            if (!m_md5Producer.Md5ProviderDisabled)
            {
                string docContent = xmlDocument.OuterXml;
                newDocHash = m_md5Producer.CalculateMD5(new MemoryStream(ASCIIEncoding.Default.GetBytes(docContent)));

                if (oldDocHash.Length > 0
                    && newDocHash.Length > 0
                    && 0 == m_md5Producer.CompareMD5(oldDocHash, newDocHash))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
