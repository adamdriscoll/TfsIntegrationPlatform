﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3053
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=2.0.50727.42.
// 
namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    using System.Xml.Serialization;


    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ConflictResolutionRule : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string ruleReferenceNameField;

        private string actionReferenceNameField;

        private string ruleDescriptionField;

        private string applicabilityScopeField;

        private DataField[] dataFieldField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "normalizedString")]
        public string RuleReferenceName
        {
            get
            {
                return this.ruleReferenceNameField;
            }
            set
            {
                this.ruleReferenceNameField = value;
                this.RaisePropertyChanged("RuleReferenceName");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "normalizedString")]
        public string ActionReferenceName
        {
            get
            {
                return this.actionReferenceNameField;
            }
            set
            {
                this.actionReferenceNameField = value;
                this.RaisePropertyChanged("ActionReferenceName");
            }
        }

        /// <remarks/>
        public string RuleDescription
        {
            get
            {
                return this.ruleDescriptionField;
            }
            set
            {
                this.ruleDescriptionField = value;
                this.RaisePropertyChanged("RuleDescription");
            }
        }

        /// <remarks/>
        public string ApplicabilityScope
        {
            get
            {
                return this.applicabilityScopeField;
            }
            set
            {
                this.applicabilityScopeField = value;
                this.RaisePropertyChanged("ApplicabilityScope");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("DataField")]
        public DataField[] DataField
        {
            get
            {
                return this.dataFieldField;
            }
            set
            {
                this.dataFieldField = value;
                this.RaisePropertyChanged("DataField");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.42")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class DataField : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string fieldNameField;

        private string fieldValueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FieldName
        {
            get
            {
                return this.fieldNameField;
            }
            set
            {
                this.fieldNameField = value;
                this.RaisePropertyChanged("FieldName");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FieldValue
        {
            get
            {
                return this.fieldValueField;
            }
            set
            {
                this.fieldValueField = value;
                this.RaisePropertyChanged("FieldValue");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}