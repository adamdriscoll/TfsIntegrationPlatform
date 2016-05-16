// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//--------------------------------------------------------------
// Generated from:
// Schema file: WitSessionSettingXmlSchema.xsd
// Creation Date: 5/10/2010 9:36:17 PM
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

namespace Microsoft.TeamFoundation.Migration.BusinessModel.WIT
{

    public struct Declarations
    {
        public const string SchemaVersion = "";
    }

    [Serializable]
    public enum UserIdPropertyNameEnum
    {
        [XmlEnum(Name = "DisplayName")]
        DisplayName,
        [XmlEnum(Name = "Domain")]
        Domain,
        [XmlEnum(Name = "Alias")]
        Alias,
        [XmlEnum(Name = "EmailAddress")]
        EmailAddress,
        [XmlEnum(Name = "UniqueId")]
        UniqueId,
        [XmlEnum(Name = "QualifiedName")]
        QualifiedName,
        [XmlEnum(Name = "DomainAlias")]
        DomainAlias
    }

    [Serializable]
    public enum SourceSideTypeEnum
    {
        [XmlEnum(Name = "Left")]
        Left,
        [XmlEnum(Name = "Right")]
        Right,
        [XmlEnum(Name = "Any")]
        Any
    }




    [XmlType(TypeName = "WITSessionCustomSettingElement"), Serializable]
    public partial class WITSessionCustomSettingElement : ModelObject
    {

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SettingsElement __Settings;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SettingsElement), ElementName = "Settings", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SettingsElement Settings
        {
            get
            {
                if (__Settings == null)
                {
                    __Settings = new Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SettingsElement();
                    this.RaisePropertyChangedEvent("Settings", null, __Settings);
                }
                return __Settings;
            }
            set
            {
                if (value != __Settings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SettingsElement oldValue = __Settings;
                    __Settings = value;
                    this.RaisePropertyChangedEvent("Settings", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private WorkItemTypes __WorkItemTypes;

        [XmlElement(Type = typeof(WorkItemTypes), ElementName = "WorkItemTypes", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public WorkItemTypes WorkItemTypes
        {
            get
            {
                if (__WorkItemTypes == null)
                {
                    __WorkItemTypes = new WorkItemTypes();
                    this.RaisePropertyChangedEvent("WorkItemTypes", null, __WorkItemTypes);
                }
                return __WorkItemTypes;
            }
            set
            {
                if (value != __WorkItemTypes)
                {
                    WorkItemTypes oldValue = __WorkItemTypes;
                    __WorkItemTypes = value;
                    this.RaisePropertyChangedEvent("WorkItemTypes", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private FieldMaps __FieldMaps;

        [XmlElement(Type = typeof(FieldMaps), ElementName = "FieldMaps", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public FieldMaps FieldMaps
        {
            get
            {
                if (__FieldMaps == null)
                {
                    __FieldMaps = new FieldMaps();
                    this.RaisePropertyChangedEvent("FieldMaps", null, __FieldMaps);
                }
                return __FieldMaps;
            }
            set
            {
                if (value != __FieldMaps)
                {
                    FieldMaps oldValue = __FieldMaps;
                    __FieldMaps = value;
                    this.RaisePropertyChangedEvent("FieldMaps", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private ValueMaps __ValueMaps;

        [XmlElement(Type = typeof(ValueMaps), ElementName = "ValueMaps", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public ValueMaps ValueMaps
        {
            get
            {
                if (__ValueMaps == null)
                {
                    __ValueMaps = new ValueMaps();
                    this.RaisePropertyChangedEvent("ValueMaps", null, __ValueMaps);
                }
                return __ValueMaps;
            }
            set
            {
                if (value != __ValueMaps)
                {
                    ValueMaps oldValue = __ValueMaps;
                    __ValueMaps = value;
                    this.RaisePropertyChangedEvent("ValueMaps", oldValue, value);
                }
            }
        }

        public WITSessionCustomSettingElement()
        {
        }
    }


    [XmlType(TypeName = "WorkItemTypes"), Serializable]
    public partial class WorkItemTypes : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.WIT.WorkItemTypeMappingElement> __WorkItemType;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.WIT.WorkItemTypeMappingElement), ElementName = "WorkItemType", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.WIT.WorkItemTypeMappingElement> WorkItemType
        {
            get
            {
                if (__WorkItemType == null)
                {
                    __WorkItemType = new NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.WIT.WorkItemTypeMappingElement>();
                }
                return __WorkItemType;
            }
        }

        public WorkItemTypes()
        {
        }
    }


    [XmlType(TypeName = "FieldMaps"), Serializable]
    public partial class FieldMaps : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<FieldMap> __FieldMap;

        [XmlElement(Type = typeof(FieldMap), ElementName = "FieldMap", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<FieldMap> FieldMap
        {
            get
            {
                if (__FieldMap == null)
                {
                    __FieldMap = new NotifyingCollection<FieldMap>();
                }
                return __FieldMap;
            }
        }

        public FieldMaps()
        {
        }
    }


    [XmlType(TypeName = "FieldMap"), Serializable]
    public partial class FieldMap : ModelObject
    {

        [XmlIgnore]
        private string __name;

        [XmlAttribute(AttributeName = "name", Form = XmlSchemaForm.Unqualified, DataType = "NCName")]
        public string name
        {
            get
            {
                return __name;
            }
            set
            {
                if (value != __name)
                {
                    string oldValue = __name;
                    __name = value;
                    this.RaisePropertyChangedEvent("name", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private MappedFields __MappedFields;

        [XmlElement(Type = typeof(MappedFields), ElementName = "MappedFields", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public MappedFields MappedFields
        {
            get
            {
                if (__MappedFields == null)
                {
                    __MappedFields = new MappedFields();
                    this.RaisePropertyChangedEvent("MappedFields", null, __MappedFields);
                }
                return __MappedFields;
            }
            set
            {
                if (value != __MappedFields)
                {
                    MappedFields oldValue = __MappedFields;
                    __MappedFields = value;
                    this.RaisePropertyChangedEvent("MappedFields", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private AggregatedFields __AggregatedFields;

        [XmlElement(Type = typeof(AggregatedFields), ElementName = "AggregatedFields", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public AggregatedFields AggregatedFields
        {
            get
            {
                if (__AggregatedFields == null)
                {
                    __AggregatedFields = new AggregatedFields();
                    this.RaisePropertyChangedEvent("AggregatedFields", null, __AggregatedFields);
                }
                return __AggregatedFields;
            }
            set
            {
                if (value != __AggregatedFields)
                {
                    AggregatedFields oldValue = __AggregatedFields;
                    __AggregatedFields = value;
                    this.RaisePropertyChangedEvent("AggregatedFields", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private UserIdentityFields __UserIdentityFields;

        [XmlElement(Type = typeof(UserIdentityFields), ElementName = "UserIdentityFields", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public UserIdentityFields UserIdentityFields
        {
            get
            {
                if (__UserIdentityFields == null)
                {
                    __UserIdentityFields = new UserIdentityFields();
                    this.RaisePropertyChangedEvent("UserIdentityFields", null, __UserIdentityFields);
                }
                return __UserIdentityFields;
            }
            set
            {
                if (value != __UserIdentityFields)
                {
                    UserIdentityFields oldValue = __UserIdentityFields;
                    __UserIdentityFields = value;
                    this.RaisePropertyChangedEvent("UserIdentityFields", oldValue, value);
                }
            }
        }

        public FieldMap()
        {
        }
    }


    [XmlType(TypeName = "MappedFields"), Serializable]
    public partial class MappedFields : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<MappedField> __MappedField;

        [XmlElement(Type = typeof(MappedField), ElementName = "MappedField", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<MappedField> MappedField
        {
            get
            {
                if (__MappedField == null)
                {
                    __MappedField = new NotifyingCollection<MappedField>();
                }
                return __MappedField;
            }
        }

        public MappedFields()
        {
        }
    }


    [XmlType(TypeName = "MappedField"), Serializable]
    public partial class MappedField : ModelObject
    {

        [XmlIgnore]
        private string __LeftName;

        [XmlAttribute(AttributeName = "LeftName", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string LeftName
        {
            get
            {
                return __LeftName;
            }
            set
            {
                if (value != __LeftName)
                {
                    string oldValue = __LeftName;
                    __LeftName = value;
                    this.RaisePropertyChangedEvent("LeftName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __RightName;

        [XmlAttribute(AttributeName = "RightName", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string RightName
        {
            get
            {
                return __RightName;
            }
            set
            {
                if (value != __RightName)
                {
                    string oldValue = __RightName;
                    __RightName = value;
                    this.RaisePropertyChangedEvent("RightName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SourceSideTypeEnum __MapFromSide;

        [XmlAttribute(AttributeName = "MapFromSide", Form = XmlSchemaForm.Unqualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SourceSideTypeEnum MapFromSide
        {
            get
            {
                return __MapFromSide;
            }
            set
            {
                if (value != __MapFromSide)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SourceSideTypeEnum oldValue = __MapFromSide;
                    __MapFromSide = value;
                    this.RaisePropertyChangedEvent("MapFromSide", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __valueMap;

        [XmlAttribute(AttributeName = "valueMap", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string valueMap
        {
            get
            {
                return __valueMap;
            }
            set
            {
                if (value != __valueMap)
                {
                    string oldValue = __valueMap;
                    __valueMap = value;
                    this.RaisePropertyChangedEvent("valueMap", oldValue, value);
                }
            }
        }

        public MappedField()
        {
        }
    }


    [XmlType(TypeName = "AggregatedFields"), Serializable]
    public partial class AggregatedFields : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<FieldsAggregationGroup> __FieldsAggregationGroup;

        [XmlElement(Type = typeof(FieldsAggregationGroup), ElementName = "FieldsAggregationGroup", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<FieldsAggregationGroup> FieldsAggregationGroup
        {
            get
            {
                if (__FieldsAggregationGroup == null)
                {
                    __FieldsAggregationGroup = new NotifyingCollection<FieldsAggregationGroup>();
                }
                return __FieldsAggregationGroup;
            }
        }

        public AggregatedFields()
        {
        }
    }


    [XmlType(TypeName = "FieldsAggregationGroup"), Serializable]
    public partial class FieldsAggregationGroup : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SourceSideTypeEnum __MapFromSide;

        [XmlAttribute(AttributeName = "MapFromSide", Form = XmlSchemaForm.Unqualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SourceSideTypeEnum MapFromSide
        {
            get
            {
                return __MapFromSide;
            }
            set
            {
                if (value != __MapFromSide)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WIT.SourceSideTypeEnum oldValue = __MapFromSide;
                    __MapFromSide = value;
                    this.RaisePropertyChangedEvent("MapFromSide", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __TargetFieldName;

        [XmlAttribute(AttributeName = "TargetFieldName", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string TargetFieldName
        {
            get
            {
                return __TargetFieldName;
            }
            set
            {
                if (value != __TargetFieldName)
                {
                    string oldValue = __TargetFieldName;
                    __TargetFieldName = value;
                    this.RaisePropertyChangedEvent("TargetFieldName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __Format;

        [XmlAttribute(AttributeName = "Format", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string Format
        {
            get
            {
                return __Format;
            }
            set
            {
                if (value != __Format)
                {
                    string oldValue = __Format;
                    __Format = value;
                    this.RaisePropertyChangedEvent("Format", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private NotifyingCollection<SourceField> __SourceField;

        [XmlElement(Type = typeof(SourceField), ElementName = "SourceField", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<SourceField> SourceField
        {
            get
            {
                if (__SourceField == null)
                {
                    __SourceField = new NotifyingCollection<SourceField>();
                }
                return __SourceField;
            }
        }

        public FieldsAggregationGroup()
        {
        }
    }


    [XmlType(TypeName = "SourceField"), Serializable]
    public partial class SourceField : ModelObject
    {

        [XmlIgnore]
        private int __Index;

        [XmlAttribute(AttributeName = "Index", Form = XmlSchemaForm.Unqualified, DataType = "int")]
        public int Index
        {
            get
            {
                return __Index;
            }
            set
            {
                if (value != __Index)
                {
                    int oldValue = __Index;
                    __Index = value;
                    this.RaisePropertyChangedEvent("Index", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __SourceFieldName;

        [XmlAttribute(AttributeName = "SourceFieldName", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string SourceFieldName
        {
            get
            {
                return __SourceFieldName;
            }
            set
            {
                if (value != __SourceFieldName)
                {
                    string oldValue = __SourceFieldName;
                    __SourceFieldName = value;
                    this.RaisePropertyChangedEvent("SourceFieldName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __valueMap;

        [XmlAttribute(AttributeName = "valueMap", Form = XmlSchemaForm.Unqualified, DataType = "NCName")]
        public string valueMap
        {
            get
            {
                return __valueMap;
            }
            set
            {
                if (value != __valueMap)
                {
                    string oldValue = __valueMap;
                    __valueMap = value;
                    this.RaisePropertyChangedEvent("valueMap", oldValue, value);
                }
            }
        }

        public SourceField()
        {
        }
    }


    [XmlType(TypeName = "UserIdentityFields"), Serializable]
    public partial class UserIdentityFields : ModelObject
    {

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement __LeftUserIdentityFields;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement), ElementName = "LeftUserIdentityFields", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement LeftUserIdentityFields
        {
            get
            {
                if (__LeftUserIdentityFields == null)
                {
                    __LeftUserIdentityFields = new Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement();
                    this.RaisePropertyChangedEvent("LeftUserIdentityFields", null, __LeftUserIdentityFields);
                }
                return __LeftUserIdentityFields;
            }
            set
            {
                if (value != __LeftUserIdentityFields)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement oldValue = __LeftUserIdentityFields;
                    __LeftUserIdentityFields = value;
                    this.RaisePropertyChangedEvent("LeftUserIdentityFields", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement __RightUserIdentityFields;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement), ElementName = "RightUserIdentityFields", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement RightUserIdentityFields
        {
            get
            {
                if (__RightUserIdentityFields == null)
                {
                    __RightUserIdentityFields = new Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement();
                    this.RaisePropertyChangedEvent("RightUserIdentityFields", null, __RightUserIdentityFields);
                }
                return __RightUserIdentityFields;
            }
            set
            {
                if (value != __RightUserIdentityFields)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldsElement oldValue = __RightUserIdentityFields;
                    __RightUserIdentityFields = value;
                    this.RaisePropertyChangedEvent("RightUserIdentityFields", oldValue, value);
                }
            }
        }

        public UserIdentityFields()
        {
        }
    }


    [XmlType(TypeName = "ValueMaps"), Serializable]
    public partial class ValueMaps : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<ValueMap> __ValueMap;

        [XmlElement(Type = typeof(ValueMap), ElementName = "ValueMap", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<ValueMap> ValueMap
        {
            get
            {
                if (__ValueMap == null)
                {
                    __ValueMap = new NotifyingCollection<ValueMap>();
                }
                return __ValueMap;
            }
        }

        public ValueMaps()
        {
        }
    }


    [XmlType(TypeName = "ValueMap"), Serializable]
    public partial class ValueMap : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private string __name;

        [XmlAttribute(AttributeName = "name", Form = XmlSchemaForm.Unqualified, DataType = "NCName")]
        public string name
        {
            get
            {
                return __name;
            }
            set
            {
                if (value != __name)
                {
                    string oldValue = __name;
                    __name = value;
                    this.RaisePropertyChangedEvent("name", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private NotifyingCollection<@Value> __Value;

        [XmlElement(Type = typeof(@Value), ElementName = "Value", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<@Value> Value
        {
            get
            {
                if (__Value == null)
                {
                    __Value = new NotifyingCollection<@Value>();
                }
                return __Value;
            }
        }

        public ValueMap()
        {
        }
    }


    [XmlType(TypeName = "Value"), Serializable]
    public partial class @Value : ModelObject
    {

        [XmlIgnore]
        private string __LeftValue;

        [XmlAttribute(AttributeName = "LeftValue", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string LeftValue
        {
            get
            {
                return __LeftValue;
            }
            set
            {
                if (value != __LeftValue)
                {
                    string oldValue = __LeftValue;
                    __LeftValue = value;
                    this.RaisePropertyChangedEvent("LeftValue", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __RightValue;

        [XmlAttribute(AttributeName = "RightValue", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string RightValue
        {
            get
            {
                return __RightValue;
            }
            set
            {
                if (value != __RightValue)
                {
                    string oldValue = __RightValue;
                    __RightValue = value;
                    this.RaisePropertyChangedEvent("RightValue", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private When __When;

        [XmlElement(Type = typeof(When), ElementName = "When", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public When When
        {
            get
            {
                if (__When == null)
                {
                    __When = new When();
                    this.RaisePropertyChangedEvent("When", null, __When);
                }
                return __When;
            }
            set
            {
                if (value != __When)
                {
                    When oldValue = __When;
                    __When = value;
                    this.RaisePropertyChangedEvent("When", oldValue, value);
                }
            }
        }

        public @Value()
        {
        }
    }


    [XmlType(TypeName = "When"), Serializable]
    public partial class When : ModelObject
    {

        [XmlIgnore]
        private string __ConditionalSrcFieldName;

        [XmlAttribute(AttributeName = "ConditionalSrcFieldName", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string ConditionalSrcFieldName
        {
            get
            {
                return __ConditionalSrcFieldName;
            }
            set
            {
                if (value != __ConditionalSrcFieldName)
                {
                    string oldValue = __ConditionalSrcFieldName;
                    __ConditionalSrcFieldName = value;
                    this.RaisePropertyChangedEvent("ConditionalSrcFieldName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __ConditionalSrcFieldValue;

        [XmlAttribute(AttributeName = "ConditionalSrcFieldValue", Form = XmlSchemaForm.Unqualified, DataType = "string")]
        public string ConditionalSrcFieldValue
        {
            get
            {
                return __ConditionalSrcFieldValue;
            }
            set
            {
                if (value != __ConditionalSrcFieldValue)
                {
                    string oldValue = __ConditionalSrcFieldValue;
                    __ConditionalSrcFieldValue = value;
                    this.RaisePropertyChangedEvent("ConditionalSrcFieldValue", oldValue, value);
                }
            }
        }

        public When()
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


    [XmlType(TypeName = "WorkItemTypeMappingElement"), Serializable]
    public partial class WorkItemTypeMappingElement : ModelObject
    {

        [XmlIgnore]
        private string __LeftWorkItemTypeName;

        [XmlAttribute(AttributeName = "LeftWorkItemTypeName", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string LeftWorkItemTypeName
        {
            get
            {
                return __LeftWorkItemTypeName;
            }
            set
            {
                if (value != __LeftWorkItemTypeName)
                {
                    string oldValue = __LeftWorkItemTypeName;
                    __LeftWorkItemTypeName = value;
                    this.RaisePropertyChangedEvent("LeftWorkItemTypeName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __RightWorkItemTypeName;

        [XmlAttribute(AttributeName = "RightWorkItemTypeName", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string RightWorkItemTypeName
        {
            get
            {
                return __RightWorkItemTypeName;
            }
            set
            {
                if (value != __RightWorkItemTypeName)
                {
                    string oldValue = __RightWorkItemTypeName;
                    __RightWorkItemTypeName = value;
                    this.RaisePropertyChangedEvent("RightWorkItemTypeName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private string __fieldMap;

        [XmlAttribute(AttributeName = "fieldMap", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string fieldMap
        {
            get
            {
                return __fieldMap;
            }
            set
            {
                if (value != __fieldMap)
                {
                    string oldValue = __fieldMap;
                    __fieldMap = value;
                    this.RaisePropertyChangedEvent("fieldMap", oldValue, value);
                }
            }
        }

        public WorkItemTypeMappingElement()
        {
        }
    }


    [XmlType(TypeName = "UserIdFieldsElement"), Serializable]
    public partial class UserIdFieldsElement : ModelObject
    {
        //sClassEnumerabilityTemplate


        [XmlIgnore]
        private NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldElement> __UserIdField;

        [XmlElement(Type = typeof(Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldElement), ElementName = "UserIdField", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldElement> UserIdField
        {
            get
            {
                if (__UserIdField == null)
                {
                    __UserIdField = new NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdFieldElement>();
                }
                return __UserIdField;
            }
        }

        public UserIdFieldsElement()
        {
        }
    }


    [XmlType(TypeName = "UserIdFieldElement"), Serializable]
    public partial class UserIdFieldElement : ModelObject
    {

        [XmlIgnore]
        private string __FieldReferenceName;

        [XmlAttribute(AttributeName = "FieldReferenceName", Form = XmlSchemaForm.Unqualified, DataType = "normalizedString")]
        public string FieldReferenceName
        {
            get
            {
                return __FieldReferenceName;
            }
            set
            {
                if (value != __FieldReferenceName)
                {
                    string oldValue = __FieldReferenceName;
                    __FieldReferenceName = value;
                    this.RaisePropertyChangedEvent("FieldReferenceName", oldValue, value);
                }
            }
        }

        [XmlIgnore]
        private Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdPropertyNameEnum __UserIdPropertyName;

        [XmlAttribute(AttributeName = "UserIdPropertyName", Form = XmlSchemaForm.Unqualified)]
        public Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdPropertyNameEnum UserIdPropertyName
        {
            get
            {
                return __UserIdPropertyName;
            }
            set
            {
                if (value != __UserIdPropertyName)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WIT.UserIdPropertyNameEnum oldValue = __UserIdPropertyName;
                    __UserIdPropertyName = value;
                    this.RaisePropertyChangedEvent("UserIdPropertyName", oldValue, value);
                }
            }
        }

        public UserIdFieldElement()
        {
        }
    }


    [XmlRoot(ElementName = "WITSessionCustomSetting", IsNullable = false), Serializable]
    public partial class WITSessionCustomSetting : Microsoft.TeamFoundation.Migration.BusinessModel.WIT.WITSessionCustomSettingElement
    {

        public WITSessionCustomSetting()
            : base()
        {
        }
    }
}

#pragma warning restore 1591
