// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    [Serializable]
    public class InvalidWorkItemLinkDetails
    {
        public InvalidWorkItemLinkDetails()
        { }

        public InvalidWorkItemLinkDetails(
            string sourceItemId,
            string targetItemId,
            string linkTypeReferenceName)
        {
            SourceWorkItemID = sourceItemId;
            TargetWorkItemID = targetItemId;
            LinkTypeReferenceName = linkTypeReferenceName;
        }

        public string SourceWorkItemID { get; set; }
        public string TargetWorkItemID { get; set; }
        public string LinkTypeReferenceName { get; set; }

        internal static string CreateConflictDetails(string sourceItem, string targetItem, string linkType)
        {
            InvalidWorkItemLinkDetails dtls =
                new InvalidWorkItemLinkDetails(sourceItem, targetItem, linkType);

            XmlSerializer serializer = new XmlSerializer(typeof(InvalidWorkItemLinkDetails));
            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, dtls);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        internal static string TranslateConflictDetailsToReadableDescription(string detailsBlob, string conflictMessage)
        {
            if (string.IsNullOrEmpty(detailsBlob))
            {
                throw new ArgumentNullException("detailsBlob");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(InvalidWorkItemLinkDetails));

            using (StringReader strReader = new StringReader(detailsBlob))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                InvalidWorkItemLinkDetails details =
                    (InvalidWorkItemLinkDetails)serializer.Deserialize(xmlReader);

                return string.Format(
                    "{0} (Source WorkItem: {1}; Target WorkItem: {2}; LinkType: {3}.)",
                    conflictMessage, details.SourceWorkItemID, details.TargetWorkItemID, details.LinkTypeReferenceName);
            }
        }
    }
}
