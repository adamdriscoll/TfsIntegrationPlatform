// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.Model;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class ConfigurationViewModel : SerializableElement<Configuration>
    {
        private ShellViewModel m_viewModel;

        public ConfigurationViewModel(ShellViewModel viewModel)
            : base(viewModel.DataModel.Configuration)
        {
            m_viewModel = viewModel;
            if (string.IsNullOrEmpty(m_viewModel.DataModel.Configuration.SessionGroup.Creator))
            {
                m_viewModel.DataModel.Configuration.SessionGroup.Creator = string.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);
            }
            Validate();
            
            RefreshSerializableConfiguration();
        }

        public ShellViewModel ShellViewModel
        {
            get
            {
                return m_viewModel;
            }
        }

        public bool RemoveFilterPair(FilterPair filterPair)
        {
            foreach (SerializableSession session in SerializableSessions)
            {
                if (session.Model.Filters.FilterPair.Remove(filterPair))
                {
                    if (session.Model.Filters.FilterPair.Count == 0)
                    {
                        AddFilterPair(session);
                    }
                    return true;
                }
            }
            return false;
        }

        public void AddFilterPair(SerializableSession serializableSession)
        {
            Session session = serializableSession.Model;

            FilterItem leftItem = new FilterItem();
            leftItem.MigrationSourceUniqueId = session.LeftMigrationSourceUniqueId;
            FilterItem rightItem = new FilterItem();
            rightItem.MigrationSourceUniqueId = session.RightMigrationSourceUniqueId;

            string leftSourceIdentifier = MigrationSources.First(x => string.Equals(x.InternalUniqueId, session.LeftMigrationSourceUniqueId, StringComparison.OrdinalIgnoreCase)).SourceIdentifier;
            string rightSourceIdentifier = MigrationSources.First(x => string.Equals(x.InternalUniqueId, session.RightMigrationSourceUniqueId, StringComparison.OrdinalIgnoreCase)).SourceIdentifier;
            
            switch (session.SessionType)
            {
                case SessionTypeEnum.VersionControl:
                    FilterPairViewModel existingFilterPair = serializableSession.DefaultFilterPair;
                    if (existingFilterPair != null)
                    {
                        leftItem.FilterString = existingFilterPair.LeftFilterStringExtension.VCFilterStringPrefix + leftSourceIdentifier;
                        rightItem.FilterString = existingFilterPair.RightFilterStringExtension.VCFilterStringPrefix + rightSourceIdentifier;
                    }
                    else
                    {
                        leftItem.FilterString = string.Empty;
                        rightItem.FilterString = string.Empty;
                    }
                    break;
                case SessionTypeEnum.WorkItemTracking:
                    leftItem.FilterString = string.Empty;
                    rightItem.FilterString = string.Empty;
                    break;
            }

            FilterPair newFilterPair = new FilterPair();
            newFilterPair.FilterItem.Add(leftItem);
            newFilterPair.FilterItem.Add(rightItem);

            session.Filters.FilterPair.Add(newFilterPair);

            RefreshFilterStrings();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true to close window, false to keep open</returns>
        public bool Cancel()
        {
            if (m_viewModel.IsDirty && m_viewModel.DataModel != null)
            {
                m_viewModel.HasErrors = FormErrors.Count + Errors.Count != 0;

                Guid g = m_viewModel.DataModel.Configuration.SessionGroupUniqueId;
                if (m_viewModel.IsConfigurationPersisted)
                {
                    if (!m_viewModel.OpenFromDB(g))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!m_viewModel.Close())
                    {
                        return false;
                    }
                    int persistedSessionGroups;
                    using (Microsoft.TeamFoundation.Migration.EntityModel.RuntimeEntityModel context = Microsoft.TeamFoundation.Migration.EntityModel.RuntimeEntityModel.CreateInstance())
                    {
                        persistedSessionGroups = context.RTSessionGroupSet.Where(x => x.GroupUniqueId.Equals(g)).Count();
                    }
                    if (persistedSessionGroups > 0) // is persisted
                    {
                        m_viewModel.OpenFromDB(g);
                    }
                }

                m_viewModel.HasErrors = false;
            }
            return true;
        }

        public void RefreshFilterStrings()
        {
            if (Model.SessionGroup.WorkFlowType.DirectionOfFlow == DirectionOfFlow.Unidirectional)
            {
                foreach (SerializableSession session in SerializableSessions.Where(x => x.Model.SessionType == SessionTypeEnum.WorkItemTracking))
                {
                    foreach (FilterPairViewModel filterPair in session.FilterPairs)
                    {
                        if (filterPair.RightFilterStringExtension != null)
                        {
                            filterPair.RightFilterItem.FilterString = filterPair.RightFilterStringExtension.EmptyWITQuery;
                        }
                    }
                }
            }
        }

        public override bool Save()
        {
            if (base.Save())
            {
                m_viewModel.DataModel.Configuration = Model;
                RefreshSerializableConfiguration();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override Configuration Deserialize(string content)
        {
            ConfigurationModelSerializer serializer = new ConfigurationModelSerializer();
            return serializer.TryDeserialize(content, true, false) as Configuration;
        }

        private void RefreshSerializableConfiguration()
        {
            Sessions.ItemAdded += new ItemAddedEventHandler<Session>(Sessions_ItemAdded);
            Sessions.ItemRemoved += new ItemRemovedEventHandler<Session>(Sessions_ItemRemoved);
            foreach (var filterPair in Sessions.Select(x => x.Filters.FilterPair))
            {
                filterPair.ItemAdded += new ItemAddedEventHandler<FilterPair>(filterPair_ItemAdded);
                filterPair.ItemRemoved += new ItemRemovedEventHandler<FilterPair>(filterPair_ItemRemoved);
            }

            MigrationSources.ItemAdded += new ItemAddedEventHandler<MigrationSource>(MigrationSources_ItemAdded);
            MigrationSources.ItemRemoved += new ItemRemovedEventHandler<MigrationSource>(MigrationSources_ItemRemoved);
            MigrationSources.ItemReplaced += new ItemReplacedEventHandler<MigrationSource>(MigrationSources_ItemReplaced);
            foreach (MigrationSource source in MigrationSources)
            {
                source.PropertyChanged += new UndoablePropertyChangedEventHandler(configurationSource_PropertyChanged);
            }

            RefreshSerializableSources();
            RefreshSerializableSessions();
        }

        void filterPair_ItemRemoved(IDualNotifyingCollection<FilterPair> sender, ItemRemovedEventArgs<FilterPair> eventArgs)
        {
            Validate();
        }

        void filterPair_ItemAdded(IDualNotifyingCollection<FilterPair> sender, ItemAddedEventArgs<FilterPair> eventArgs)
        {
            Validate();
        }

        void MigrationSources_ItemRemoved(IDualNotifyingCollection<MigrationSource> sender, ItemRemovedEventArgs<MigrationSource> eventArgs)
        {
            SerializableSources.Remove(SerializableSources.First(x => x.Model == eventArgs.Item));
        }

        void MigrationSources_ItemAdded(IDualNotifyingCollection<MigrationSource> sender, ItemAddedEventArgs<MigrationSource> eventArgs)
        {
            eventArgs.Item.PropertyChanged += new UndoablePropertyChangedEventHandler(configurationSource_PropertyChanged);
            SerializableSources.Add(new SerializableSource(eventArgs.Item, MigrationSources, Sessions, this));
        }

        void MigrationSources_ItemReplaced(IDualNotifyingCollection<MigrationSource> sender, ItemReplacedEventArgs<MigrationSource> eventArgs)
        {
            eventArgs.NewItem.PropertyChanged += new UndoablePropertyChangedEventHandler(configurationSource_PropertyChanged);
        }

        void configurationSource_PropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName.Equals("ProviderReferenceName"))
            {
                Validate();
            }
        }

        void Sessions_ItemRemoved(IDualNotifyingCollection<Session> sender, ItemRemovedEventArgs<Session> eventArgs)
        {
            SerializableSessions.Remove(SerializableSessions.First(x => x.Model == eventArgs.Item));
            Validate();
            OnPropertyChanged("CanAddVCSession");
            OnPropertyChanged("CanAddWITSession");
        }

        void Sessions_ItemAdded(IDualNotifyingCollection<Session> sender, ItemAddedEventArgs<Session> eventArgs)
        {
            SerializableSessions.Add(new SerializableSession(eventArgs.Item, Sessions, SerializableSources, this));
            eventArgs.Item.Filters.FilterPair.ItemAdded += new ItemAddedEventHandler<FilterPair>(filterPair_ItemAdded);
            eventArgs.Item.Filters.FilterPair.ItemRemoved += new ItemRemovedEventHandler<FilterPair>(filterPair_ItemRemoved);
            Validate();
            OnPropertyChanged("CanAddVCSession");
            OnPropertyChanged("CanAddWITSession");
        }

        private bool? m_canReconfigure;
        public bool CanReconfigure
        {
            get
            {
                if (m_canReconfigure == null)
                {
                    m_canReconfigure = !m_viewModel.SessionGroupStatus.WasRunBefore;
                }
                return m_canReconfigure == true;
            }
        }

        public ExtensibilityViewModel ExtensibilityViewModel
        {
            get
            {
                return m_viewModel.ExtensibilityViewModel;
            }
        }

        public NotifyingCollection<MigrationSource> MigrationSources
        {
            get
            {
                return m_viewModel.DataModel.Configuration.SessionGroup.MigrationSources.MigrationSource;
            }
        }

        private NotifyingCollection<SerializableSource> m_serializableSources;
        public NotifyingCollection<SerializableSource> SerializableSources
        {
            get
            {
                if (m_serializableSources == null)
                {
                    m_serializableSources = new NotifyingCollection<SerializableSource>();
                    RefreshSerializableSources();
                }
                return m_serializableSources;
            }
        }

        public bool CanAddVCSession
        {
            get
            {
                return Sessions.Count(x => x.SessionType == SessionTypeEnum.VersionControl) == 0;
            }
        }

        public bool CanAddWITSession
        {
            get
            {
                return Sessions.Count(x => x.SessionType == SessionTypeEnum.WorkItemTracking) == 0;
            }
        }

        private NotifyingCollection<Session> Sessions
        {
            get
            {
                return m_viewModel.DataModel.Configuration.SessionGroup.Sessions.Session;
            }
        }

        private NotifyingCollection<SerializableSession> m_serializableSessions;
        public NotifyingCollection<SerializableSession> SerializableSessions
        {
            get
            {
                if (m_serializableSessions == null)
                {
                    m_serializableSessions = new NotifyingCollection<SerializableSession>();
                    RefreshSerializableSessions();
                }
                return m_serializableSessions;
            }
        }

        private void RefreshSerializableSessions()
        {
            SerializableSessions.Clear();
            foreach (Session session in Sessions)
            {
                SerializableSessions.Add(new SerializableSession(session, Sessions, SerializableSources, this));
            }
        }

        private void RefreshSerializableSources()
        {
            SerializableSources.Clear();
            foreach (MigrationSource source in MigrationSources)
            {
                SerializableSources.Add(new SerializableSource(source, MigrationSources, Sessions, this));
            }
        }

        public IEnumerable<ProviderHandler> AllProviders
        {
            get
            {
                return Toolkit.Utility.LoadProvider(new DirectoryInfo(Toolkit.Constants.PluginsFolderName));
            }
        }

        public NotifyingCollection<ProviderElement> Providers
        {
            get
            {
                return m_viewModel.DataModel.Configuration.Providers.Provider;
            }
        }

        public override string PrettySerializedContent
        {
            get
            {
                return SerializedContent;
            }
            set
            {
                SerializedContent = value;
            }
        }

        private CompositeCollection m_compositeCollection;
        private CollectionViewSource m_allErrors;
        public CollectionViewSource AllErrors
        {
            get
            {
                if (m_allErrors == null)
                {
                    CollectionContainer formErrorsContainer = new CollectionContainer();
                    formErrorsContainer.Collection = FormErrors;

                    CollectionContainer errorsContainer = new CollectionContainer();
                    errorsContainer.Collection = Errors;

                    m_compositeCollection = new CompositeCollection();
                    m_compositeCollection.Add(formErrorsContainer);
                    m_compositeCollection.Add(errorsContainer);

                    m_allErrors = new CollectionViewSource { Source = m_compositeCollection };
                    m_allErrors.View.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(View_CollectionChanged);
                }
                return m_allErrors;
            }
        }

        void View_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            m_allErrors.View.MoveCurrentToFirst();
        }

        private ObservableCollection<string> m_formErrors = new ObservableCollection<string>();
        public ObservableCollection<string> FormErrors
        {
            get
            {
                return m_formErrors;
            }
        }

        /// <summary>
        /// Check for form errors.
        /// </summary>
        /// <returns>Whether or not configuration has been serialized already.</returns>
        public bool Validate()
        {
            foreach (ProviderElement provider in Providers)
            {
                Guid providerId = new Guid(provider.ReferenceName);
                ProviderHandler providerHandler = AllProviders.FirstOrDefault(x => x.ProviderId.Equals(providerId));
                if (providerHandler != null)
                {
                    RegisterFilterStringExtension(providerHandler.ProviderDescriptionAttribute.ShellAdapterIdentifier, providerId);
                }
            }

            bool xmlHasBeenSerialized = false;

            m_formErrors.Clear();

            var invalidProviders = from p in Providers
                                   where !Guid.Empty.Equals(new Guid(p.ReferenceName))
                                   && !AllProviders.Select(x => x.ProviderId).Contains(new Guid(p.ReferenceName))
                                   select p;

            foreach (var provider in invalidProviders)
            {
                m_formErrors.Add(string.Format("Provider '{0}' not found", provider.FriendlyName));
            }

            if (Sessions.Count == 0)
            {
                m_formErrors.Add("Need to add at least 1 session.");
            }
            else
            {
                foreach (SerializableSession session in SerializableSessions)
                {
                    if (!session.LeftMigrationSource.IsConfigured)
                    {
                        m_formErrors.Add("Need to configure left migration source for " + session.Model.FriendlyName);
                    }
                    if (!session.RightMigrationSource.IsConfigured)
                    {
                        m_formErrors.Add("Need to configure right migration source for " + session.Model.FriendlyName);
                    }
                    if (session.Model.Filters.FilterPair.Count == 0 && session.LeftMigrationSource.IsConfigured && session.RightMigrationSource.IsConfigured)
                    {
                        m_formErrors.Add("Need to add filter pair");
                    }
                }
                if (m_formErrors.Count == 0)
                {
                    // no form errors, check xml errors
                    SerializedContent = Serialize();
                    xmlHasBeenSerialized = true;
                }
            }

            return xmlHasBeenSerialized;
        }

        internal void RegisterFilterStringExtension(Guid shellAdapterIdentifier, Guid providerId)
        {
            ExecuteFilterStringExtension command = m_viewModel.PluginManager.GetFilterStringExtension(shellAdapterIdentifier);
            ExtensibilityViewModel.AddFilterStringExtension(providerId, command);
        }
    }

    public abstract class SerializableElement : INotifyPropertyChanged
    {
        private ObservableCollection<string> m_errors = new ObservableCollection<string>();
        public ObservableCollection<string> Errors
        {
            get
            {
                return m_errors;
            }
        }

        public abstract bool Save();
        public abstract bool IsEditingXml { get; set; }
        public abstract string Serialize();
        public abstract string SerializedContent { get; set; }
        
        private int m_lineNumber;
        public int LineNumber
        {
            get
            {
                return m_lineNumber;
            }
            set
            {
                m_lineNumber = value;
                OnPropertyChanged("LineNumber");
            }
        }

        private int m_columnNumber;
        public int ColumnNumber
        {
            get
            {
                return m_columnNumber;
            }
            set
            {
                m_columnNumber = value;
                OnPropertyChanged("ColumnNumber");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    public abstract class SerializableElement<T> : SerializableElement
        where T : ModelObject, new()
    {
        private T m_model;

        public SerializableElement(T model)
        {
            m_model = model;
        }

        public T Model
        {
            get
            {
                return m_model;
            }
            private set
            {
                m_model = value;
                OnPropertyChanged("Model");
            }
        }

        public override bool Save()
        {
            if (!string.Equals(Serialize(), SerializedContent))
            {
                Model = Deserialize(SerializedContent);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Validate(string serializedContent)
        {
            try
            {
                Errors.Clear();
                Deserialize(serializedContent);
            }
            catch (ConfigurationSchemaViolationException e)
            {
                foreach (var error in e.ConfigurationValidationResult.SchemaValidationResults)
                {
                    Errors.Add(error.Message);
                }
            }
            catch (ConfigurationBusinessRuleViolationException e)
            {
                foreach (var result in e.ConfigurationValidationResult.ResultItems)
                {
                    Errors.Add(result.ToString());
                }
            }
            catch (Exception e)
            {
                Errors.Add(e.Message);
                if (e.InnerException != null)
                {
                    Errors[0] += ": " + e.InnerException.Message;
                }
            }
        }

        private bool m_isEditingXml = false;
        public override bool IsEditingXml
        {
            get
            {
                return m_isEditingXml;
            }
            set
            {
                if (m_isEditingXml != value)
                {
                    m_isEditingXml = value;
                    if (m_isEditingXml || Errors.Count > 0)
                    {
                        SerializedContent = Serialize();
                    }
                    OnPropertyChanged("IsEditingXml");
                }
            }
        }

        private string m_serializedContent;
        public override string SerializedContent
        {
            get
            {
                return m_serializedContent;
            }
            set
            {
                if (m_serializedContent == null || !m_serializedContent.Equals(value))
                {
                    m_serializedContent = value;
                    OnPropertyChanged("PrettySerializedContent");
                    Validate(SerializedContent);
                }
            }
        }

        private string m_startingTag;
        public string StartingTag
        {
            get
            {
                if (m_startingTag != null)
                {
                    string guidPattern = "[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}";
                    string xmlnsPattern = "xmlns:xs[id]{1}=";

                    List<string> attributeList = m_startingTag.TrimEnd('>').Split('\n').Last().Split(' ').ToList();

                    attributeList.RemoveAll(x => Regex.IsMatch(x, guidPattern) || Regex.IsMatch(x, xmlnsPattern));
                    return string.Join(" ", attributeList.ToArray()) + ">";
                }
                else
                {
                    return null;
                }
            }
            private set
            {
                m_startingTag = value;
                OnPropertyChanged("StartingTag");
            }
        }

        private string m_endingTag;
        public string EndingTag
        {
            get
            {
                return m_endingTag;
            }
            private set
            {
                m_endingTag = value;
                OnPropertyChanged("EndingTag");
            }
        }

        public virtual string PrettySerializedContent
        {
            get
            {
                if (SerializedContent != null)
                {
                    int first = SerializedContent.IndexOf('>');
                    int second = SerializedContent.Substring(first + 1).IndexOf('>') + first + 1;
                    StartingTag = SerializedContent.Substring(0, second + 1);
                    string str = SerializedContent.Substring(second + 1);

                    int last = str.LastIndexOf('<');
                    EndingTag = str.Substring(last);
                    str = str.Substring(0, last);
                    str = str.Trim(Environment.NewLine.ToCharArray());
                    return str;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                SerializedContent = m_startingTag + value + m_endingTag;
            }
        }

        /// <summary>
        /// Serialize an object into XML.
        /// </summary>
        /// <returns></returns>
        public override string Serialize()
        {
            Stream stream = new MemoryStream();
            XmlModelSerializer<T> serializer = new XmlModelSerializer<T>();
            serializer.Serialize(stream, m_model);

            StreamReader reader = new StreamReader(stream);
            stream.Position = 0;
            string content = reader.ReadToEnd();

            return content;
        }

        /// <summary>
        /// Deserializes the content and puts it in property Model.  Override to save back to original model.
        /// </summary>
        /// <param name="content"></param>
        protected virtual T Deserialize(string content)
        {
            Stream stream = new MemoryStream(Encoding.Unicode.GetBytes(content));
            XmlModelSerializer<T> serializer = new XmlModelSerializer<T>();
            return serializer.Deserialize(stream) as T;
        }
    }

    public class SerializableSession : SerializableElement<Session>
    {
        private ConfigurationViewModel m_configuration;

        public SerializableSession(Session session, NotifyingCollection<Session> sessions, NotifyingCollection<SerializableSource> sources, ConfigurationViewModel configuration)
            : base(session)
        {
            m_sessions = sessions;
            m_sources = sources;
            m_configuration = configuration;

            if (Model.Filters.FilterPair.Count == 0)
            {
                m_configuration.AddFilterPair(this);
            }

            foreach (var v in Model.Filters.FilterPair)
            {
                m_filterPairViewModels.Add(new FilterPairViewModel(v, Model, m_configuration));
            }

            DefaultFilterPair = m_filterPairViewModels.FirstOrDefault();
            
            Model.Filters.FilterPair.ItemAdded += new ItemAddedEventHandler<FilterPair>(FilterPair_ItemAdded);
            Model.Filters.FilterPair.ItemRemoved += new ItemRemovedEventHandler<FilterPair>(FilterPair_ItemRemoved);
            Model.Filters.FilterPair.ItemReplaced += new ItemReplacedEventHandler<FilterPair>(FilterPair_ItemReplaced);
        }

        void FilterPair_ItemReplaced(IDualNotifyingCollection<FilterPair> sender, ItemReplacedEventArgs<FilterPair> eventArgs)
        {
            throw new NotImplementedException();
        }

        void FilterPair_ItemRemoved(IDualNotifyingCollection<FilterPair> sender, ItemRemovedEventArgs<FilterPair> eventArgs)
        {
            m_filterPairViewModels.RemoveAt(eventArgs.Index);
        }

        void FilterPair_ItemAdded(IDualNotifyingCollection<FilterPair> sender, ItemAddedEventArgs<FilterPair> eventArgs)
        {
            m_filterPairViewModels.Add(new FilterPairViewModel(eventArgs.Item, Model, m_configuration));
        }

        private ObservableCollection<FilterPairViewModel> m_filterPairViewModels = new ObservableCollection<FilterPairViewModel>();

        public override bool Save()
        {
            Session oldModel = Model;
            int index = m_sessions.IndexOf(oldModel);
            if (base.Save())
            {
                m_sessions[index] = Model;
                return true;
            }
            else
            {
                return false;
            }
        }

        public SerializableSource LeftMigrationSource
        {
            get
            {
                return m_sources.First(x => string.Equals(x.Model.InternalUniqueId, Model.LeftMigrationSourceUniqueId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public SerializableSource RightMigrationSource
        {
            get
            {
                return m_sources.First(x => string.Equals(x.Model.InternalUniqueId, Model.RightMigrationSourceUniqueId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public FilterPairViewModel DefaultFilterPair { get; private set; }

        public ObservableCollection<FilterPairViewModel> FilterPairs
        {
            get
            {
                return m_filterPairViewModels;
            }
        }

        public NotifyingCollection<Session> m_sessions;
        public NotifyingCollection<SerializableSource> m_sources;
    }

    public enum FilterPairType
    {
        VC,
        WITOneWay,
        WITTwoWay
    }

    public class FilterPairViewModel : INotifyPropertyChanged
    {
        private FilterPair m_filterPair;
        private Session m_session;
        private ConfigurationViewModel m_configuration;
        public FilterPair FilterPair
        {
            get
            {
                return m_filterPair;
            }
        }

        public FilterItem LeftFilterItem { get; private set; }
        public FilterItem RightFilterItem { get; private set; }

        public FilterPairViewModel(FilterPair filterPair, Session session, ConfigurationViewModel configuration)
        {
            m_filterPair = filterPair;
            m_session = session;
            m_configuration = configuration;

            LeftFilterItem = filterPair.FilterItem.FirstOrDefault(x => string.Equals(x.MigrationSourceUniqueId, session.LeftMigrationSourceUniqueId));
            Debug.Assert(LeftFilterItem != null, "LeftFilterItem == null");
            RightFilterItem = filterPair.FilterItem.FirstOrDefault(x => string.Equals(x.MigrationSourceUniqueId, session.RightMigrationSourceUniqueId));
            Debug.Assert(RightFilterItem != null, "RightFilterItem == null");

            UpdateFilterStringExtensions();
        }

        public MigrationSource LeftMigrationSource
        {
            get
            {
                return m_configuration.MigrationSources.FirstOrDefault(x => string.Equals(x.InternalUniqueId, m_session.LeftMigrationSourceUniqueId));
            }
        }

        public MigrationSource RightMigrationSource
        {
            get
            {
                return m_configuration.MigrationSources.FirstOrDefault(x => string.Equals(x.InternalUniqueId, m_session.RightMigrationSourceUniqueId));
            }
        }

        public void UpdateFilterStringExtensions()
        {
            Guid leftProviderId = GetProviderFromFilterItem(LeftFilterItem);
            Guid rightProviderId = GetProviderFromFilterItem(RightFilterItem);
            LeftFilterStringExtension = m_configuration.ExtensibilityViewModel.GetFilterStringExtension(leftProviderId);
            RightFilterStringExtension = m_configuration.ExtensibilityViewModel.GetFilterStringExtension(rightProviderId);
        }

        private Guid GetProviderFromFilterItem(FilterItem filterItem)
        {
            Guid migrationSourceUniqueId = new Guid(filterItem.MigrationSourceUniqueId);
            MigrationSource migrationSource = m_configuration.MigrationSources.FirstOrDefault(x => migrationSourceUniqueId.Equals(new Guid(x.InternalUniqueId)));
            Debug.Assert(migrationSource != null, "Could not find migration source");
            return new Guid(migrationSource.ProviderReferenceName);
        }

        private ExecuteFilterStringExtension m_leftFilterStringExtension;
        public ExecuteFilterStringExtension LeftFilterStringExtension
        {
            get
            {
                return m_leftFilterStringExtension;
            }
            private set
            {
                m_leftFilterStringExtension = value;
                OnPropertyChanged("LeftFilterStringExtension");
            }
        }

        private ExecuteFilterStringExtension m_rightFilterStringExtension;
        public ExecuteFilterStringExtension RightFilterStringExtension
        {
            get
            {
                return m_rightFilterStringExtension;
            }
            private set
            {
                m_rightFilterStringExtension = value;
                OnPropertyChanged("RightFilterStringExtension");
            }
        }

        public FilterPairType FilterPairType
        {
            get
            {
                if (m_session.SessionType == SessionTypeEnum.VersionControl)
                {
                    return FilterPairType.VC;
                }
                else // SessionTypeEnum.WorkItemTracking
                {
                    Debug.Assert(m_session.SessionType == SessionTypeEnum.WorkItemTracking, string.Format("Unknown session type {0}", m_session.SessionType));
                    if (m_configuration.Model.SessionGroup.WorkFlowType.DirectionOfFlow == DirectionOfFlow.Unidirectional)
                    {
                        return FilterPairType.WITOneWay;
                    }
                    else // DirectionOfFlow.Bidirectional
                    {
                        Debug.Assert(m_configuration.Model.SessionGroup.WorkFlowType.DirectionOfFlow == DirectionOfFlow.Bidirectional, string.Format("Unknown direction {0}", m_configuration.Model.SessionGroup.WorkFlowType.DirectionOfFlow));
                        return FilterPairType.WITTwoWay;
                    }
                }
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion
    }

    public class SerializableSource : SerializableElement<MigrationSource>
    {
        private ConfigurationViewModel m_configuration;

        public SerializableSource(MigrationSource source, NotifyingCollection<MigrationSource> sources, NotifyingCollection<Session> sessions, ConfigurationViewModel configuration)
            : base(source)
        {
            m_configuration = configuration;
            m_sources = sources;
            m_sessions = sessions;
        }

        void Model_PropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName.Equals("ProviderReferenceName"))
            {
                OnPropertyChanged("IsConfigured");
            }
        }

        public void Refresh()
        {
            OnPropertyChanged("IsConfigured");
            OnPropertyChanged("Properties");
        }

        public override bool Save()
        {
            MigrationSource oldModel = Model;
            int index = m_sources.IndexOf(oldModel);
            if (base.Save())
            {
                Model.PropertyChanged += new UndoablePropertyChangedEventHandler(Model_PropertyChanged);
                m_sources[index] = Model;
                return true;
            }
            else
            {
                return false;
            }
        }

        public Dictionary<string, string> Properties
        {
            get
            {
                Guid shellAdapterIdentifier;
                ProviderHandler providerHandler = m_configuration.AllProviders.FirstOrDefault(x => x.ProviderId.Equals(new Guid(Model.ProviderReferenceName)));
                if (providerHandler != null)
                {
                    shellAdapterIdentifier = providerHandler.ProviderDescriptionAttribute.ShellAdapterIdentifier;
                }
                else
                {
                    shellAdapterIdentifier = Guid.Empty;
                }

                return m_configuration.ExtensibilityViewModel.GetMigrationSourceProperties(Model, shellAdapterIdentifier);
            }
        }

        public bool IsConfigured
        {
            get
            {
                return Model.ProviderReferenceName != null && !Guid.Empty.Equals(new Guid(Model.ProviderReferenceName));
            }
        }

        public Session Session
        {
            get
            {
                return m_sessions.First(x => string.Equals(x.LeftMigrationSourceUniqueId, Model.InternalUniqueId, StringComparison.OrdinalIgnoreCase) || string.Equals(x.RightMigrationSourceUniqueId, Model.InternalUniqueId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public NotifyingCollection<MigrationSource> m_sources;
        public NotifyingCollection<Session> m_sessions;
    }

    public class SerializableCustomSettings : SerializableElement<GenericSettingsElement>
    {
        private Session m_session;

        public SerializableCustomSettings(Session session)
            : base(session.CustomSettings)
        {
            m_session = session;
        }

        protected override GenericSettingsElement Deserialize(string content)
        {
            GenericSettingsElement customSettings = base.Deserialize(content);
            Configuration.ValidateCustomSettings(customSettings, m_session.SessionType);
            return customSettings;
        }

        public object CustomSettings
        {
            get
            {
                switch (m_session.SessionType)
                {
                    case SessionTypeEnum.VersionControl:
                        return m_session.VCCustomSetting;
                    case SessionTypeEnum.WorkItemTracking:
                        WITCustomSettingViewModel witSettings = new WITCustomSettingViewModel(m_session.WITCustomSetting);
                        return witSettings;
                    default:
                        return null;
                }
            }
        }

        public override bool Save()
        {
            if (base.Save())
            {
                m_session.CustomSettings = Model;
                OnPropertyChanged("CustomSettings");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
