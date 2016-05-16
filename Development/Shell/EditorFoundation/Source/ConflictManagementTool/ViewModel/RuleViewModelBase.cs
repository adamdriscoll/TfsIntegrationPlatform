// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
    public abstract class RuleViewModelBase : INotifyPropertyChanged
    {
        public RuleViewModelBase(ApplicationViewModel appViewModel)
        {
            m_appViewModel = appViewModel;
        }

        public void SetConflictManager(Guid scopeId, Guid sourceId, Guid conflictTypeReferenceName)
        {
            m_conflictManager = null;

            IEnumerable<ConflictManager> conflictManagers = m_appViewModel.Sync.GetConflictManagers(scopeId, sourceId);
            foreach (var manager in conflictManagers)
            {
                if (manager.RegisteredConflictTypes.ContainsKey(conflictTypeReferenceName))
                {
                    m_conflictManager = manager;
                    break;
                }
            }
            Debug.Assert(m_conflictManager != null, string.Format("Unable to find ConflictManager.  Possible resolution: register conflict type ({0}) with ConflictManager.", conflictTypeReferenceName));

            if (m_conflictManager != null)
            {
                ConflictType = m_conflictManager.RegisteredConflictTypes[conflictTypeReferenceName];
            }
            else
            {
                ConflictType = new GenericConflictType();
            }
        }

        public ConflictType ConflictType
        {
            get
            {
                return m_conflictType;
            }
            set
            {
                if (m_conflictType != value)
                {
                    m_conflictType = value;
                    OnPropertyChanged("ScopeSyntaxHint");
                    OnPropertyChanged("ResolutionActions");
                    m_selectedResolutionAction = null;
                    OnPropertyChanged("SelectedResolutionAction");
                    OnPropertyChanged("CanSave");
                }
            }
        }

        public virtual string Scope
        {
            get
            {
                return m_scope;
            }
            set
            {
                if (m_scope == null || (m_scope != null && !m_scope.Equals(value)))
                {
                    m_scope = value;
                    OnPropertyChanged("Scope");
                    OnPropertyChanged("CanSave");
                    OnPropertyChanged("IsScopeValid");
                }
            }
        }

        private string m_description;
        public virtual string Description
        {
            get
            {
                return m_description;
            }
            set
            {
                m_description = value;
                OnPropertyChanged("Description");
            }
        }
        
        public string ScopeSyntaxHint
        {
            get
            {
                return ConflictType.ScopeSyntaxHint;
            }
        }

        public IEnumerable<ResolutionAction> ResolutionActions
        {
            get
            {
                if (ConflictType != null)
                {
                    m_resolutionActions = ConflictType.SupportedResolutionActions.Select(x => x.Value);
                }
                else
                {
                    m_resolutionActions = new List<ResolutionAction>();
                }
                return m_resolutionActions;
            }
        }
        
        public virtual ResolutionAction SelectedResolutionAction
        {
            get
            {
                if (m_selectedResolutionAction == null)
                {
                    SelectedResolutionAction = ResolutionActions.FirstOrDefault();
                }
                return m_selectedResolutionAction;
            }
            set
            {
                if (value != null && m_selectedResolutionAction != value)
                {
                    m_selectedResolutionAction = value;
                    m_dataFields = new BindingList<ObservableDataField>();
                    foreach (string key in SelectedResolutionAction.ActionDataKeys)
                    {
                        DataField dataField = new DataField();
                        dataField.FieldName = key;
                        m_dataFields.Add(new ObservableDataField(dataField));
                    }
                    m_dataFields.ListChanged += new ListChangedEventHandler(m_DataFields_ListChanged);
                    OnPropertyChanged("ObservableDataFields");
                    OnPropertyChanged("DataFieldsEnabled");
                    OnPropertyChanged("SelectedResolutionAction");
                }
            }
        }

        void m_DataFields_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.PropertyDescriptor.DisplayName.Equals("IsChanged"))
            {
                OnPropertyChanged("CanSave");
            }
        }

        public bool DataFieldsIsChanged
        {
            get
            {
                foreach (ObservableDataField dataField in m_dataFields)
                {
                    if (dataField.IsChanged)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public IEnumerable<ObservableDataField> ObservableDataFields
        {
            get
            {
                return m_dataFields;
            }
        }

        public IEnumerable<DataField> DataFields
        {
            get
            {
                return m_dataFields.Select(x => x.DataField);
            }
        }
        
        public bool DataFieldsEnabled
        {
            get
            {
                return DataFields.Count() != 0;
            }
        }

        public string DescriptionDoc
        {
            get
            {
                if (m_appViewModel.ExtensibilityViewModel != null)
                {
                    return m_appViewModel.ExtensibilityViewModel.GetConflictTypeDescription(this, SourceId);
                }
                return string.Empty;
            }
        }

        public bool IsScopeReadOnly
        {
            get
            {
                try
                {
                    string guid = Scope.Split('/').Last();
                    Guid scopeId = new Guid(guid);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool IsScopeValid
        {
            get
            {
                if (m_conflictManager == null)
                {
                    return false;
                }
                else
                {
                    string hint;
                    return ConflictManagementServiceProxy.Conflicts.IsResolutionRuleScopeValid(ConflictType, Scope, out hint);
                }
            }
        }

        public virtual bool CanSave
        {
            get
            {
                OnPropertyChanged("IsScopeValid");
                return IsScopeValid;
            }
        }
        public abstract Guid SourceId { get; }
        public ICollection<MigrationConflict> ResolvableConflicts
        {
            get
            {
                return ConflictManagementServiceProxy.Conflicts.GetResolvableConflictListByScope(
                    m_appViewModel.AllConflicts.Where(x => x.IsResolved != ResolvedStatus.Resolved && x.SourceId.Equals(SourceId)).Select(x => x.MigrationConflict).ToArray(), ConflictType, Scope).ToList();
            }
        }

        private IEnumerable<ResolutionAction> m_resolutionActions;
        protected ConflictManager m_conflictManager;
        protected ApplicationViewModel m_appViewModel; // needed to get a handle on all conflicts to get resolvable list
        private ConflictType m_conflictType;
        protected ResolutionAction m_selectedResolutionAction;
        private BindingList<ObservableDataField> m_dataFields = new BindingList<ObservableDataField>();
        private string m_scope;
        
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

    public class ObservableDataField : INotifyPropertyChanged
    {
        public ObservableDataField(DataField dataField)
        {
            m_dataField = dataField;
            m_oldFieldValue = dataField.FieldValue;
        }

        public DataField DataField
        {
            get
            {
                return m_dataField;
            }
        }

        public string FieldName
        {
            get
            {
                return m_dataField.FieldName;
            }
        }

        public string FieldValue
        {
            get
            {
                return m_dataField.FieldValue;
            }
            set
            {
                m_dataField.FieldValue = value;
                OnPropertyChanged("FieldValue");
                OnPropertyChanged("IsChanged");
            }
        }

        public string DefaultFieldValue
        {
            get
            {
                return m_oldFieldValue;
            }
            set
            {
                m_oldFieldValue = value;
                m_dataField.FieldValue = value;
            }
        }

        public bool IsChanged
        {
            get
            {
                if (m_oldFieldValue != null)
                {
                    return !m_oldFieldValue.Equals(FieldValue);
                }
                else
                {
                    return !string.IsNullOrEmpty(FieldValue);
                }
            }
        }

        private DataField m_dataField;
        private string m_oldFieldValue;

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
}
