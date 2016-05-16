// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//--------------------------------------------------------------
// Generated from:
// Schema file: TfsMigrationConfigurationXMLSchema.xsd
// Creation Date: 6/10/2010 3:12:22 PM
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

namespace Microsoft.TeamFoundation.Migration.BusinessModel
{

	public struct Declarations
	{
		public const string SchemaVersion = "";
	}

	[Serializable]
	public enum SessionTypeEnum
	{
		[XmlEnum(Name="VersionControl")] VersionControl,
		[XmlEnum(Name="WorkItemTracking")] WorkItemTracking
	}

	[Serializable]
	public enum Frequency
	{
		[XmlEnum(Name="ContinuousManual")] ContinuousManual,
		[XmlEnum(Name="ContinuousAutomatic")] ContinuousAutomatic,
		[XmlEnum(Name="OneTime")] OneTime
	}

	[Serializable]
	public enum UserIdPropertyNameEnum
	{
		[XmlEnum(Name="DisplayName")] DisplayName,
		[XmlEnum(Name="Domain")] Domain,
		[XmlEnum(Name="Alias")] Alias,
		[XmlEnum(Name="EmailAddress")] EmailAddress,
		[XmlEnum(Name="UniqueId")] UniqueId,
		[XmlEnum(Name="QualifiedName")] QualifiedName,
		[XmlEnum(Name="DomainAlias")] DomainAlias
	}

	[Serializable]
	public enum MappingDirectionEnum
	{
		[XmlEnum(Name="LeftToRight")] LeftToRight,
		[XmlEnum(Name="RightToLeft")] RightToLeft,
		[XmlEnum(Name="TwoWay")] TwoWay
	}

	[Serializable]
	public enum SyncContext
	{
		[XmlEnum(Name="Disabled")] Disabled,
		[XmlEnum(Name="Unidirectional")] Unidirectional,
		[XmlEnum(Name="Bidirectional")] Bidirectional
	}

	[Serializable]
	public enum MappingRules
	{
		[XmlEnum(Name="SimpleReplacement")] SimpleReplacement,
		[XmlEnum(Name="FormatStringComposition")] FormatStringComposition,
		[XmlEnum(Name="FormatStringDecomposition")] FormatStringDecomposition,
		[XmlEnum(Name="Ignore")] Ignore
	}

	[Serializable]
	public enum DirectionOfFlow
	{
		[XmlEnum(Name="Unidirectional")] Unidirectional,
		[XmlEnum(Name="Bidirectional")] Bidirectional
	}




	[XmlType(TypeName="User"),Serializable]
	public partial class User : ModelObject
	{

		[XmlIgnore]
		private string __Alias;
		
