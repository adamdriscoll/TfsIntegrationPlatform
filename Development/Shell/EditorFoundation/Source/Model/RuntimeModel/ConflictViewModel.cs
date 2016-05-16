// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    public class ConflictViewModel : ModelObject
    {
        #region Fields
        RTConflict m_conflict;
        private RuntimeManager m_host;
        #endregion

        #region Constructors
        public ConflictViewModel(RTConflict conflict)
        {
            m_conflict = conflict;
            m_host = RuntimeManager.GetInstance();
        }
        #endregion

        #region Properties
        // Selected RTConflict properties
        public int Id { get { return m_conflict.Id; } }
        public string ConflictDetails { get { return m_conflict.ConflictDetails; } }
        public string ScopeHint { get { return m_conflict.ScopeHint; } }
        public Guid ScopeId { get { return m_conflict.ScopeId; } }

        // TODO: Map to enum values
        public int Status { get { return m_conflict.Status; } }

        // Collections and relationships
        /// <summary>
        /// ResolvedByRule
        /// </summary>
        private ConflictResolutionRuleViewModel m_resolvedByRule;

        public ConflictResolutionRuleViewModel ResolvedByRule 
        {
            get
            {
                if (m_resolvedByRule == null)
                {
                    if (! m_conflict.ResolvedByRuleReference.IsLoaded)
                    {
                        m_conflict.ResolvedByRuleReference.Load();
                    }

                    m_resolvedByRule = new ConflictResolutionRuleViewModel(m_conflict.ResolvedByRule);
                }

                return m_resolvedByRule;
            }
        }

        /// <summary>
        /// ConflictType
        /// </summary>
        private ConflictTypeViewModel m_conflictType;

        public ConflictTypeViewModel ConflictType
        {
            get
            {
                if (m_conflictType == null)
                {
                    if (!m_conflict.ConflictTypeReference.IsLoaded)
                    {
                        m_conflict.ConflictTypeReference.Load();
                    }

                    m_conflictType = new ConflictTypeViewModel(m_conflict.ConflictType);
                }

                return m_conflictType;
            }
        }

        /// <summary>
        /// MigrationSourceViewModel
        /// 
        /// A reference to the system that was the source of the item in conflict.
        /// </summary>
        private MigrationSourceViewModel m_sourceSideMigrationSource;

        public MigrationSourceViewModel SourceSideMigrationSource 
        {
            get
            {
                if (m_sourceSideMigrationSource == null)
                {
                    if (!m_conflict.SourceSideMigrationSourceReference.IsLoaded)
                    {
                        m_conflict.SourceSideMigrationSourceReference.Load();
                    }

                    m_sourceSideMigrationSource = new MigrationSourceViewModel(m_conflict.SourceSideMigrationSource);
                }

                return m_sourceSideMigrationSource;
            }
        }

        private ObservableCollection<ConflictContentReservationViewModel> m_conflictContentReservations;

        private ObservableCollection<ConflictContentReservationViewModel> ConflictContentReservations
        {
            get
            {
                // Static loading for now... could be moved to AutoRefresh
                if (m_conflictContentReservations == null)
                {
                    m_conflictContentReservations = new ObservableCollection<ConflictContentReservationViewModel>();

                    ObjectQuery<RTConflictContentReservation> contentReservationSet = m_conflict.ContentReservation.CreateSourceQuery();

                    m_conflictContentReservations.Clear();

                    var query = (from c in contentReservationSet
                                 orderby c.ItemId descending
                                 select c).Take(m_host.MaxQueryResults);

                    foreach (RTConflictContentReservation contentReservation in query)
                    {
                        m_conflictContentReservations.Add(new ConflictContentReservationViewModel(contentReservation));
                    }
                }

                return m_conflictContentReservations;
            }
        }

        // View support
        ObservableCollection<ResolutionActionViewModel> m_supportedResolutionActions;

        public ObservableCollection<ResolutionActionViewModel> SupportedResolutionActions
        {
            // DEBUG DEBUG DEBUG
            // DEBUG DEBUG DEBUG
            // DEBUG DEBUG DEBUG
            get
            {
                if (m_supportedResolutionActions == null)
                {
                    // TODO: Clean up path to resolution actions in adapters
                    //ConfigurationViewModelManager configHost = ConfigurationViewModelManager.GetInstance();
                    //IConflictResolutionService conflictResolutionService = (IConflictResolutionService)configHost.GetService(typeof(IConflictResolutionService));
                    //m_supportedResolutionActions = conflictResolutionService.ListResolutionActions(this.ConflictType.ReferenceName, this.ScopeId, this.SourceSideMigrationSource.UniqueId);
                }
                return m_supportedResolutionActions;
            }
            // DEBUG DEBUG DEBUG
            // DEBUG DEBUG DEBUG
            // DEBUG DEBUG DEBUG
        }
        #endregion
    }
}
