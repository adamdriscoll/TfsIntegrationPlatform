// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration;

namespace MigrationTestLibrary
{
    public class TestMigrationSource : ModelObject
    {
        private string __Id;

        [XmlAttribute]
        public string Id
        {
            get
            {
                return __Id;
            }
            set
            {
                if (value != __Id)
                {
                    string oldValue = __Id;
                    __Id = value;
                    this.RaisePropertyChangedEvent("Id", oldValue, value);
                }
            }
        }

        private TFSVersionEnum __TFSVersion;

        [XmlAttribute]
        public TFSVersionEnum TFSVersion
        {
            get
            {
                return __TFSVersion;
            }
            set
            {
                if (value != __TFSVersion)
                {
                    MigrationTestLibrary.TFSVersionEnum oldValue = __TFSVersion;
                    __TFSVersion = value;
                    this.RaisePropertyChangedEvent("TFSVersion", oldValue, value);
                }
            }
        }

        private string __ServerIdentifier;

        [XmlAttribute]
        public string ServerIdentifier
        {
            get
            {
                return __ServerIdentifier;
            }
            set
            {
                if (value != __ServerIdentifier)
                {
                    string oldValue = __ServerIdentifier;
                    __ServerIdentifier = value;
                    this.RaisePropertyChangedEvent("ServerIdentifier", oldValue, value);
                }
            }
        }

        private string __ProviderRefName;

        [XmlAttribute]
        public string ProviderRefName
        {
            get
            {
                return __ProviderRefName;
            }
            set
            {
                if (value != __ProviderRefName)
                {
                    string oldValue = __ProviderRefName;
                    __ProviderRefName = value;
                    this.RaisePropertyChangedEvent("ProviderRefName", oldValue, value);
                }
            }
        }

        private TCAdapterEnvironment __TCAdapterEnv;

        [XmlElement]
        public TCAdapterEnvironment TCAdapterEnv
        {
            get
            {
                if (__TCAdapterEnv == null)
                {
                    __TCAdapterEnv = new TCAdapterEnvironment();
                    this.RaisePropertyChangedEvent("TCAdapterEnv", null, __TCAdapterEnv);
                }
                return __TCAdapterEnv;
            }
            set
            {
                if (value != __TCAdapterEnv)
                {
                    TCAdapterEnvironment oldValue = __TCAdapterEnv;
                    __TCAdapterEnv = value;
                    this.RaisePropertyChangedEvent("TCAdapterEnv", oldValue, value);
                }
            }
        }

        private TCCustomSettings __CustomSettingList;

        [XmlElement]
        public TCCustomSettings CustomSettingList
        {
            get
            {
                if (__CustomSettingList == null)
                {
                    __CustomSettingList = new MigrationTestLibrary.TCCustomSettings();
                    this.RaisePropertyChangedEvent("CustomSettingList", null, __CustomSettingList);
                }
                return __CustomSettingList;
            }
            set
            {
                if (value != __CustomSettingList)
                {
                    TCCustomSettings oldValue = __CustomSettingList;
                    __CustomSettingList = value;
                    this.RaisePropertyChangedEvent("CustomSettingList", oldValue, value);
                }
            }
        }

        public TestMigrationSource()
        {
        }
    }
}
