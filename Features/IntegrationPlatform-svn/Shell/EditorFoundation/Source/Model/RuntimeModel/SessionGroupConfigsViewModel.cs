// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class SessionGroupConfigsViewModel : ModelObject
    {
        RuntimeEntityModel m_runtimeEntities;

        public SessionGroupConfigsViewModel(RuntimeEntityModel runtimeEntities)
        {
            m_runtimeEntities = runtimeEntities;

            // Subscribe for notifications of when a new customer is saved.
            //_customerRepository.CustomerAdded += this.OnCustomerAddedToRepository;

            LoadAllSessionGroupConfigs();
        }

        /// <summary>
        /// Returns a collection of all the SessionConfigViewModel objects.
        /// </summary>
        public ObservableCollection<SessionGroupConfigViewModel> AllSessionGroupConfigs 
        { 
            get; 
            private set; 
        }
        
        private void LoadAllSessionGroupConfigs()
        {
            var all = (from sessionGroupConfig in m_runtimeEntities.RTSessionGroupConfigSet
                       where sessionGroupConfig.Status.Equals(0)
                       select sessionGroupConfig);

            this.AllSessionGroupConfigs = new ObservableCollection<SessionGroupConfigViewModel>();

            foreach (RTSessionGroupConfig sessionGroupConfig in all)
            {
                SessionGroupConfigViewModel sessionGroupConfigViewModel = new SessionGroupConfigViewModel(sessionGroupConfig);

                this.AllSessionGroupConfigs.Add(sessionGroupConfigViewModel);
                sessionGroupConfigViewModel.PropertyChanged += this.OnSessionGroupConfigViewModelPropertyChanged;
            }

            // TODO: Try NotifyingCollection again...
            this.AllSessionGroupConfigs.CollectionChanged += this.OnCollectionChanged;
        }

        private List<RTSessionGroupConfig> GetActiveSessionGroupConfigs()
        {
            using (RuntimeEntityModel runtimeEntities = new RuntimeEntityModel())
            {
                var sessionGroups = from sessionGroup in runtimeEntities.RTSessionGroupConfigSet
                                    where sessionGroup.Status.Equals(0)
                                    select sessionGroup;

                return sessionGroups.ToList<RTSessionGroupConfig>();
            }
        }

        #region Event Handling Methods

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (SessionGroupConfigViewModel sessionGroupConfig in e.NewItems)
                    sessionGroupConfig.PropertyChanged += this.OnSessionGroupConfigViewModelPropertyChanged;

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (SessionGroupConfigViewModel sessionGroupConfig in e.OldItems)
                    sessionGroupConfig.PropertyChanged -= this.OnSessionGroupConfigViewModelPropertyChanged;
        }

        void OnSessionGroupConfigViewModelPropertyChanged(object sender, UndoablePropertyChangedEventArgs e)
        {
            string IsSelected = "IsSelected";

            // TODO: Debug verification of property name on sink side (ModelObject checks on source side)
            // Make sure that the property name we're referencing is valid.
            //(sender as ModelObject).ValidateProperty(IsSelected);

            if (string.Equals(e.PropertyName, IsSelected, StringComparison.InvariantCulture))
            {
                return;
                //this.OnPropertyChanged("<some aggregate property of the collection>");
            }
        }

        //void OnSessionGroupConfigAdded(object sender, SessionGroupConfigAddedEventArgs e)
        //{
        //    SessionConfigGroupViewModel sessionConfigGroup = new SessionConfigGroupViewModel(e.SessionGroupConfig);
        //    this.AllSessionGroupConfigs.Add(sessionConfigGroup);
        //}

        #endregion // Event Handling Methods
    }
}
