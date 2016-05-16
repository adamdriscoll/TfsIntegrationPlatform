// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace MigrationTestLibrary
{
    [Serializable]
    // Make sure that newer TFS adapters come after older ones so tests can do comparisons like AdapterType >= TFS2010VC, etc
    public enum AdapterType
    {
        TFS2008VC,
        TFS2008WIT,
        TFS2010VC,
        TFS2010WIT,
        TFS11VC,
        TFS11WIT,
        FileSystem,
        ClearCaseSelectedHistory,
        ClearCaseDetailedHistory,
        ClearQuest,
        Subversion,
    }

    public class EndPoint
    {
        [XmlAttribute]
        public string ID { get; set; }

        /// <summary>
        /// Type of adapter to use for this end point
        /// </summary>
        public AdapterType AdapterType { get; set; }

        public bool IsTfsAdapter
        {
            get
            {
                switch (AdapterType)
                {
                    case MigrationTestLibrary.AdapterType.TFS2008VC:
                    case MigrationTestLibrary.AdapterType.TFS2008WIT:
                    case MigrationTestLibrary.AdapterType.TFS2010VC:
                    case MigrationTestLibrary.AdapterType.TFS2010WIT:
                    case MigrationTestLibrary.AdapterType.TFS11VC:
                    case MigrationTestLibrary.AdapterType.TFS11WIT:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public string FriendlyName { get; set; }

        public string ServerUrl { get; set; }

        public string TeamProject { get; set; }

        public String WorkspaceName { get; set; }

        /// <summary>
        /// Optional: User can override the default by explicitly providing an adapter to use here
        /// Otherwise we figure it out based on AdapterType
        /// </summary>
        public Guid AdapterID { get; set; }

        /// <summary>
        /// Optional: User can override the default by explicitly providing an TCadapter to use here
        /// Otherwise we figure it out based on AdapterType
        /// </summary>
        public Guid TCAdapterID { get; set; }

        public string VobName { get; set; }

        public string ViewName { get; set; }

        public string UncStorageLocation { get; set; }

        public string LocalStorageLocation { get; set; }

        public List<Setting> CustomSettingsList = new List<Setting>();

        /// <summary>
        /// Used in the session configuration file
        /// </summary>
        [XmlIgnore]
        public Guid InternalUniqueID = Guid.NewGuid();

        [XmlIgnore]
        public string TestName { get; set; }

        public void Initialize()
        {
            // if the user does not explicitly provide Adapters to use, then try to find one for them based on the specified AdapterType
            if (AdapterID == Guid.Empty)
            {
                AdapterID = GetAdapterID(AdapterType);
            }
            if (TCAdapterID == Guid.Empty)
            {
                TCAdapterID = GetTCAdapterID(AdapterType);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("FriendlyName:{0} ", FriendlyName));
            sb.AppendLine(String.Format("AdapterType:{0} ", AdapterType));
            sb.AppendLine(String.Format("AdapterID:{0} ", AdapterID));
            sb.AppendLine(String.Format("TCAdapterID:{0} ", TCAdapterID));
            sb.AppendLine(String.Format("ServerUrl:{0} ", ServerUrl));
            sb.AppendLine(String.Format("TeamProject:{0} ", TeamProject));
            sb.AppendLine(String.Format("WorkspaceName:{0} ", WorkspaceName));
            sb.AppendLine(String.Format("VobName:{0} ", VobName));
            sb.AppendLine(String.Format("ViewName:{0} ", ViewName));
            sb.AppendLine(String.Format("UncStorageLocation:{0} ", UncStorageLocation));
            sb.AppendLine(String.Format("LocalStorageLocation:{0} ", LocalStorageLocation));
            foreach (Setting s in CustomSettingsList)
            {
                sb.AppendLine(s.ToString());
            }

            return sb.ToString();
        }

        private Guid GetAdapterID(AdapterType adapterType)
        {
            foreach (AdapterMapping am in map)
            {
                if (adapterType == am.AdapterType)
                {
                    return new Guid(am.AdapterID);
                }
            }
            throw new Exception(String.Format("Failed to find Adapter ID for AdapterType {0}", adapterType));
        }

        private Guid GetTCAdapterID(AdapterType adapterType)
        {
            foreach (AdapterMapping am in map)
            {
                if (adapterType == am.AdapterType)
                {
                    return new Guid(am.TCAdapterID);
                }
            }
            throw new Exception(String.Format("Failed to find TCAdapter ID for AdapterType {0}", adapterType));
        }

        private struct AdapterMapping
        {
            public AdapterType AdapterType;
            public string AdapterID;
            public string TCAdapterID;
        }

        private static AdapterMapping[] map = new AdapterMapping[]
        {
            new AdapterMapping()
            { 
                AdapterType = AdapterType.TFS2008VC, 
                AdapterID = "2F82C6C4-BBEE-42fb-B3D0-4799CABCF00E",
                TCAdapterID = "0A2595BE-5DA5-4fb7-A298-BB05C40C5CC0",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.TFS2008WIT, 
                AdapterID = "663A8B36-7852-4750-87FC-D189B0640FC1",
                TCAdapterID = "1F9D1FCE-6E9E-45ea-AB21-3F6B395AB323",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.TFS2010VC, 
                AdapterID = "FEBC091F-82A2-449E-AED8-133E5896C47A",
                TCAdapterID = "0A2595BE-5DA5-4FB7-A298-BB05C40C5CC0",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.TFS2010WIT, 
                AdapterID = "04201D39-6E47-416f-98B2-07F0013F8455",
                TCAdapterID = "F8F18C95-764D-47e3-AB45-9ACD47CA8F82",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.TFS11WIT, 
                AdapterID = "B84B30DD-1496-462A-BD9D-5A078A617779",
                TCAdapterID = "F8F18C95-764D-47e3-AB45-9ACD47CA8F82",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.TFS11VC, 
                AdapterID = "4CC33B2B-4B76-451F-8C2C-D86A3846D6D2",
                TCAdapterID = "0A2595BE-5DA5-4FB7-A298-BB05C40C5CC0",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.FileSystem, 
                AdapterID = "43B0D301-9B38-4caa-A754-61E854A71C78",
                TCAdapterID = "EEB25ECB-A9C2-4f30-9D22-3B30B80D3118",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.ClearQuest, 
                AdapterID = "D9637401-7385-4643-9C64-31585D77ED16",
                TCAdapterID = "35AF98D6-5227-4807-B205-CD97AF08A1CA",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.ClearCaseSelectedHistory, 
                AdapterID = "43B0D301-9B38-4caa-A754-61E854A71C78",
                TCAdapterID = "EEB25ECB-A9C2-4f30-9D22-3B30B80D3118",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.ClearCaseDetailedHistory, 
                AdapterID = "F2A6BA65-8ACB-4cd0-BE8F-B25887F94392",
                TCAdapterID = "E6EE3EF6-6B1B-470c-AB89-82B5C418BDB3",
            },
            new AdapterMapping()
            { 
                AdapterType = AdapterType.Subversion, 
                AdapterID = "BCC31CA2-534D-4054-9013-C1FEF67D5273",
                TCAdapterID = "95ECF05F-966F-4391-8CB1-534B03A392DA",
            },
        };
    }
}
