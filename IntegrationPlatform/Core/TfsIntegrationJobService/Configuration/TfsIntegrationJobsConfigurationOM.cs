// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//--------------------------------------------------------------
// Generated from:
// Schema file: TfsIntegrationJobsConfigSchema.xsd
// Creation Date: 5/20/2010 2:04:59 PM
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

namespace Microsoft.TeamFoundation.Migration.TfsIntegrationJobService
{

    public struct Declarations
    {
        public const string SchemaVersion = "";
    }

    [Serializable]
    public enum TriggerOption
    {
        [XmlEnum(Name = "IntervalBased")]
        IntervalBased,
        [XmlEnum(Name = "TimeBased")]
        TimeBased
    }




    [XmlType(TypeName = "PropertyBagType"), Serializable]
    public partial class PropertyBagType : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<Setting> __Setting;

        [XmlElement(Type = typeof(Setting), ElementName = "Setting", IsNullable = false)]
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

        public PropertyBagType()
        {
        }
    }


    [XmlType(TypeName = "Setting"), Serializable]
    public partial class Setting : ModelObject
    {

        [XmlIgnore]
        private string __SettingKey;

        [XmlAttribute(AttributeName = "SettingKey", DataType = "normalizedString")]
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

        [XmlAttribute(AttributeName = "SettingValue", DataType = "string")]
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


    [XmlRoot(ElementName = "Configuration", IsNullable = false), Serializable]
    public partial class Configuration : ModelObject
    {

        [XmlIgnore]
        private Jobs __Jobs;

        [XmlElement(Type = typeof(Jobs), ElementName = "Jobs", IsNullable = false)]
        public Jobs Jobs
        {
            get
            {
                if (__Jobs == null)
                {
                    __Jobs = new Jobs();
                    this.RaisePropertyChangedEvent("Jobs", null, __Jobs);
                }
                return __Jobs;
            }
            set
            {
                if (value != __Jobs)
                {
                    Jobs oldValue = __Jobs;
                    __Jobs = value;
                    this.RaisePropertyChangedEvent("Jobs", oldValue, value);
                }
            }
        }

        public Configuration()
        {
        }
    }


    [XmlType(TypeName = "Jobs"), Serializable]
    public partial class Jobs : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<Job> __Job;

        [XmlElement(Type = typeof(Job), ElementName = "Job", IsNullable = false)]
        public NotifyingCollection<Job> Job
        {
            get
            {
                if (__Job == null)
                {
                    __Job = new NotifyingCollection<Job>();
                }
                return __Job;
            }
        }

        public Jobs()
        {
        }
    }


    [XmlType(TypeName = "Job"), Serializable]
    public partial class Job : ModelObject
    {

        [XmlIgnore]
        private string __ReferenceName;

        [XmlAttribute(AttributeName = "ReferenceName", DataType = "string")]
        public string ReferenceName
        {
            get
            {
                return __ReferenceName;
            }
            set
            {
                if (value != __ReferenceName)
                {
                    string oldValue = __ReferenceName;
                    __ReferenceName = value;
                    this.RaisePropertyChangedEvent("ReferenceName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private bool __Enabled;

        [XmlAttribute(AttributeName = "Enabled", DataType = "boolean")]
        public bool Enabled
        {
            get
            {
                return __Enabled;
            }
            set
            {
                if (value != __Enabled)
                {
                    bool oldValue = __Enabled;
                    __Enabled = value;
                    this.RaisePropertyChangedEvent("Enabled", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private Trigger __Trigger;

        [XmlElement(Type = typeof(Trigger), ElementName = "Trigger", IsNullable = false)]
        public Trigger Trigger
        {
            get
            {
                if (__Trigger == null)
                {
                    __Trigger = new Trigger();
                    this.RaisePropertyChangedEvent("Trigger", null, __Trigger);
                }
                return __Trigger;
            }
            set
            {
                if (value != __Trigger)
                {
                    Trigger oldValue = __Trigger;
                    __Trigger = value;
                    this.RaisePropertyChangedEvent("Trigger", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private Settings __Settings;

        [XmlElement(Type = typeof(Settings), ElementName = "Settings", IsNullable = false)]
        public Settings Settings
        {
            get
            {
                if (__Settings == null)
                {
                    __Settings = new Settings();
                    this.RaisePropertyChangedEvent("Settings", null, __Settings);
                }
                return __Settings;
            }
            set
            {
                if (value != __Settings)
                {
                    Settings oldValue = __Settings;
                    __Settings = value;
                    this.RaisePropertyChangedEvent("Settings", oldValue, value);
                }
            }
        }

        public Job()
        {
        }
    }


    [XmlType(TypeName = "Trigger"), Serializable]
    public partial class Trigger : ModelObject
    {

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.TriggerOption __Option;

        [XmlAttribute(AttributeName = "Option")]
        public Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.TriggerOption Option
        {
            get
            {
                return __Option;
            }
            set
            {
                if (value != __Option)
                {
                    Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.TriggerOption oldValue = __Option;
                    __Option = value;
                    this.RaisePropertyChangedEvent("Option", oldValue, value);
                }
            }
        }

        [XmlElement(ElementName = "Setting", IsNullable = false, DataType = "string")]
        public string __Setting;

        [XmlIgnore]
        public string Setting
        {
            get { return __Setting; }
            set { __Setting = value; }
        }

        public Trigger()
        {
        }
    }


    [XmlType(TypeName = "Settings"), Serializable]
    public partial class Settings : ModelObject
    {

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.PropertyBagType __NamedSettings;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.PropertyBagType), ElementName = "NamedSettings", IsNullable = false)]
        public Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.PropertyBagType NamedSettings
        {
            get
            {
                if (__NamedSettings == null)
                {
                    __NamedSettings = new Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.PropertyBagType();
                    this.RaisePropertyChangedEvent("NamedSettings", null, __NamedSettings);
                }
                return __NamedSettings;
            }
            set
            {
                if (value != __NamedSettings)
                {
                    Microsoft.TeamFoundation.Migration.TfsIntegrationJobService.PropertyBagType oldValue = __NamedSettings;
                    __NamedSettings = value;
                    this.RaisePropertyChangedEvent("NamedSettings", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private CustomSettings __CustomSettings;

        [XmlElement(Type = typeof(CustomSettings), ElementName = "CustomSettings", IsNullable = false)]
        public CustomSettings CustomSettings
        {
            get
            {
                if (__CustomSettings == null)
                {
                    __CustomSettings = new CustomSettings();
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
                return __CustomSettings;
            }
            set
            {
                if (value != __CustomSettings)
                {
                    CustomSettings oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
        }

        public Settings()
        {
        }
    }


    [XmlType(TypeName = "CustomSettings"), Serializable]
    public partial class CustomSettings : ModelObject
    {

        [XmlAnyElement()]
        public System.Xml.XmlElement[] Any;

        public CustomSettings()
        {
        }
    }
}

#pragma warning restore 1591
