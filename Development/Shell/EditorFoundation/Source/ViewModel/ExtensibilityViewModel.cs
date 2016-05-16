// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    public class ExtensibilityViewModel
    {
        #region Fields
        private Dictionary<Guid, IMigrationSourceView> m_migrationSourceViews = new Dictionary<Guid, IMigrationSourceView>();
        private Dictionary<Guid, Dictionary<Guid, IConflictTypeView>> m_conflictTypes = new Dictionary<Guid, Dictionary<Guid, IConflictTypeView>>();
        #endregion

        public ExecuteFilterStringExtension GetFilterStringExtension(Guid providerId)
        {
            if (m_filterStringCommands.ContainsKey(providerId))
            {
                return m_filterStringCommands[providerId];
            }
            else
            {
                return null;
            }
        }

        public IMigrationSourceView GetMigrationSourceView(Guid guid)
        {
            if (!m_migrationSourceViews.ContainsKey(guid)) // shellAdapter not found, use default
            {
                Debug.Assert(m_migrationSourceViews.ContainsKey(Guid.Empty), "Default adapter is not found"); // TODO: throw exception if default adapter is not found
                guid = Guid.Empty;
            }
            return m_migrationSourceViews[guid];
        }

        private IConflictTypeView GetConflictTypeView(RuleViewModelBase conflict, Guid sourceId)
        {
            Guid conflictTypeGuid = conflict.ConflictType.ReferenceName;

            IConflictTypeView conflictType = null;
            if (m_conflictTypes.ContainsKey(sourceId))
            {
                m_conflictTypes[sourceId].TryGetValue(conflictTypeGuid, out conflictType);
            }
            return conflictType;
        }

        public string GetConflictTypeFriendlyName(RuleViewModelBase conflict, Guid sourceId)
        {
            IConflictTypeView conflictType = GetConflictTypeView(conflict, sourceId);

            if (conflictType == null || string.IsNullOrEmpty(conflictType.FriendlyName))
            {
                return conflict.ConflictType.FriendlyName;
            }
            else
            {
                return conflictType.FriendlyName;
            }
        }

        public string GetConflictTypeDescription(RuleViewModelBase conflict, Guid sourceId)
        {
            IConflictTypeView conflictType = GetConflictTypeView(conflict, sourceId);

            if (conflictType == null || string.IsNullOrEmpty(conflictType.Description))
            {
                return "This conflict type has no description.";
            }
            else
            {
                return conflictType.Description;
            }
        }

        public IConflictTypeUserControl GetConflictTypeUserControl(ConflictRuleViewModel conflict, Guid sourceId)
        {
            IConflictTypeView conflictType = GetConflictTypeView(conflict, sourceId);

            Type type;
            if (conflictType == null || conflictType.Type == null || conflictType.Type.GetInterface(typeof(IConflictTypeUserControl).Name) == null)
            {
                type = typeof(RuleEditView);
            }
            else
            {
                type = conflictType.Type;
            }

            IConflictTypeUserControl customControl = Activator.CreateInstance(type) as IConflictTypeUserControl;
            customControl.SetConflictRuleViewModel(conflict);
            return customControl;
        }

        public void AddMigrationSourceView(IMigrationSourceView migrationSourceView)
        {
            m_migrationSourceViews[migrationSourceView.ProviderId] = migrationSourceView;
        }

        public void AddConflictTypeView(IConflictTypeView conflictType, Guid sourceId)
        {
            if (!m_conflictTypes.ContainsKey(sourceId))
            {
                m_conflictTypes[sourceId] = new Dictionary<Guid, IConflictTypeView>();
            }
            m_conflictTypes[sourceId][conflictType.Guid] = conflictType;
        }

        private Dictionary<Guid, ExecuteFilterStringExtension> m_filterStringCommands = new Dictionary<Guid, ExecuteFilterStringExtension>();
        public void AddFilterStringExtension(Guid providerId, ExecuteFilterStringExtension filterStringExtension)
        {
            if (filterStringExtension != null)
            {
                m_filterStringCommands[providerId] = filterStringExtension;
            }
        }

        internal Dictionary<string, string> GetMigrationSourceProperties(MigrationSource migrationSource, Guid shellAdapterIdentifier)
        {
            IMigrationSourceView sourceView = GetMigrationSourceView(shellAdapterIdentifier);
            return sourceView.GetProperties(migrationSource);
        }
    }

    public interface IConflictTypeView // IConflictTypeControlExtension
    {
        Guid Guid { get; }
        string FriendlyName { get; }
        string Description { get; }
        Type Type { get; }
    }

    public interface IMigrationSourceView // IMigrationSourceConnectExtension
    {
        Guid ProviderId { get; }
        string Name { get; }
        BitmapImage Image { get; }
        ExecuteMigrationSourceView Command { get; }
        ExecuteMigrationSourceProperties GetProperties { get; }
    }

    public class MigrationSourceView : IMigrationSourceView
    {
        public MigrationSourceView(string name, Guid guid, BitmapImage image, ExecuteMigrationSourceView command, ExecuteMigrationSourceProperties getProperties)
        {
            Name = name;
            Image = image;
            Command = command;
            ProviderId = guid;
            GetProperties = getProperties;
        }

        public Guid ProviderId { get; private set; }

        public string Name { get; private set; }

        public BitmapImage Image { get; private set; }

        public ExecuteMigrationSourceView Command { get; private set; }

        public ExecuteMigrationSourceProperties GetProperties { get; private set; }
    }

    public delegate void ExecuteMigrationSourceView(MigrationSource migrationSource);
    public delegate Dictionary<string, string> ExecuteMigrationSourceProperties(MigrationSource migrationSource);

    public class ExecuteFilterStringExtension
    {
        public ExecuteFilterStringExtension(PopulateFilterStringCommand command, string emptyWitQuery, string vcFilterStringPrefix)
        {
            Command = command;
            EmptyWITQuery = emptyWitQuery;
            VCFilterStringPrefix = vcFilterStringPrefix;
        }

        public string EmptyWITQuery { get; private set; }

        public string VCFilterStringPrefix { get; private set; }

        public PopulateFilterStringCommand Command { get; private set; }

        public delegate void PopulateFilterStringCommand(FilterItem filterItem, MigrationSource migrationSource);
    }
}
