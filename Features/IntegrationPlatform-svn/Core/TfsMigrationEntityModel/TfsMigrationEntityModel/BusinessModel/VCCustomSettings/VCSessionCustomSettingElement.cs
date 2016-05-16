// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//--------------------------------------------------------------
// Generated from:
// Schema file: VcSessionSettingXmlSchema.xsd
// Creation Date: 2/10/2010 6:07:13 PM
//--------------------------------------------------------------

#pragma warning disable 1591

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration;

namespace Microsoft.TeamFoundation.Migration.BusinessModel.VC
{

    public struct Declarations
    {
        public const string SchemaVersion = "";
    }




    [XmlType(TypeName = "VCSessionCustomSettingElement"), Serializable]
    public partial class VCSessionCustomSettingElement : ModelObject
    {

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.VC.SettingsElement __Settings;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.VC.SettingsElement), ElementName = "Settings", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.VC.SettingsElement Settings
        {
            get
            {
                if (__Settings == null)
                {
                    __Settings = new Microsoft.TeamFoundation.Migration.BusinessModel.VC.SettingsElement();
                    this.RaisePropertyChangedEvent("Settings", null, __Settings);
                }
                return __Settings;
            }
            set
            {
                if (value != __Settings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.VC.SettingsElement oldValue = __Settings;
                    __Settings = value;
                    this.RaisePropertyChangedEvent("Settings", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.VC.BranchSettingsElement __BranchSettings;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.VC.BranchSettingsElement), ElementName = "BranchSettings", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.VC.BranchSettingsElement BranchSettings
        {
            get
            {
                if (__BranchSettings == null)
                {
                    __BranchSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.VC.BranchSettingsElement();
                    this.RaisePropertyChangedEvent("BranchSettings", null, __BranchSettings);
                }
                return __BranchSettings;
            }
            set
            {
                if (value != __BranchSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.VC.BranchSettingsElement oldValue = __BranchSettings;
                    __BranchSettings = value;
                    this.RaisePropertyChangedEvent("BranchSettings", oldValue, value);
                }
            }
        }

		public VCSessionCustomSettingElement()
		{
		}
	}


    [XmlType(TypeName = "SettingsElement"), Serializable]
    public partial class SettingsElement : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<Setting> __Setting;

        [XmlElement(Type = typeof(Setting), ElementName = "Setting", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<Setting> Setting
        {
            get
            {
                if (__Setting == null)
                {
                    __Setting = new NotifyingCollection<Setting>();
                }
                return __Setting;
            }
        }

        public SettingsElement()
        {
        }
    }


    [XmlType(TypeName = "Setting"), Serializable]
    public partial class Setting : ModelObject
    {

        [XmlIgnore]
        private string __SettingKey;

        [XmlAttribute(AttributeName = "SettingKey", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string SettingKey
        {
            get
            {
                return __SettingKey;
            }
            set
            {
                if (value != __SettingKey)
                {
                    string oldValue = __SettingKey;
                    __SettingKey = value;
                    this.RaisePropertyChangedEvent("SettingKey", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __SettingValue;

        [XmlAttribute(AttributeName = "SettingValue", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string SettingValue
        {
            get
            {
                return __SettingValue;
            }
            set
            {
                if (value != __SettingValue)
                {
                    string oldValue = __SettingValue;
                    __SettingValue = value;
                    this.RaisePropertyChangedEvent("SettingValue", oldValue, value);
                }
            }
        }

        public Setting()
        {
        }
    }

    [XmlType(TypeName = "BranchSettingsElement"), Serializable]
    public partial class BranchSettingsElement : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<BranchSetting> __BranchSetting;

        [XmlElement(Type = typeof(BranchSetting), ElementName = "BranchSetting", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<BranchSetting> BranchSetting
        {
            get
            {
                if (__BranchSetting == null)
                {
                    __BranchSetting = new NotifyingCollection<BranchSetting>();
                }
                return __BranchSetting;
            }
        }

        public BranchSettingsElement()
        {
        }
    }

    [XmlType(TypeName = "BranchSetting"), Serializable]
    public partial class BranchSetting : ModelObject
    {

        [XmlIgnore]
        private string __SourceBranch;

        [XmlAttribute(AttributeName = "SourceBranch", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string SourceBranch
        {
            get
            {
                return __SourceBranch;
            }
            set
            {
                if (value != __SourceBranch)
                {
                    string oldValue = __SourceBranch;
                    __SourceBranch = value;
                    this.RaisePropertyChangedEvent("SourceBranch", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __TargetBranch;

        [XmlAttribute(AttributeName = "TargetBranch", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string TargetBranch
        {
            get
            {
                return __TargetBranch;
            }
            set
            {
                if (value != __TargetBranch)
                {
                    string oldValue = __TargetBranch;
                    __TargetBranch = value;
                    this.RaisePropertyChangedEvent("TargetBranch", oldValue, value);
                }
            }
        }


        [XmlIgnore]
        private string __SourceId;

        [XmlAttribute(AttributeName = "SourceId", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string SourceId
        {
            get
            {
                return __SourceId;
            }
            set
            {
                if (value != __SourceId)
                {
                    string oldValue = __SourceId;
                    __SourceId = value;
                    this.RaisePropertyChangedEvent("SourceId", oldValue, value);
                }
            }
        }

        public BranchSetting()
        {
        }
    }


    [XmlRoot(ElementName = "VCSessionCustomSetting", IsNullable = false), Serializable]
    public partial class VCSessionCustomSetting : Microsoft.TeamFoundation.Migration.BusinessModel.VC.VCSessionCustomSettingElement
    {

        public VCSessionCustomSetting()
            : base()
        {
        }
    }
}

#pragma warning restore 1591
