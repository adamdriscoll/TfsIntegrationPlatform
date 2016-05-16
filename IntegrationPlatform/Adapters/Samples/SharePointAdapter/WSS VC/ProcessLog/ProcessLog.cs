//------------------------------------------------------------------------------
// <copyright file="ProcessLog.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System.Collections.Generic;
    using System.IO;
    using System;
    using System.Xml.Serialization;

    public class ProcessLog 
    {
        private List<Item> Items;
        const string logFile = "sharepoint-write.log";
        private bool modified = false;

        public List<Item> LogItems
        {
            get
            {
                return this.Items;
            }
        }

        public ProcessLog()
        {
            if (File.Exists(logFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Item>));
                using (FileStream file = new FileStream(logFile, FileMode.Open))
                {
                    Items = serializer.Deserialize(file) as List<Item>;
                }
            }
            else
            {
                Items = new List<Item>();
            }
        }

        public void Add(Item item)
        {
            Items.Add(item);
            modified = true;
        }

        public void Save()
        {
            if (modified)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Item>));
                using (FileStream file = new FileStream(logFile, FileMode.Create))
                {
                    serializer.Serialize(file, Items);
                }
            }
        }
    }

    public class Item
    {
        public string EncodedAbsUrl { get; set; }
        public string Version { get; set; }
        public string Workspace { get; set; }
    }
}
