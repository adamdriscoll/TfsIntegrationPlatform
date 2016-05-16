// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
// 20091101 TFS Integration Platform Custom Adapter Proof-of-Concept
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Rangers.TFS.Migration.PocAdapter.VC
{
    class PocVCMigrationItemSerializer : IMigrationItemSerializer
    {
        #region IMigrationItemSerializer Members

        IMigrationItem IMigrationItemSerializer.LoadItem(string itemBlob, ChangeGroupManager manager)
        {
            TraceManager.TraceInformation("POC:Ser:LoadItem");
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrEmpty(itemBlob))
            {
                throw new ArgumentNullException("itemBlob");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PocVCMigrationItem));

            using (StringReader strReader = new StringReader(itemBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                PocVCMigrationItem item = (PocVCMigrationItem)serializer.Deserialize(xmlReader);
                return item;
            }
        }

        string IMigrationItemSerializer.SerializeItem(IMigrationItem item)
        {
            TraceManager.TraceInformation("POC:Ser:SerializeItem - {0}", item.DisplayName);
            XmlSerializer serializer = new XmlSerializer(typeof(PocVCMigrationItem));

            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, item);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        #endregion
    }
}