		[XmlAttribute(AttributeName="Alias",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string Alias
		{ 
			get 
            { 
                return __Alias; 
            }
			set 
            {
                if (value != __Alias)
                {
                    string oldValue = __Alias;
                    __Alias = value;
                    this.RaisePropertyChangedEvent("Alias", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __Domain;
		
		[XmlAttribute(AttributeName="Domain",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Domain
		{ 
			get 
            { 
                return __Domain; 
            }
			set 
            {
                if (value != __Domain)
                {
                    string oldValue = __Domain;
                    __Domain = value;
                    this.RaisePropertyChangedEvent("Domain", oldValue, value);
                }
            }
		}

		public User()
		{
		}
	}


	[XmlType(TypeName="ConfigurationElement"),Serializable]
	public partial class ConfigurationElement : ModelObject
	{

		[XmlIgnore]
		private string __UniqueId;
		
		[XmlAttribute(AttributeName="UniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string UniqueId
		{ 
			get 
            { 
                return __UniqueId; 
            }
			set 
            {
                if (value != __UniqueId)
                {
                    string oldValue = __UniqueId;
                    __UniqueId = value;
                    this.RaisePropertyChangedEvent("UniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Providers __Providers;
		
		[XmlElement(Type=typeof(Providers),ElementName="Providers",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Providers Providers
		{
			get
			{
				if (__Providers == null) 
                {
                    __Providers = new Providers();		
                    this.RaisePropertyChangedEvent("Providers", null, __Providers);
                }
				return __Providers;
			}
			set 
            {
                if (value != __Providers)
                {
                    Providers oldValue = __Providers;
                    __Providers = value;
                    this.RaisePropertyChangedEvent("Providers", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Addins __Addins;
		
		[XmlElement(Type=typeof(Addins),ElementName="Addins",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Addins Addins
		{
			get
			{
				if (__Addins == null) 
                {
                    __Addins = new Addins();		
                    this.RaisePropertyChangedEvent("Addins", null, __Addins);
                }
				return __Addins;
			}
			set 
            {
                if (value != __Addins)
                {
                    Addins oldValue = __Addins;
                    __Addins = value;
                    this.RaisePropertyChangedEvent("Addins", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.SessionGroupElement __SessionGroup;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.SessionGroupElement),ElementName="SessionGroup",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.SessionGroupElement SessionGroup
		{
			get
			{
				if (__SessionGroup == null) 
                {
                    __SessionGroup = new Microsoft.TeamFoundation.Migration.BusinessModel.SessionGroupElement();		
                    this.RaisePropertyChangedEvent("SessionGroup", null, __SessionGroup);
                }
				return __SessionGroup;
			}
			set 
            {
                if (value != __SessionGroup)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.SessionGroupElement oldValue = __SessionGroup;
                    __SessionGroup = value;
                    this.RaisePropertyChangedEvent("SessionGroup", oldValue, value);
                }
            }
		}

		public ConfigurationElement()
		{
		}
	}


	[XmlType(TypeName="Providers"),Serializable]
	public partial class Providers : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.ProviderElement> __Provider;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.ProviderElement),ElementName="Provider",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.ProviderElement> Provider
		{
			get
			{
				if (__Provider == null) 
                {
                    __Provider = new NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.ProviderElement>();
                }
			    return __Provider;
            }
		}

		public Providers()
		{
		}
	}


	[XmlType(TypeName="Addins"),Serializable]
	public partial class Addins : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement> __Addin;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement),ElementName="Addin",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement> Addin
		{
			get
			{
				if (__Addin == null) 
                {
                    __Addin = new NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement>();
                }
			    return __Addin;
            }
		}

		public Addins()
		{
		}
	}


	[XmlType(TypeName="ProviderElement"),Serializable]
	public partial class ProviderElement : ModelObject
	{

		[XmlIgnore]
		private string __ReferenceName;
		
		[XmlAttribute(AttributeName="ReferenceName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
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
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		public ProviderElement()
		{
		}
	}


	[XmlType(TypeName="AddinElement"),Serializable]
	public partial class AddinElement : ModelObject
	{

		[XmlIgnore]
		private string __ReferenceName;
		
		[XmlAttribute(AttributeName="ReferenceName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
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
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement __CustomSettings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement),ElementName="CustomSettings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement CustomSettings
		{
			get
			{
				if (__CustomSettings == null) 
                {
                    __CustomSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement();		
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
				return __CustomSettings;
			}
			set 
            {
                if (value != __CustomSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
		}

		public AddinElement()
		{
		}
	}


	[XmlType(TypeName="SessionGroupElement"),Serializable]
	public partial class SessionGroupElement : ModelObject
	{

		[XmlIgnore]
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __SessionGroupGUID;
		
		[XmlAttribute(AttributeName="SessionGroupGUID",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string SessionGroupGUID
		{ 
			get 
            { 
                return __SessionGroupGUID; 
            }
			set 
            {
                if (value != __SessionGroupGUID)
                {
                    string oldValue = __SessionGroupGUID;
                    __SessionGroupGUID = value;
                    this.RaisePropertyChangedEvent("SessionGroupGUID", oldValue, value);
                }
            }
		}

		[XmlAttribute(AttributeName="CreationTime",Form=XmlSchemaForm.Unqualified,DataType="dateTime")]
		public System.DateTime __CreationTime;
		
		[XmlIgnore]
		public bool __CreationTimeSpecified;
		
		[XmlIgnore]
		public System.DateTime CreationTime
		{ 
			get { return __CreationTime; }
			set { __CreationTime = value; __CreationTimeSpecified = true; }
		}
		
		[XmlIgnore]
		public System.DateTime CreationTimeUtc
		{ 
			get { return __CreationTime.ToUniversalTime(); }
			set { __CreationTime = value.ToLocalTime(); __CreationTimeSpecified = true; }
		}

		[XmlIgnore]
		private string __Creator;
		
		[XmlAttribute(AttributeName="Creator",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string Creator
		{ 
			get 
            { 
                return __Creator; 
            }
			set 
            {
                if (value != __Creator)
                {
                    string oldValue = __Creator;
                    __Creator = value;
                    this.RaisePropertyChangedEvent("Creator", oldValue, value);
                }
            }
		}

		[XmlAttribute(AttributeName="DeprecationTime",Form=XmlSchemaForm.Unqualified,DataType="dateTime")]
		public System.DateTime __DeprecationTime;
		
		[XmlIgnore]
		public bool __DeprecationTimeSpecified;
		
		[XmlIgnore]
		public System.DateTime DeprecationTime
		{ 
			get { return __DeprecationTime; }
			set { __DeprecationTime = value; __DeprecationTimeSpecified = true; }
		}
		
		[XmlIgnore]
		public System.DateTime DeprecationTimeUtc
		{ 
			get { return __DeprecationTime.ToUniversalTime(); }
			set { __DeprecationTime = value.ToLocalTime(); __DeprecationTimeSpecified = true; }
		}

		[XmlIgnore]
		private int __SyncIntervalInSeconds;
		
		[XmlAttribute(AttributeName="SyncIntervalInSeconds",Form=XmlSchemaForm.Unqualified,DataType="int")]
		public int SyncIntervalInSeconds
		{ 
			get 
            { 
                return __SyncIntervalInSeconds; 
            }
			set 
            {
                if (value != __SyncIntervalInSeconds)
                {
                    int oldValue = __SyncIntervalInSeconds;
                    __SyncIntervalInSeconds = value;
                    this.RaisePropertyChangedEvent("SyncIntervalInSeconds", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private int __SyncDurationInMinutes;
		
		[XmlAttribute(AttributeName="SyncDurationInMinutes",Form=XmlSchemaForm.Unqualified,DataType="int")]
		public int SyncDurationInMinutes
		{ 
			get 
            { 
                return __SyncDurationInMinutes; 
            }
			set 
            {
                if (value != __SyncDurationInMinutes)
                {
                    int oldValue = __SyncDurationInMinutes;
                    __SyncDurationInMinutes = value;
                    this.RaisePropertyChangedEvent("SyncDurationInMinutes", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSourcesElement __MigrationSources;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSourcesElement),ElementName="MigrationSources",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSourcesElement MigrationSources
		{
			get
			{
				if (__MigrationSources == null) 
                {
                    __MigrationSources = new Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSourcesElement();		
                    this.RaisePropertyChangedEvent("MigrationSources", null, __MigrationSources);
                }
				return __MigrationSources;
			}
			set 
            {
                if (value != __MigrationSources)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSourcesElement oldValue = __MigrationSources;
                    __MigrationSources = value;
                    this.RaisePropertyChangedEvent("MigrationSources", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.SessionsElement __Sessions;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.SessionsElement),ElementName="Sessions",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.SessionsElement Sessions
		{
			get
			{
				if (__Sessions == null) 
                {
                    __Sessions = new Microsoft.TeamFoundation.Migration.BusinessModel.SessionsElement();		
                    this.RaisePropertyChangedEvent("Sessions", null, __Sessions);
                }
				return __Sessions;
			}
			set 
            {
                if (value != __Sessions)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.SessionsElement oldValue = __Sessions;
                    __Sessions = value;
                    this.RaisePropertyChangedEvent("Sessions", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.LinkingElement __Linking;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.LinkingElement),ElementName="Linking",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.LinkingElement Linking
		{
			get
			{
				if (__Linking == null) 
                {
                    __Linking = new Microsoft.TeamFoundation.Migration.BusinessModel.LinkingElement();		
                    this.RaisePropertyChangedEvent("Linking", null, __Linking);
                }
				return __Linking;
			}
			set 
            {
                if (value != __Linking)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.LinkingElement oldValue = __Linking;
                    __Linking = value;
                    this.RaisePropertyChangedEvent("Linking", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.WorkFlowType __WorkFlowType;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.WorkFlowType),ElementName="WorkFlowType",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.WorkFlowType WorkFlowType
		{
			get
			{
				if (__WorkFlowType == null) 
                {
                    __WorkFlowType = new Microsoft.TeamFoundation.Migration.BusinessModel.WorkFlowType();		
                    this.RaisePropertyChangedEvent("WorkFlowType", null, __WorkFlowType);
                }
				return __WorkFlowType;
			}
			set 
            {
                if (value != __WorkFlowType)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.WorkFlowType oldValue = __WorkFlowType;
                    __WorkFlowType = value;
                    this.RaisePropertyChangedEvent("WorkFlowType", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement __CustomSettings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement),ElementName="CustomSettings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement CustomSettings
		{
			get
			{
				if (__CustomSettings == null) 
                {
                    __CustomSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement();		
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
				return __CustomSettings;
			}
			set 
            {
                if (value != __CustomSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private UserIdentityMappings __UserIdentityMappings;
		
		[XmlElement(Type=typeof(UserIdentityMappings),ElementName="UserIdentityMappings",IsNullable=true,Form=XmlSchemaForm.Qualified)]
		public UserIdentityMappings UserIdentityMappings
		{
			get
			{
				if (__UserIdentityMappings == null) 
                {
                    __UserIdentityMappings = new UserIdentityMappings();		
                    this.RaisePropertyChangedEvent("UserIdentityMappings", null, __UserIdentityMappings);
                }
				return __UserIdentityMappings;
			}
			set 
            {
                if (value != __UserIdentityMappings)
                {
                    UserIdentityMappings oldValue = __UserIdentityMappings;
                    __UserIdentityMappings = value;
                    this.RaisePropertyChangedEvent("UserIdentityMappings", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private ErrorManagement __ErrorManagement;
		
		[XmlElement(Type=typeof(ErrorManagement),ElementName="ErrorManagement",IsNullable=true,Form=XmlSchemaForm.Qualified)]
		public ErrorManagement ErrorManagement
		{
			get
			{
				if (__ErrorManagement == null) 
                {
                    __ErrorManagement = new ErrorManagement();		
                    this.RaisePropertyChangedEvent("ErrorManagement", null, __ErrorManagement);
                }
				return __ErrorManagement;
			}
			set 
            {
                if (value != __ErrorManagement)
                {
                    ErrorManagement oldValue = __ErrorManagement;
                    __ErrorManagement = value;
                    this.RaisePropertyChangedEvent("ErrorManagement", oldValue, value);
                }
            }
		}

		public SessionGroupElement()
		{
			__CreationTime = System.DateTime.Now;
			__DeprecationTime = System.DateTime.Now;
		}
	}


	[XmlType(TypeName="UserIdentityMappings"),Serializable]
	public partial class UserIdentityMappings : ModelObject
	{

		[XmlIgnore]
		private bool __EnableValidation;
		
		[XmlAttribute(AttributeName="EnableValidation",Form=XmlSchemaForm.Unqualified,DataType="boolean")]
		public bool EnableValidation
		{ 
			get 
            { 
                return __EnableValidation; 
            }
			set 
            {
                if (value != __EnableValidation)
                {
                    bool oldValue = __EnableValidation;
                    __EnableValidation = value;
                    this.RaisePropertyChangedEvent("EnableValidation", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private UserIdentityLookupAddins __UserIdentityLookupAddins;
		
		[XmlElement(Type=typeof(UserIdentityLookupAddins),ElementName="UserIdentityLookupAddins",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public UserIdentityLookupAddins UserIdentityLookupAddins
		{
			get
			{
				if (__UserIdentityLookupAddins == null) 
                {
                    __UserIdentityLookupAddins = new UserIdentityLookupAddins();		
                    this.RaisePropertyChangedEvent("UserIdentityLookupAddins", null, __UserIdentityLookupAddins);
                }
				return __UserIdentityLookupAddins;
			}
			set 
            {
                if (value != __UserIdentityLookupAddins)
                {
                    UserIdentityLookupAddins oldValue = __UserIdentityLookupAddins;
                    __UserIdentityLookupAddins = value;
                    this.RaisePropertyChangedEvent("UserIdentityLookupAddins", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private NotifyingCollection<UserMappings> __UserMappings;
		
		[XmlElement(Type=typeof(UserMappings),ElementName="UserMappings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<UserMappings> UserMappings
		{
			get
			{
				if (__UserMappings == null) 
                {
                    __UserMappings = new NotifyingCollection<UserMappings>();
                }
			    return __UserMappings;
            }
		}

		[XmlIgnore]
		private NotifyingCollection<DisplayNameMappings> __DisplayNameMappings;
		
		[XmlElement(Type=typeof(DisplayNameMappings),ElementName="DisplayNameMappings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<DisplayNameMappings> DisplayNameMappings
		{
			get
			{
				if (__DisplayNameMappings == null) 
                {
                    __DisplayNameMappings = new NotifyingCollection<DisplayNameMappings>();
                }
			    return __DisplayNameMappings;
            }
		}

		[XmlIgnore]
		private NotifyingCollection<AliasMappings> __AliasMappings;
		
		[XmlElement(Type=typeof(AliasMappings),ElementName="AliasMappings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<AliasMappings> AliasMappings
		{
			get
			{
				if (__AliasMappings == null) 
                {
                    __AliasMappings = new NotifyingCollection<AliasMappings>();
                }
			    return __AliasMappings;
            }
		}

		[XmlIgnore]
		private NotifyingCollection<DomainMappings> __DomainMappings;
		
		[XmlElement(Type=typeof(DomainMappings),ElementName="DomainMappings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<DomainMappings> DomainMappings
		{
			get
			{
				if (__DomainMappings == null) 
                {
                    __DomainMappings = new NotifyingCollection<DomainMappings>();
                }
			    return __DomainMappings;
            }
		}

		public UserIdentityMappings()
		{
		}
	}


	[XmlType(TypeName="UserIdentityLookupAddins"),Serializable]
	public partial class UserIdentityLookupAddins : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<string> __UserIdentityLookupAddin;
		
		[XmlElement(Type=typeof(string),ElementName="UserIdentityLookupAddin",IsNullable=false,Form=XmlSchemaForm.Qualified,DataType="normalizedString")]
		public NotifyingCollection<string> UserIdentityLookupAddin
		{
			get
			{
				if (__UserIdentityLookupAddin == null) 
                {
                    __UserIdentityLookupAddin = new NotifyingCollection<string>();
                }
			    return __UserIdentityLookupAddin;
            }
		}

		public UserIdentityLookupAddins()
		{
		}
	}


	[XmlType(TypeName="UserMappings"),Serializable]
	public partial class UserMappings : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum __DirectionOfMapping;
		
		[XmlAttribute(AttributeName="DirectionOfMapping",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum DirectionOfMapping
		{ 
			get 
            { 
                return __DirectionOfMapping; 
            }
			set 
            {
                if (value != __DirectionOfMapping)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum oldValue = __DirectionOfMapping;
                    __DirectionOfMapping = value;
                    this.RaisePropertyChangedEvent("DirectionOfMapping", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private NotifyingCollection<UserMapping> __UserMapping;
		
		[XmlElement(Type=typeof(UserMapping),ElementName="UserMapping",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<UserMapping> UserMapping
		{
			get
			{
				if (__UserMapping == null) 
                {
                    __UserMapping = new NotifyingCollection<UserMapping>();
                }
			    return __UserMapping;
            }
		}

		public UserMappings()
		{
		}
	}


	[XmlType(TypeName="UserMapping"),Serializable]
	public partial class UserMapping : ModelObject
	{

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.User __LeftUser;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.User),ElementName="LeftUser",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.User LeftUser
		{
			get
			{
				if (__LeftUser == null) 
                {
                    __LeftUser = new Microsoft.TeamFoundation.Migration.BusinessModel.User();		
                    this.RaisePropertyChangedEvent("LeftUser", null, __LeftUser);
                }
				return __LeftUser;
			}
			set 
            {
                if (value != __LeftUser)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.User oldValue = __LeftUser;
                    __LeftUser = value;
                    this.RaisePropertyChangedEvent("LeftUser", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.User __RightUser;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.User),ElementName="RightUser",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.User RightUser
		{
			get
			{
				if (__RightUser == null) 
                {
                    __RightUser = new Microsoft.TeamFoundation.Migration.BusinessModel.User();		
                    this.RaisePropertyChangedEvent("RightUser", null, __RightUser);
                }
				return __RightUser;
			}
			set 
            {
                if (value != __RightUser)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.User oldValue = __RightUser;
                    __RightUser = value;
                    this.RaisePropertyChangedEvent("RightUser", oldValue, value);
                }
            }
		}

		public UserMapping()
		{
		}
	}


	[XmlType(TypeName="DisplayNameMappings"),Serializable]
	public partial class DisplayNameMappings : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum __DirectionOfMapping;
		
		[XmlAttribute(AttributeName="DirectionOfMapping",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum DirectionOfMapping
		{ 
			get 
            { 
                return __DirectionOfMapping; 
            }
			set 
            {
                if (value != __DirectionOfMapping)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum oldValue = __DirectionOfMapping;
                    __DirectionOfMapping = value;
                    this.RaisePropertyChangedEvent("DirectionOfMapping", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private NotifyingCollection<DisplayNameMapping> __DisplayNameMapping;
		
		[XmlElement(Type=typeof(DisplayNameMapping),ElementName="DisplayNameMapping",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<DisplayNameMapping> DisplayNameMapping
		{
			get
			{
				if (__DisplayNameMapping == null) 
                {
                    __DisplayNameMapping = new NotifyingCollection<DisplayNameMapping>();
                }
			    return __DisplayNameMapping;
            }
		}

		public DisplayNameMappings()
		{
		}
	}


	[XmlType(TypeName="DisplayNameMapping"),Serializable]
	public partial class DisplayNameMapping : ModelObject
	{

		[XmlIgnore]
		private string __Left;
		
		[XmlAttribute(AttributeName="Left",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Left
		{ 
			get 
            { 
                return __Left; 
            }
			set 
            {
                if (value != __Left)
                {
                    string oldValue = __Left;
                    __Left = value;
                    this.RaisePropertyChangedEvent("Left", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __Right;
		
		[XmlAttribute(AttributeName="Right",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Right
		{ 
			get 
            { 
                return __Right; 
            }
			set 
            {
                if (value != __Right)
                {
                    string oldValue = __Right;
                    __Right = value;
                    this.RaisePropertyChangedEvent("Right", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules __MappingRule;
		
		[XmlAttribute(AttributeName="MappingRule",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules MappingRule
		{ 
			get 
            { 
                return __MappingRule; 
            }
			set 
            {
                if (value != __MappingRule)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules oldValue = __MappingRule;
                    __MappingRule = value;
                    this.RaisePropertyChangedEvent("MappingRule", oldValue, value);
                }
            }
		}

		public DisplayNameMapping()
		{
		}
	}


	[XmlType(TypeName="AliasMappings"),Serializable]
	public partial class AliasMappings : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum __DirectionOfMapping;
		
		[XmlAttribute(AttributeName="DirectionOfMapping",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum DirectionOfMapping
		{ 
			get 
            { 
                return __DirectionOfMapping; 
            }
			set 
            {
                if (value != __DirectionOfMapping)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum oldValue = __DirectionOfMapping;
                    __DirectionOfMapping = value;
                    this.RaisePropertyChangedEvent("DirectionOfMapping", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private NotifyingCollection<AliasMapping> __AliasMapping;
		
		[XmlElement(Type=typeof(AliasMapping),ElementName="AliasMapping",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<AliasMapping> AliasMapping
		{
			get
			{
				if (__AliasMapping == null) 
                {
                    __AliasMapping = new NotifyingCollection<AliasMapping>();
                }
			    return __AliasMapping;
            }
		}

		public AliasMappings()
		{
		}
	}


	[XmlType(TypeName="AliasMapping"),Serializable]
	public partial class AliasMapping : ModelObject
	{

		[XmlIgnore]
		private string __Left;
		
		[XmlAttribute(AttributeName="Left",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Left
		{ 
			get 
            { 
                return __Left; 
            }
			set 
            {
                if (value != __Left)
                {
                    string oldValue = __Left;
                    __Left = value;
                    this.RaisePropertyChangedEvent("Left", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __Right;
		
		[XmlAttribute(AttributeName="Right",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Right
		{ 
			get 
            { 
                return __Right; 
            }
			set 
            {
                if (value != __Right)
                {
                    string oldValue = __Right;
                    __Right = value;
                    this.RaisePropertyChangedEvent("Right", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules __MappingRule;
		
		[XmlAttribute(AttributeName="MappingRule",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules MappingRule
		{ 
			get 
            { 
                return __MappingRule; 
            }
			set 
            {
                if (value != __MappingRule)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules oldValue = __MappingRule;
                    __MappingRule = value;
                    this.RaisePropertyChangedEvent("MappingRule", oldValue, value);
                }
            }
		}

		public AliasMapping()
		{
		}
	}


	[XmlType(TypeName="DomainMappings"),Serializable]
	public partial class DomainMappings : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum __DirectionOfMapping;
		
		[XmlAttribute(AttributeName="DirectionOfMapping",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum DirectionOfMapping
		{ 
			get 
            { 
                return __DirectionOfMapping; 
            }
			set 
            {
                if (value != __DirectionOfMapping)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingDirectionEnum oldValue = __DirectionOfMapping;
                    __DirectionOfMapping = value;
                    this.RaisePropertyChangedEvent("DirectionOfMapping", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private NotifyingCollection<DomainMapping> __DomainMapping;
		
		[XmlElement(Type=typeof(DomainMapping),ElementName="DomainMapping",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<DomainMapping> DomainMapping
		{
			get
			{
				if (__DomainMapping == null) 
                {
                    __DomainMapping = new NotifyingCollection<DomainMapping>();
                }
			    return __DomainMapping;
            }
		}

		public DomainMappings()
		{
		}
	}


	[XmlType(TypeName="DomainMapping"),Serializable]
	public partial class DomainMapping : ModelObject
	{

		[XmlIgnore]
		private string __Left;
		
		[XmlAttribute(AttributeName="Left",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Left
		{ 
			get 
            { 
                return __Left; 
            }
			set 
            {
                if (value != __Left)
                {
                    string oldValue = __Left;
                    __Left = value;
                    this.RaisePropertyChangedEvent("Left", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __Right;
		
		[XmlAttribute(AttributeName="Right",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Right
		{ 
			get 
            { 
                return __Right; 
            }
			set 
            {
                if (value != __Right)
                {
                    string oldValue = __Right;
                    __Right = value;
                    this.RaisePropertyChangedEvent("Right", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules __MappingRule;
		
		[XmlAttribute(AttributeName="MappingRule",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules MappingRule
		{ 
			get 
            { 
                return __MappingRule; 
            }
			set 
            {
                if (value != __MappingRule)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.MappingRules oldValue = __MappingRule;
                    __MappingRule = value;
                    this.RaisePropertyChangedEvent("MappingRule", oldValue, value);
                }
            }
		}

		public DomainMapping()
		{
		}
	}


	[XmlType(TypeName="ErrorManagement"),Serializable]
	public partial class ErrorManagement : ModelObject
	{

		[XmlIgnore]
		private ErrorRouters __ErrorRouters;
		
		[XmlElement(Type=typeof(ErrorRouters),ElementName="ErrorRouters",IsNullable=true,Form=XmlSchemaForm.Qualified)]
		public ErrorRouters ErrorRouters
		{
			get
			{
				if (__ErrorRouters == null) 
                {
                    __ErrorRouters = new ErrorRouters();		
                    this.RaisePropertyChangedEvent("ErrorRouters", null, __ErrorRouters);
                }
				return __ErrorRouters;
			}
			set 
            {
                if (value != __ErrorRouters)
                {
                    ErrorRouters oldValue = __ErrorRouters;
                    __ErrorRouters = value;
                    this.RaisePropertyChangedEvent("ErrorRouters", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private ReportingSettings __ReportingSettings;
		
		[XmlElement(Type=typeof(ReportingSettings),ElementName="ReportingSettings",IsNullable=true,Form=XmlSchemaForm.Qualified)]
		public ReportingSettings ReportingSettings
		{
			get
			{
				if (__ReportingSettings == null) 
                {
                    __ReportingSettings = new ReportingSettings();		
                    this.RaisePropertyChangedEvent("ReportingSettings", null, __ReportingSettings);
                }
				return __ReportingSettings;
			}
			set 
            {
                if (value != __ReportingSettings)
                {
                    ReportingSettings oldValue = __ReportingSettings;
                    __ReportingSettings = value;
                    this.RaisePropertyChangedEvent("ReportingSettings", oldValue, value);
                }
            }
		}

		public ErrorManagement()
		{
		}
	}


	[XmlType(TypeName="ErrorRouters"),Serializable]
	public partial class ErrorRouters : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<ErrorRouter> __ErrorRouter;
		
		[XmlElement(Type=typeof(ErrorRouter),ElementName="ErrorRouter",IsNullable=true,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<ErrorRouter> ErrorRouter
		{
			get
			{
				if (__ErrorRouter == null) 
                {
                    __ErrorRouter = new NotifyingCollection<ErrorRouter>();
                }
			    return __ErrorRouter;
            }
		}

		public ErrorRouters()
		{
		}
	}


	[XmlType(TypeName="ErrorRouter"),Serializable]
	public partial class ErrorRouter : ModelObject
	{

		[XmlIgnore]
		private Signature __Signature;
		
		[XmlElement(Type=typeof(Signature),ElementName="Signature",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Signature Signature
		{
			get
			{
				if (__Signature == null) 
                {
                    __Signature = new Signature();		
                    this.RaisePropertyChangedEvent("Signature", null, __Signature);
                }
				return __Signature;
			}
			set 
            {
                if (value != __Signature)
                {
                    Signature oldValue = __Signature;
                    __Signature = value;
                    this.RaisePropertyChangedEvent("Signature", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Policy __Policy;
		
		[XmlElement(Type=typeof(Policy),ElementName="Policy",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Policy Policy
		{
			get
			{
				if (__Policy == null) 
                {
                    __Policy = new Policy();		
                    this.RaisePropertyChangedEvent("Policy", null, __Policy);
                }
				return __Policy;
			}
			set 
            {
                if (value != __Policy)
                {
                    Policy oldValue = __Policy;
                    __Policy = value;
                    this.RaisePropertyChangedEvent("Policy", oldValue, value);
                }
            }
		}

		public ErrorRouter()
		{
		}
	}


	[XmlType(TypeName="Signature"),Serializable]
	public partial class Signature : ModelObject
	{

		[XmlIgnore]
		private string __Exception;
		
		[XmlAttribute(AttributeName="Exception",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Exception
		{ 
			get 
            { 
                return __Exception; 
            }
			set 
            {
                if (value != __Exception)
                {
                    string oldValue = __Exception;
                    __Exception = value;
                    this.RaisePropertyChangedEvent("Exception", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __Message;
		
		[XmlAttribute(AttributeName="Message",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string Message
		{ 
			get 
            { 
                return __Message; 
            }
			set 
            {
                if (value != __Message)
                {
                    string oldValue = __Message;
                    __Message = value;
                    this.RaisePropertyChangedEvent("Message", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __InnerException;
		
		[XmlAttribute(AttributeName="InnerException",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string InnerException
		{ 
			get 
            { 
                return __InnerException; 
            }
			set 
            {
                if (value != __InnerException)
                {
                    string oldValue = __InnerException;
                    __InnerException = value;
                    this.RaisePropertyChangedEvent("InnerException", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __InnerMessage;
		
		[XmlAttribute(AttributeName="InnerMessage",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string InnerMessage
		{ 
			get 
            { 
                return __InnerMessage; 
            }
			set 
            {
                if (value != __InnerMessage)
                {
                    string oldValue = __InnerMessage;
                    __InnerMessage = value;
                    this.RaisePropertyChangedEvent("InnerMessage", oldValue, value);
                }
            }
		}

		public Signature()
		{
		}
	}


	[XmlType(TypeName="Policy"),Serializable]
	public partial class Policy : ModelObject
	{

		[XmlElement(ElementName="OccurrenceCount",IsNullable=false,Form=XmlSchemaForm.Qualified,DataType="int")]
		public int __OccurrenceCount;
		
		[XmlIgnore]
		public bool __OccurrenceCountSpecified;
		
		[XmlIgnore]
		public int OccurrenceCount
		{ 
			get { return __OccurrenceCount; }
			set { __OccurrenceCount = value; __OccurrenceCountSpecified = true; }
		}

		public Policy()
		{
		}
	}


	[XmlType(TypeName="ReportingSettings"),Serializable]
	public partial class ReportingSettings : ModelObject
	{

		[XmlElement(ElementName="ReportingLevel",IsNullable=false,Form=XmlSchemaForm.Qualified,DataType="int")]
		public int __ReportingLevel;
		
		[XmlIgnore]
		public bool __ReportingLevelSpecified;
		
		[XmlIgnore]
		public int ReportingLevel
		{ 
			get { return __ReportingLevel; }
			set { __ReportingLevel = value; __ReportingLevelSpecified = true; }
		}

		[XmlElement(ElementName="EnableDebugAssertion",IsNullable=false,Form=XmlSchemaForm.Qualified,DataType="boolean")]
		public bool __EnableDebugAssertion;
		
		[XmlIgnore]
		public bool __EnableDebugAssertionSpecified;
		
		[XmlIgnore]
		public bool EnableDebugAssertion
		{ 
			get { return __EnableDebugAssertion; }
			set { __EnableDebugAssertion = value; __EnableDebugAssertionSpecified = true; }
		}

		public ReportingSettings()
		{
		}
	}


	[XmlType(TypeName="MigrationSourcesElement"),Serializable]
	public partial class MigrationSourcesElement : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<MigrationSource> __MigrationSource;
		
		[XmlElement(Type=typeof(MigrationSource),ElementName="MigrationSource",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<MigrationSource> MigrationSource
		{
			get
			{
				if (__MigrationSource == null) 
                {
                    __MigrationSource = new NotifyingCollection<MigrationSource>();
                }
			    return __MigrationSource;
            }
		}

		public MigrationSourcesElement()
		{
		}
	}


	[XmlType(TypeName="MigrationSource"),Serializable]
	public partial class MigrationSource : ModelObject
	{

		[XmlIgnore]
		private string __InternalUniqueId;
		
		[XmlAttribute(AttributeName="InternalUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string InternalUniqueId
		{ 
			get 
            { 
                return __InternalUniqueId; 
            }
			set 
            {
                if (value != __InternalUniqueId)
                {
                    string oldValue = __InternalUniqueId;
                    __InternalUniqueId = value;
                    this.RaisePropertyChangedEvent("InternalUniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __ServerIdentifier;
		
		[XmlAttribute(AttributeName="ServerIdentifier",Form=XmlSchemaForm.Unqualified,DataType="string")]
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

		[XmlIgnore]
		private string __ServerUrl;
		
		[XmlAttribute(AttributeName="ServerUrl",Form=XmlSchemaForm.Unqualified,DataType="anyURI")]
		public string ServerUrl
		{ 
			get 
            { 
                return __ServerUrl; 
            }
			set 
            {
                if (value != __ServerUrl)
                {
                    string oldValue = __ServerUrl;
                    __ServerUrl = value;
                    this.RaisePropertyChangedEvent("ServerUrl", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __SourceIdentifier;
		
		[XmlAttribute(AttributeName="SourceIdentifier",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string SourceIdentifier
		{ 
			get 
            { 
                return __SourceIdentifier; 
            }
			set 
            {
                if (value != __SourceIdentifier)
                {
                    string oldValue = __SourceIdentifier;
                    __SourceIdentifier = value;
                    this.RaisePropertyChangedEvent("SourceIdentifier", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __ProviderReferenceName;
		
		[XmlAttribute(AttributeName="ProviderReferenceName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string ProviderReferenceName
		{ 
			get 
            { 
                return __ProviderReferenceName; 
            }
			set 
            {
                if (value != __ProviderReferenceName)
                {
                    string oldValue = __ProviderReferenceName;
                    __ProviderReferenceName = value;
                    this.RaisePropertyChangedEvent("ProviderReferenceName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __EndpointSystemName;
		
		[XmlAttribute(AttributeName="EndpointSystemName",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string EndpointSystemName
		{ 
			get 
            { 
                return __EndpointSystemName; 
            }
			set 
            {
                if (value != __EndpointSystemName)
                {
                    string oldValue = __EndpointSystemName;
                    __EndpointSystemName = value;
                    this.RaisePropertyChangedEvent("EndpointSystemName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Settings __Settings;
		
		[XmlElement(Type=typeof(Settings),ElementName="Settings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
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

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement __CustomSettings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement),ElementName="CustomSettings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement CustomSettings
		{
			get
			{
				if (__CustomSettings == null) 
                {
                    __CustomSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement();		
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
				return __CustomSettings;
			}
			set 
            {
                if (value != __CustomSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private StoredCredential __StoredCredential;
		
		[XmlElement(Type=typeof(StoredCredential),ElementName="StoredCredential",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public StoredCredential StoredCredential
		{
			get
			{
				if (__StoredCredential == null) 
                {
                    __StoredCredential = new StoredCredential();		
                    this.RaisePropertyChangedEvent("StoredCredential", null, __StoredCredential);
                }
				return __StoredCredential;
			}
			set 
            {
                if (value != __StoredCredential)
                {
                    StoredCredential oldValue = __StoredCredential;
                    __StoredCredential = value;
                    this.RaisePropertyChangedEvent("StoredCredential", oldValue, value);
                }
            }
		}

		public MigrationSource()
		{
		}
	}


	[XmlType(TypeName="Settings"),Serializable]
	public partial class Settings : ModelObject
	{

		[XmlIgnore]
		private SettingsAddins __Addins;
		
		[XmlElement(Type=typeof(SettingsAddins),ElementName="Addins",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public SettingsAddins Addins
		{
			get
			{
				if (__Addins == null) 
                {
                    __Addins = new SettingsAddins();		
                    this.RaisePropertyChangedEvent("Addins", null, __Addins);
                }
				return __Addins;
			}
			set 
            {
                if (value != __Addins)
                {
                    SettingsAddins oldValue = __Addins;
                    __Addins = value;
                    this.RaisePropertyChangedEvent("Addins", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private UserIdentityLookup __UserIdentityLookup;
		
		[XmlElement(Type=typeof(UserIdentityLookup),ElementName="UserIdentityLookup",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public UserIdentityLookup UserIdentityLookup
		{
			get
			{
				if (__UserIdentityLookup == null) 
                {
                    __UserIdentityLookup = new UserIdentityLookup();		
                    this.RaisePropertyChangedEvent("UserIdentityLookup", null, __UserIdentityLookup);
                }
				return __UserIdentityLookup;
			}
			set 
            {
                if (value != __UserIdentityLookup)
                {
                    UserIdentityLookup oldValue = __UserIdentityLookup;
                    __UserIdentityLookup = value;
                    this.RaisePropertyChangedEvent("UserIdentityLookup", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private DefaultUserIdProperty __DefaultUserIdProperty;
		
		[XmlElement(Type=typeof(DefaultUserIdProperty),ElementName="DefaultUserIdProperty",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public DefaultUserIdProperty DefaultUserIdProperty
		{
			get
			{
				if (__DefaultUserIdProperty == null) 
                {
                    __DefaultUserIdProperty = new DefaultUserIdProperty();		
                    this.RaisePropertyChangedEvent("DefaultUserIdProperty", null, __DefaultUserIdProperty);
                }
				return __DefaultUserIdProperty;
			}
			set 
            {
                if (value != __DefaultUserIdProperty)
                {
                    DefaultUserIdProperty oldValue = __DefaultUserIdProperty;
                    __DefaultUserIdProperty = value;
                    this.RaisePropertyChangedEvent("DefaultUserIdProperty", oldValue, value);
                }
            }
		}

		public Settings()
		{
		}
	}


	[XmlType(TypeName="SettingsAddins"),Serializable]
	public partial class SettingsAddins : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement> __Addin;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement),ElementName="Addin",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement> Addin
		{
			get
			{
				if (__Addin == null) 
                {
                    __Addin = new NotifyingCollection<Microsoft.TeamFoundation.Migration.BusinessModel.AddinElement>();
                }
			    return __Addin;
            }
		}

		public SettingsAddins()
		{
		}
	}


	[XmlType(TypeName="UserIdentityLookup"),Serializable]
	public partial class UserIdentityLookup : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<LookupAddin> __LookupAddin;
		
		[XmlElement(Type=typeof(LookupAddin),ElementName="LookupAddin",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<LookupAddin> LookupAddin
		{
			get
			{
				if (__LookupAddin == null) 
                {
                    __LookupAddin = new NotifyingCollection<LookupAddin>();
                }
			    return __LookupAddin;
            }
		}

		public UserIdentityLookup()
		{
		}
	}


	[XmlType(TypeName="LookupAddin"),Serializable]
	public partial class LookupAddin : ModelObject
	{

		[XmlIgnore]
		private int __Precedence;
		
		[XmlAttribute(AttributeName="Precedence",Form=XmlSchemaForm.Unqualified,DataType="int")]
		public int Precedence
		{ 
			get 
            { 
                return __Precedence; 
            }
			set 
            {
                if (value != __Precedence)
                {
                    int oldValue = __Precedence;
                    __Precedence = value;
                    this.RaisePropertyChangedEvent("Precedence", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __ReferenceName;
		
		[XmlAttribute(AttributeName="ReferenceName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
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

		public LookupAddin()
		{
		}
	}


	[XmlType(TypeName="DefaultUserIdProperty"),Serializable]
	public partial class DefaultUserIdProperty : ModelObject
	{

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.UserIdPropertyNameEnum __UserIdPropertyName;
		
		[XmlAttribute(AttributeName="UserIdPropertyName",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.UserIdPropertyNameEnum UserIdPropertyName
		{ 
			get 
            { 
                return __UserIdPropertyName; 
            }
			set 
            {
                if (value != __UserIdPropertyName)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.UserIdPropertyNameEnum oldValue = __UserIdPropertyName;
                    __UserIdPropertyName = value;
                    this.RaisePropertyChangedEvent("UserIdPropertyName", oldValue, value);
                }
            }
		}

		public DefaultUserIdProperty()
		{
		}
	}


	[XmlType(TypeName="StoredCredential"),Serializable]
	public partial class StoredCredential : ModelObject
	{

		[XmlIgnore]
		private string __CredentialString;
		
		[XmlAttribute(AttributeName="CredentialString",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string CredentialString
		{ 
			get 
            { 
                return __CredentialString; 
            }
			set 
            {
                if (value != __CredentialString)
                {
                    string oldValue = __CredentialString;
                    __CredentialString = value;
                    this.RaisePropertyChangedEvent("CredentialString", oldValue, value);
                }
            }
		}

		public StoredCredential()
		{
		}
	}


	[XmlType(TypeName="LinkingElement"),Serializable]
	public partial class LinkingElement : ModelObject
	{

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement __CustomSettings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement),ElementName="CustomSettings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement CustomSettings
		{
			get
			{
				if (__CustomSettings == null) 
                {
                    __CustomSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement();		
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
				return __CustomSettings;
			}
			set 
            {
                if (value != __CustomSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.LinkTypeMappingsElement __LinkTypeMappings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.LinkTypeMappingsElement),ElementName="LinkTypeMappings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.LinkTypeMappingsElement LinkTypeMappings
		{
			get
			{
				if (__LinkTypeMappings == null) 
                {
                    __LinkTypeMappings = new Microsoft.TeamFoundation.Migration.BusinessModel.LinkTypeMappingsElement();		
                    this.RaisePropertyChangedEvent("LinkTypeMappings", null, __LinkTypeMappings);
                }
				return __LinkTypeMappings;
			}
			set 
            {
                if (value != __LinkTypeMappings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.LinkTypeMappingsElement oldValue = __LinkTypeMappings;
                    __LinkTypeMappings = value;
                    this.RaisePropertyChangedEvent("LinkTypeMappings", oldValue, value);
                }
            }
		}

		public LinkingElement()
		{
		}
	}


	[XmlType(TypeName="SessionsElement"),Serializable]
	public partial class SessionsElement : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<Session> __Session;
		
		[XmlElement(Type=typeof(Session),ElementName="Session",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<Session> Session
		{
			get
			{
				if (__Session == null) 
                {
                    __Session = new NotifyingCollection<Session>();
                }
			    return __Session;
            }
		}

		public SessionsElement()
		{
		}
	}


	[XmlType(TypeName="Session"),Serializable]
	public partial class Session : ModelObject
	{

		[XmlIgnore]
		private string __SessionUniqueId;
		
		[XmlAttribute(AttributeName="SessionUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string SessionUniqueId
		{ 
			get 
            { 
                return __SessionUniqueId; 
            }
			set 
            {
                if (value != __SessionUniqueId)
                {
                    string oldValue = __SessionUniqueId;
                    __SessionUniqueId = value;
                    this.RaisePropertyChangedEvent("SessionUniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		[XmlAttribute(AttributeName="CreationTime",Form=XmlSchemaForm.Unqualified,DataType="dateTime")]
		public System.DateTime __CreationTime;
		
		[XmlIgnore]
		public bool __CreationTimeSpecified;
		
		[XmlIgnore]
		public System.DateTime CreationTime
		{ 
			get { return __CreationTime; }
			set { __CreationTime = value; __CreationTimeSpecified = true; }
		}
		
		[XmlIgnore]
		public System.DateTime CreationTimeUtc
		{ 
			get { return __CreationTime.ToUniversalTime(); }
			set { __CreationTime = value.ToLocalTime(); __CreationTimeSpecified = true; }
		}

		[XmlIgnore]
		private string __Creator;
		
		[XmlAttribute(AttributeName="Creator",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string Creator
		{ 
			get 
            { 
                return __Creator; 
            }
			set 
            {
                if (value != __Creator)
                {
                    string oldValue = __Creator;
                    __Creator = value;
                    this.RaisePropertyChangedEvent("Creator", oldValue, value);
                }
            }
		}

		[XmlAttribute(AttributeName="DeprecationTime",Form=XmlSchemaForm.Unqualified,DataType="dateTime")]
		public System.DateTime __DeprecationTime;
		
		[XmlIgnore]
		public bool __DeprecationTimeSpecified;
		
		[XmlIgnore]
		public System.DateTime DeprecationTime
		{ 
			get { return __DeprecationTime; }
			set { __DeprecationTime = value; __DeprecationTimeSpecified = true; }
		}
		
		[XmlIgnore]
		public System.DateTime DeprecationTimeUtc
		{ 
			get { return __DeprecationTime.ToUniversalTime(); }
			set { __DeprecationTime = value.ToLocalTime(); __DeprecationTimeSpecified = true; }
		}

		[XmlIgnore]
		private string __LeftMigrationSourceUniqueId;
		
		[XmlAttribute(AttributeName="LeftMigrationSourceUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string LeftMigrationSourceUniqueId
		{ 
			get 
            { 
                return __LeftMigrationSourceUniqueId; 
            }
			set 
            {
                if (value != __LeftMigrationSourceUniqueId)
                {
                    string oldValue = __LeftMigrationSourceUniqueId;
                    __LeftMigrationSourceUniqueId = value;
                    this.RaisePropertyChangedEvent("LeftMigrationSourceUniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __RightMigrationSourceUniqueId;
		
		[XmlAttribute(AttributeName="RightMigrationSourceUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string RightMigrationSourceUniqueId
		{ 
			get 
            { 
                return __RightMigrationSourceUniqueId; 
            }
			set 
            {
                if (value != __RightMigrationSourceUniqueId)
                {
                    string oldValue = __RightMigrationSourceUniqueId;
                    __RightMigrationSourceUniqueId = value;
                    this.RaisePropertyChangedEvent("RightMigrationSourceUniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.SessionTypeEnum __SessionType;
		
		[XmlAttribute(AttributeName="SessionType",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.SessionTypeEnum SessionType
		{ 
			get 
            { 
                return __SessionType; 
            }
			set 
            {
                if (value != __SessionType)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.SessionTypeEnum oldValue = __SessionType;
                    __SessionType = value;
                    this.RaisePropertyChangedEvent("SessionType", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.EventSinksElement __EventSinks;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.EventSinksElement),ElementName="EventSinks",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.EventSinksElement EventSinks
		{
			get
			{
				if (__EventSinks == null) 
                {
                    __EventSinks = new Microsoft.TeamFoundation.Migration.BusinessModel.EventSinksElement();		
                    this.RaisePropertyChangedEvent("EventSinks", null, __EventSinks);
                }
				return __EventSinks;
			}
			set 
            {
                if (value != __EventSinks)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.EventSinksElement oldValue = __EventSinks;
                    __EventSinks = value;
                    this.RaisePropertyChangedEvent("EventSinks", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.GenericSettingsElement __CustomSettings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.GenericSettingsElement),ElementName="CustomSettings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.GenericSettingsElement CustomSettings
		{
			get
			{
				if (__CustomSettings == null) 
                {
                    __CustomSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.GenericSettingsElement();		
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
				return __CustomSettings;
			}
			set 
            {
                if (value != __CustomSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.GenericSettingsElement oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.FiltersElement __Filters;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.FiltersElement),ElementName="Filters",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.FiltersElement Filters
		{
			get
			{
				if (__Filters == null) 
                {
                    __Filters = new Microsoft.TeamFoundation.Migration.BusinessModel.FiltersElement();		
                    this.RaisePropertyChangedEvent("Filters", null, __Filters);
                }
				return __Filters;
			}
			set 
            {
                if (value != __Filters)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.FiltersElement oldValue = __Filters;
                    __Filters = value;
                    this.RaisePropertyChangedEvent("Filters", oldValue, value);
                }
            }
		}

		public Session()
		{
			__CreationTime = System.DateTime.Now;
			__DeprecationTime = System.DateTime.Now;
		}
	}


	[XmlType(TypeName="FiltersElement"),Serializable]
	public partial class FiltersElement : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<FilterPair> __FilterPair;
		
		[XmlElement(Type=typeof(FilterPair),ElementName="FilterPair",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<FilterPair> FilterPair
		{
			get
			{
				if (__FilterPair == null) 
                {
                    __FilterPair = new NotifyingCollection<FilterPair>();
                }
			    return __FilterPair;
            }
		}

		public FiltersElement()
		{
		}
	}


	[XmlType(TypeName="FilterPair"),Serializable]
	public partial class FilterPair : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private bool __Neglect;
		
		[XmlAttribute(AttributeName="Neglect",Form=XmlSchemaForm.Unqualified,DataType="boolean")]
		public bool Neglect
		{ 
			get 
            { 
                return __Neglect; 
            }
			set 
            {
                if (value != __Neglect)
                {
                    bool oldValue = __Neglect;
                    __Neglect = value;
                    this.RaisePropertyChangedEvent("Neglect", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private NotifyingCollection<FilterItem> __FilterItem;
		
		[XmlElement(Type=typeof(FilterItem),ElementName="FilterItem",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<FilterItem> FilterItem
		{
			get
			{
				if (__FilterItem == null) 
                {
                    __FilterItem = new NotifyingCollection<FilterItem>();
                }
			    return __FilterItem;
            }
		}

		public FilterPair()
		{
		}
	}


	[XmlType(TypeName="FilterItem"),Serializable]
	public partial class FilterItem : ModelObject
	{

		[XmlIgnore]
		private string __MigrationSourceUniqueId;
		
		[XmlAttribute(AttributeName="MigrationSourceUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string MigrationSourceUniqueId
		{ 
			get 
            { 
                return __MigrationSourceUniqueId; 
            }
			set 
            {
                if (value != __MigrationSourceUniqueId)
                {
                    string oldValue = __MigrationSourceUniqueId;
                    __MigrationSourceUniqueId = value;
                    this.RaisePropertyChangedEvent("MigrationSourceUniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __FilterString;
		
		[XmlAttribute(AttributeName="FilterString",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string FilterString
		{ 
			get 
            { 
                return __FilterString; 
            }
			set 
            {
                if (value != __FilterString)
                {
                    string oldValue = __FilterString;
                    __FilterString = value;
                    this.RaisePropertyChangedEvent("FilterString", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __SnapshotStartPoint;
		
		[XmlAttribute(AttributeName="SnapshotStartPoint",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string SnapshotStartPoint
		{ 
			get 
            { 
                return __SnapshotStartPoint; 
            }
			set 
            {
                if (value != __SnapshotStartPoint)
                {
                    string oldValue = __SnapshotStartPoint;
                    __SnapshotStartPoint = value;
                    this.RaisePropertyChangedEvent("SnapshotStartPoint", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __PeerSnapshotStartPoint;
		
		[XmlAttribute(AttributeName="PeerSnapshotStartPoint",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string PeerSnapshotStartPoint
		{ 
			get 
            { 
                return __PeerSnapshotStartPoint; 
            }
			set 
            {
                if (value != __PeerSnapshotStartPoint)
                {
                    string oldValue = __PeerSnapshotStartPoint;
                    __PeerSnapshotStartPoint = value;
                    this.RaisePropertyChangedEvent("PeerSnapshotStartPoint", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __MergeScope;
		
		[XmlAttribute(AttributeName="MergeScope",Form=XmlSchemaForm.Unqualified,DataType="string")]
		public string MergeScope
		{ 
			get 
            { 
                return __MergeScope; 
            }
			set 
            {
                if (value != __MergeScope)
                {
                    string oldValue = __MergeScope;
                    __MergeScope = value;
                    this.RaisePropertyChangedEvent("MergeScope", oldValue, value);
                }
            }
		}

		public FilterItem()
		{
		}
	}


	[XmlType(TypeName="PropertyBagElement"),Serializable]
	public partial class PropertyBagElement : ModelObject
	{

		[XmlIgnore]
		private string __Name;
		
		[XmlAttribute(AttributeName="Name",Form=XmlSchemaForm.Unqualified,DataType="NCName")]
		public string Name
		{ 
			get 
            { 
                return __Name; 
            }
			set 
            {
                if (value != __Name)
                {
                    string oldValue = __Name;
                    __Name = value;
                    this.RaisePropertyChangedEvent("Name", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __Value;
		
		[XmlAttribute(AttributeName="Value",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string @Value
		{ 
			get 
            { 
                return __Value; 
            }
			set 
            {
                if (value != __Value)
                {
                    string oldValue = __Value;
                    __Value = value;
                    this.RaisePropertyChangedEvent("@Value", oldValue, value);
                }
            }
		}

		public PropertyBagElement()
		{
		}
	}


	[XmlType(TypeName="EventSinksElement"),Serializable]
	public partial class EventSinksElement : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<EventSink> __EventSink;
		
		[XmlElement(Type=typeof(EventSink),ElementName="EventSink",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<EventSink> EventSink
		{
			get
			{
				if (__EventSink == null) 
                {
                    __EventSink = new NotifyingCollection<EventSink>();
                }
			    return __EventSink;
            }
		}

		public EventSinksElement()
		{
		}
	}


	[XmlType(TypeName="EventSink"),Serializable]
	public partial class EventSink : ModelObject
	{

		[XmlIgnore]
		private string __ProviderReferenceName;
		
		[XmlAttribute(AttributeName="ProviderReferenceName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string ProviderReferenceName
		{ 
			get 
            { 
                return __ProviderReferenceName; 
            }
			set 
            {
                if (value != __ProviderReferenceName)
                {
                    string oldValue = __ProviderReferenceName;
                    __ProviderReferenceName = value;
                    this.RaisePropertyChangedEvent("ProviderReferenceName", oldValue, value);
                }
            }
		}

		[XmlAttribute(AttributeName="CreationTime",Form=XmlSchemaForm.Unqualified,DataType="dateTime")]
		public System.DateTime __CreationTime;
		
		[XmlIgnore]
		public bool __CreationTimeSpecified;
		
		[XmlIgnore]
		public System.DateTime CreationTime
		{ 
			get { return __CreationTime; }
			set { __CreationTime = value; __CreationTimeSpecified = true; }
		}
		
		[XmlIgnore]
		public System.DateTime CreationTimeUtc
		{ 
			get { return __CreationTime.ToUniversalTime(); }
			set { __CreationTime = value.ToLocalTime(); __CreationTimeSpecified = true; }
		}

		[XmlIgnore]
		private string __FriendlyName;
		
		[XmlAttribute(AttributeName="FriendlyName",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string FriendlyName
		{ 
			get 
            { 
                return __FriendlyName; 
            }
			set 
            {
                if (value != __FriendlyName)
                {
                    string oldValue = __FriendlyName;
                    __FriendlyName = value;
                    this.RaisePropertyChangedEvent("FriendlyName", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement __CustomSettings;
		
		[XmlElement(Type=typeof(Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement),ElementName="CustomSettings",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement CustomSettings
		{
			get
			{
				if (__CustomSettings == null) 
                {
                    __CustomSettings = new Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement();		
                    this.RaisePropertyChangedEvent("CustomSettings", null, __CustomSettings);
                }
				return __CustomSettings;
			}
			set 
            {
                if (value != __CustomSettings)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.CustomSettingsElement oldValue = __CustomSettings;
                    __CustomSettings = value;
                    this.RaisePropertyChangedEvent("CustomSettings", oldValue, value);
                }
            }
		}

		public EventSink()
		{
			__CreationTime = System.DateTime.Now;
		}
	}


	[XmlType(TypeName="CustomSettingsElement"),Serializable]
	public partial class CustomSettingsElement : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<CustomSetting> __CustomSetting;
		
		[XmlElement(Type=typeof(CustomSetting),ElementName="CustomSetting",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<CustomSetting> CustomSetting
		{
			get
			{
				if (__CustomSetting == null) 
                {
                    __CustomSetting = new NotifyingCollection<CustomSetting>();
                }
			    return __CustomSetting;
            }
		}

		public CustomSettingsElement()
		{
		}
	}


	[XmlType(TypeName="CustomSetting"),Serializable]
	public partial class CustomSetting : ModelObject
	{

		[XmlIgnore]
		private string __SettingKey;
		
		[XmlAttribute(AttributeName="SettingKey",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
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
		
		[XmlAttribute(AttributeName="SettingValue",Form=XmlSchemaForm.Unqualified,DataType="string")]
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

		public CustomSetting()
		{
		}
	}


	[XmlType(TypeName="GenericSettingsElement"),Serializable]
	public partial class GenericSettingsElement : ModelObject
	{

		[XmlIgnore]
		private SettingXml __SettingXml;
		
		[XmlElement(Type=typeof(SettingXml),ElementName="SettingXml",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public SettingXml SettingXml
		{
			get
			{
				if (__SettingXml == null) 
                {
                    __SettingXml = new SettingXml();		
                    this.RaisePropertyChangedEvent("SettingXml", null, __SettingXml);
                }
				return __SettingXml;
			}
			set 
            {
                if (value != __SettingXml)
                {
                    SettingXml oldValue = __SettingXml;
                    __SettingXml = value;
                    this.RaisePropertyChangedEvent("SettingXml", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private SettingXmlSchema __SettingXmlSchema;
		
		[XmlElement(Type=typeof(SettingXmlSchema),ElementName="SettingXmlSchema",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public SettingXmlSchema SettingXmlSchema
		{
			get
			{
				if (__SettingXmlSchema == null) 
                {
                    __SettingXmlSchema = new SettingXmlSchema();		
                    this.RaisePropertyChangedEvent("SettingXmlSchema", null, __SettingXmlSchema);
                }
				return __SettingXmlSchema;
			}
			set 
            {
                if (value != __SettingXmlSchema)
                {
                    SettingXmlSchema oldValue = __SettingXmlSchema;
                    __SettingXmlSchema = value;
                    this.RaisePropertyChangedEvent("SettingXmlSchema", oldValue, value);
                }
            }
		}

		public GenericSettingsElement()
		{
		}
	}


	[XmlType(TypeName="SettingXml"),Serializable]
	public partial class SettingXml : ModelObject
	{

		[XmlAnyElement()]
		public System.Xml.XmlElement[] Any;

		public SettingXml()
		{
		}
	}


	[XmlType(TypeName="SettingXmlSchema"),Serializable]
	public partial class SettingXmlSchema : ModelObject
	{

		[XmlAnyElement()]
		public System.Xml.XmlElement[] Any;

		public SettingXmlSchema()
		{
		}
	}


	[XmlType(TypeName="LinkTypeMappingsElement"),Serializable]
	public partial class LinkTypeMappingsElement : ModelObject
	{
		//sClassEnumerabilityTemplate


		[XmlIgnore]
		private NotifyingCollection<LinkTypeMapping> __LinkTypeMapping;
		
		[XmlElement(Type=typeof(LinkTypeMapping),ElementName="LinkTypeMapping",IsNullable=false,Form=XmlSchemaForm.Qualified)]
		public NotifyingCollection<LinkTypeMapping> LinkTypeMapping
		{
			get
			{
				if (__LinkTypeMapping == null) 
                {
                    __LinkTypeMapping = new NotifyingCollection<LinkTypeMapping>();
                }
			    return __LinkTypeMapping;
            }
		}

		public LinkTypeMappingsElement()
		{
		}
	}


	[XmlType(TypeName="LinkTypeMapping"),Serializable]
	public partial class LinkTypeMapping : ModelObject
	{

		[XmlIgnore]
		private string __LeftMigrationSourceUniqueId;
		
		[XmlAttribute(AttributeName="LeftMigrationSourceUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string LeftMigrationSourceUniqueId
		{ 
			get 
            { 
                return __LeftMigrationSourceUniqueId; 
            }
			set 
            {
                if (value != __LeftMigrationSourceUniqueId)
                {
                    string oldValue = __LeftMigrationSourceUniqueId;
                    __LeftMigrationSourceUniqueId = value;
                    this.RaisePropertyChangedEvent("LeftMigrationSourceUniqueId", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __LeftLinkType;
		
		[XmlAttribute(AttributeName="LeftLinkType",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string LeftLinkType
		{ 
			get 
            { 
                return __LeftLinkType; 
            }
			set 
            {
                if (value != __LeftLinkType)
                {
                    string oldValue = __LeftLinkType;
                    __LeftLinkType = value;
                    this.RaisePropertyChangedEvent("LeftLinkType", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __RightLinkType;
		
		[XmlAttribute(AttributeName="RightLinkType",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string RightLinkType
		{ 
			get 
            { 
                return __RightLinkType; 
            }
			set 
            {
                if (value != __RightLinkType)
                {
                    string oldValue = __RightLinkType;
                    __RightLinkType = value;
                    this.RaisePropertyChangedEvent("RightLinkType", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private string __RightMigrationSourceUniqueId;
		
		[XmlAttribute(AttributeName="RightMigrationSourceUniqueId",Form=XmlSchemaForm.Unqualified,DataType="normalizedString")]
		public string RightMigrationSourceUniqueId
		{ 
			get 
            { 
                return __RightMigrationSourceUniqueId; 
            }
			set 
            {
                if (value != __RightMigrationSourceUniqueId)
                {
                    string oldValue = __RightMigrationSourceUniqueId;
                    __RightMigrationSourceUniqueId = value;
                    this.RaisePropertyChangedEvent("RightMigrationSourceUniqueId", oldValue, value);
                }
            }
		}

		public LinkTypeMapping()
		{
		}
	}


	[XmlType(TypeName="WorkFlowType"),Serializable]
	public partial class WorkFlowType : ModelObject
	{

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.Frequency __Frequency;
		
		[XmlAttribute(AttributeName="Frequency",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.Frequency Frequency
		{ 
			get 
            { 
                return __Frequency; 
            }
			set 
            {
                if (value != __Frequency)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.Frequency oldValue = __Frequency;
                    __Frequency = value;
                    this.RaisePropertyChangedEvent("Frequency", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.DirectionOfFlow __DirectionOfFlow;
		
		[XmlAttribute(AttributeName="DirectionOfFlow",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.DirectionOfFlow DirectionOfFlow
		{ 
			get 
            { 
                return __DirectionOfFlow; 
            }
			set 
            {
                if (value != __DirectionOfFlow)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.DirectionOfFlow oldValue = __DirectionOfFlow;
                    __DirectionOfFlow = value;
                    this.RaisePropertyChangedEvent("DirectionOfFlow", oldValue, value);
                }
            }
		}

		[XmlIgnore]
		private Microsoft.TeamFoundation.Migration.BusinessModel.SyncContext __SyncContext;
		
		[XmlAttribute(AttributeName="SyncContext",Form=XmlSchemaForm.Unqualified)]
		public Microsoft.TeamFoundation.Migration.BusinessModel.SyncContext SyncContext
		{ 
			get 
            { 
                return __SyncContext; 
            }
			set 
            {
                if (value != __SyncContext)
                {
                    Microsoft.TeamFoundation.Migration.BusinessModel.SyncContext oldValue = __SyncContext;
                    __SyncContext = value;
                    this.RaisePropertyChangedEvent("SyncContext", oldValue, value);
                }
            }
		}

		public WorkFlowType()
		{
		}
	}


	[XmlRoot(ElementName="Configuration",IsNullable=false),Serializable]
	public partial class Configuration : Microsoft.TeamFoundation.Migration.BusinessModel.ConfigurationElement
	{

		public Configuration() : base()
		{
		}
	}
}

#pragma warning restore 1591 
