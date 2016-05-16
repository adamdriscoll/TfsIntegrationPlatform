// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.IO;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.ClearQuestAdapter.MigrationItem
{
    public class ClearQuestRecordItemSerializer : IMigrationItemSerializer
    {
        private GenericSerializer<ClearQuestRecordItem> m_serializer = new GenericSerializer<ClearQuestRecordItem>();

        #region IMigrationItemSerializer Members

        public IMigrationItem LoadItem(string itemBlob, ChangeGroupManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrEmpty(itemBlob))
            {
                throw new ArgumentNullException("itemBlob");
            }

            return m_serializer.DeserializeItem(itemBlob) as IMigrationItem;
        }

        public string SerializeItem(IMigrationItem item)
        {
            return m_serializer.SerializeItem(item);
        }

        #endregion
    }
}


